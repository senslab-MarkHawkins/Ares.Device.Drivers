using AlicatBusControl.Calculations;
using AlicatBusControl.Enums;
using AlicatBusControl.Models;
using AlicatMFCRemastered;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using DynamicData;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UnitsNet;

namespace AlicatBusControl
{
    public class AlicatBusController : AresDevice, IAlicatBusController
    {
        private const string GasComponentArg = "gas";
        private const string ValueArg = "value";

        private const double RelativeFlowTolerance = 0.01;          // 1%
        private const double AbsoluteFlowTolerance = 0.5;           // SCCM

        private const double RelativeSetpointTolerance = 0.001;     // 0.1%
        private const double AbsoluteSetpointTolerance = 0.01;      // SCCM

        private static readonly TimeSpan SettlingTime =
            TimeSpan.FromSeconds(3);

        private DateTimeOffset? _lastApplyTime;

        private readonly DeviceConnectionInfo _info;
        private readonly ILogger _logger;

        private readonly IReadOnlyList<FlowComponent> _flowComponents;
        private readonly FlowCompositionSolver _flowSolver;
        private readonly FlowCompositionModel _flowModel;

        private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

        private readonly object _stateLock = new();

        private readonly Dictionary<string, double> _targetComposition;
        private readonly Dictionary<string, double> _appliedComposition;
        private readonly Dictionary<string, double> _activeComposition;
        private readonly Dictionary<string, double> _appliedSetpoints =    new(StringComparer.OrdinalIgnoreCase);

        private double _targetFlow;
        private double _appliedFlow;
        private double _activeFlow;

        private bool _hasPendingChanges;
        private FlowVerificationState _verificationState;

        private readonly CancellationTokenSource _monitorCancellation = new();
        private Task? _monitorTask;

        private static readonly TimeSpan ActiveStateUpdateInterval =
            TimeSpan.FromSeconds(1);

        public AlicatBusController(
            DeviceConnectionInfo info,
            ILogger logger)
            : base(info)
        {
            _info = info;
            _logger = logger;

            // Load the static flow configuration
            _flowComponents = FlowComponent.ComponentsFromAres(info.DeviceSettings);

            // Build the static solver model once
            _flowSolver = new FlowCompositionSolver();
            _flowModel = _flowSolver.BuildModel(_flowComponents);

            _targetComposition = _flowModel.Gases.ToDictionary(
                gas => gas,
                _ => 0.0,
                StringComparer.OrdinalIgnoreCase);

            _appliedComposition = _flowModel.Gases.ToDictionary(
                gas => gas,
                _ => 0.0,
                StringComparer.OrdinalIgnoreCase);

            _activeComposition = _flowModel.Gases.ToDictionary(
                gas => gas,
                _ => 0.0,
                StringComparer.OrdinalIgnoreCase);


            _targetFlow = 0.0;
            _appliedFlow = 0.0;
            _activeFlow = 0.0;
            _hasPendingChanges = false;

            StateStream = _stateSubject.AsObservable();

            StateSchema = AresSchemaBuilder.Empty()
                    .AddEntry("Name", AresSchemaBuilder.StringEntry().Build())
                    .AddEntry("ChangesPending", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
                    .AddEntry("FlowVerificationState", AresSchemaBuilder.StringEntry().Build())
                    .AddEntry("Applied", AresSchemaBuilder.Entry(AresDataType.Struct)
                        .WithStructSchema(af =>
                        {
                            foreach(var flow in _appliedComposition)
                            {
                                af.Fields.Add(flow.Key, AresSchemaBuilder.NumberEntry().Build());
                            }
                            af.Fields.Add("TotalFlow", AresSchemaBuilder.NumberEntry().Build());
                        })
                        .Build())
                    .AddEntry("Target", AresSchemaBuilder.Entry(AresDataType.Struct)
                        .WithStructSchema(af =>
                        {
                            foreach (var flow in _targetComposition)
                            {
                                af.Fields.Add(flow.Key, AresSchemaBuilder.NumberEntry().Build());
                            }
                            af.Fields.Add("TotalFlow", AresSchemaBuilder.NumberEntry().Build());
                        })
                        .Build())
                    .Build();

            PublishInitialState();
        }

        private const double StateComparisonTolerance = 1e-9;

        private void UpdatePendingChanges()
        {
            _hasPendingChanges =
                Math.Abs(_targetFlow - _appliedFlow) >
                    StateComparisonTolerance ||
                _flowModel.Gases.Any(
                    gas =>
                        Math.Abs(
                            _targetComposition[gas] -
                            _appliedComposition[gas]) >
                        StateComparisonTolerance);
        }

        private async Task MonitorActiveStateAsync(
    CancellationToken cancellationToken)
        {
            using var timer =
                new PeriodicTimer(ActiveStateUpdateInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(
                           cancellationToken))
                {
                    await UpdateActiveStateAsync(
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The Alicat bus active-state monitor stopped unexpectedly.");
            }
        }

        private async Task UpdateActiveStateAsync(
       CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, double> appliedSetpoints;

            lock (_stateLock)
            {
                appliedSetpoints =
                    new Dictionary<string, double>(
                        _appliedSetpoints,
                        StringComparer.OrdinalIgnoreCase);
            }

            if (appliedSetpoints.Count == 0)
            {
                SetVerificationState(
                    FlowVerificationState.Unknown);

                return;
            }

            var componentFlows =
                new double[_flowModel.FlowComponents.Count];

            var allSetpointsConfirmed = true;
            var allFlowsConfirmed = true;

            for (var index = 0;
                 index < _flowModel.FlowComponents.Count;
                 index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var flowComponent =
                    _flowModel.FlowComponents[index];

                if (!MassFlowControllerRegistry.TryGet(
                        flowComponent.DeviceName,
                        out var controller) ||
                    controller is null)
                {
                    _logger.LogDebug(
                        "MFC '{DeviceName}' required by the Alicat bus " +
                        "is not currently registered.",
                        flowComponent.DeviceName);

                    SetVerificationState(
                        FlowVerificationState.MissingHardware);

                    return;
                }

                AresStruct state;

                try
                {
                    state = await controller
                        .GetState()
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogDebug(
                        exception,
                        "Unable to retrieve state from MFC '{DeviceName}'.",
                        flowComponent.DeviceName);

                    SetVerificationState(
                        FlowVerificationState.Unknown);

                    return;
                }

                MassFlowControllerSnapshot snapshot;

                try
                {
                    snapshot =
                        MassFlowControllerSnapshot.FromAresStruct(
                            state);
                }
                catch (Exception exception)
                {
                    _logger.LogDebug(
                        exception,
                        "MFC '{DeviceName}' has not published valid live data.",
                        flowComponent.DeviceName);

                    SetVerificationState(
                        FlowVerificationState.Unknown);

                    return;
                }

                if (!appliedSetpoints.TryGetValue(
                        flowComponent.DeviceName,
                        out var transmittedSetpoint))
                {
                    SetVerificationState(
                        FlowVerificationState.Unknown);

                    return;
                }

                var setpointTolerance =
                    (
                        transmittedSetpoint);

                var setpointConfirmed =
                    Math.Abs(
                        snapshot.Setpoint -
                        transmittedSetpoint) <=
                    setpointTolerance;

                allSetpointsConfirmed &=
                    setpointConfirmed;

                var flowTolerance =
                    CalculateFlowTolerance(
                        snapshot.Setpoint);

                var flowConfirmed =
                    Math.Abs(
                        snapshot.MassFlow -
                        snapshot.Setpoint) <=
                    flowTolerance;

                allFlowsConfirmed &=
                    flowConfirmed;

                componentFlows[index] =
                    Math.Max(0.0, snapshot.MassFlow);
            }

            UpdateMeasuredComposition(componentFlows);

            FlowVerificationState nextState;

            if (!allSetpointsConfirmed)
            {
                nextState =
                    FlowVerificationState.OutOfSync;
            }
            else if (allFlowsConfirmed)
            {
                nextState =
                    FlowVerificationState.FlowsConfirmed;
            }
            else if (IsWithinSettlingPeriod())
            {
                nextState =
                    FlowVerificationState.Settling;
            }
            else
            {
                nextState =
                    FlowVerificationState.FlowError;
            }

            SetVerificationState(nextState);
        }

        private bool IsWithinSettlingPeriod()
        {
            if (!_lastApplyTime.HasValue)
            {
                return false;
            }

            return DateTimeOffset.UtcNow - _lastApplyTime.Value
                < SettlingTime;
        }

        private static double CalculateFlowTolerance(double setpoint)
        {
            return Math.Max(
                AbsoluteFlowTolerance,
                Math.Abs(setpoint) * RelativeFlowTolerance);
        }

        private void UpdateMeasuredComposition(
    IReadOnlyList<double> componentFlows)
        {
            var flowVector =
                MathNet.Numerics.LinearAlgebra
                    .Vector<double>
                    .Build
                    .DenseOfEnumerable(componentFlows);

            var gasFlows =
                _flowModel.CompositionMatrix * flowVector;

            var totalFlow =
                componentFlows.Sum();

            lock (_stateLock)
            {
                _activeFlow = totalFlow;

                for (var i = 0;
                     i < _flowModel.Gases.Count;
                     i++)
                {
                    _activeComposition[_flowModel.Gases[i]] =
                        totalFlow > 0
                            ? gasFlows[i] / totalFlow
                            : 0;
                }

                PublishState();
            }
        }

        private void SetVerificationState(
    FlowVerificationState state)
        {
            lock (_stateLock)
            {
                if (_verificationState == state)
                {
                    return;
                }

                _verificationState = state;
                PublishState();
            }
        }

        public IReadOnlyList<FlowComponent> FlowComponents => _flowComponents;

        public double? TargetFlow => _targetFlow;

        public override IObservable<AresStruct> StateStream {get;}

        public override Task<bool> Activate(
            CancellationToken ct)
        {
            if (_monitorTask is null)
            {
                _monitorTask =
                    MonitorActiveStateAsync(
                        _monitorCancellation.Token);
            }

            PublishState();

            return Task.FromResult(true);
        }


        public async Task<bool> ApplyFlow()
        {
            Dictionary<string, double> targetComposition;
            double targetFlow;

            lock (_stateLock)
            {
                targetComposition =
                    new Dictionary<string, double>(
                        _targetComposition,
                        StringComparer.OrdinalIgnoreCase);

                targetFlow = _targetFlow;
            }

            var result = _flowSolver.Calculate(
                _flowModel,
                targetComposition,
                targetFlow);

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning(
                        "Unable to apply flow: {Error}",
                        error);
                }

                return false;
            }

            var resolvedControllers =
                new List<(
                    FlowSetpoint Setpoint,
                    MassFlowController Controller)>(
                        result.Setpoints.Count);

            foreach (var setpoint in result.Setpoints)
            {
                if (!MassFlowControllerRegistry.TryGet(
                        setpoint.DeviceName,
                        out var controller) ||
                    controller is null)
                {
                    _logger.LogWarning(
                        "Cannot apply flow because MFC '{DeviceName}' " +
                        "is not currently registered.",
                        setpoint.DeviceName);

                    lock (_stateLock)
                    {
                        _verificationState =
                            FlowVerificationState.MissingHardware;

                        PublishState();
                    }

                    return false;
                }

                resolvedControllers.Add(
                    (setpoint, controller));
            }

            try
            {
                foreach (var item in resolvedControllers)
                {
                    var flow =
                        StandardVolumeFlow
                            .FromStandardCubicCentimetersPerMinute(
                                item.Setpoint.Flow);

                    await item.Controller
                        .NewSetpoint(flow)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while applying MFC setpoints.");

                return false;
            }

            lock (_stateLock)
            {
                _appliedSetpoints.Clear();

                foreach (var setpoint in result.Setpoints)
                {
                    _appliedSetpoints[setpoint.DeviceName] =
                        setpoint.Flow;
                }

                _appliedFlow = targetFlow;

                foreach (var gas in _flowModel.Gases)
                {
                    _appliedComposition[gas] =
                        result.AchievedComposition.TryGetValue(
                            gas,
                            out var concentration)
                            ? concentration
                            : 0.0;
                }

                _lastApplyTime = DateTimeOffset.UtcNow;
                _verificationState =
                    FlowVerificationState.Settling;

                UpdatePendingChanges();
                PublishState();
            }

            return true;
        }


        private void PublishState()
        {
            //StateSchema = AresSchemaBuilder.Empty()
            //        .AddEntry("Name", AresSchemaBuilder.StringEntry().Build())
            //        .AddEntry("ChangesPending", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
            //        .AddEntry("FlowVerificationState", AresSchemaBuilder.StringEntry().Build())
            //        .AddEntry("Applied", AresSchemaBuilder.Entry(AresDataType.Struct)
            //            .WithStructSchema(af =>
            //            {
            //                foreach (var flow in _appliedComposition)
            //                {
            //                    af.Fields.Add(flow.Key, AresSchemaBuilder.NumberEntry().Build());
            //                }
            //                af.Fields.Add("TotalFlow", AresSchemaBuilder.NumberEntry().Build());
            //            })
            //            .Build())
            //        .AddEntry("Target", AresSchemaBuilder.Entry(AresDataType.Struct)
            //            .WithStructSchema(af =>
            //            {
            //                foreach (var flow in _targetComposition)
            //                {
            //                    af.Fields.Add(flow.Key, AresSchemaBuilder.NumberEntry().Build());
            //                }
            //                af.Fields.Add("TotalFlow", AresSchemaBuilder.NumberEntry().Build());
            //            })
            //            .Build())
            //        .Build();

            _stateSubject.OnNext(
                AresStateBuilder.Create()
                    .Add("Name", Name)
                    .Add("ChangesPending", _hasPendingChanges)
                    .Add("FlowVerificationState", _verificationState.ToString())
                    .AddStruct("Applied", applied =>
                    {
                        foreach (var flow in _appliedComposition)
                        {
                            applied.Add(flow.Key, _appliedComposition[flow.Key]);
                        }
                        applied.Add("TotalFlow", _appliedFlow);
                    }).AddStruct("Target", target =>
                    {
                        foreach (var flow in _targetComposition)
                        {
                            target.Add(flow.Key, _targetComposition[flow.Key]);
                        }
                        target.Add("TotalFlow", _targetFlow);
                    }
                    )
                    .Build());
        }

        private void PublishInitialState()
        {
            //StateSchema = AresSchemaBuilder.Empty()
            //        .AddEntry("Name", AresSchemaBuilder.StringEntry().Build())
            //        .AddEntry("ChangesPending", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
            //        .AddEntry("FlowVerificationState", AresSchemaBuilder.StringEntry().Build())
            //        .AddEntry("Applied", AresSchemaBuilder.Entry(AresDataType.Struct)
            //            .WithStructSchema(af =>
            //            {
            //                foreach (var flow in _appliedComposition)
            //                {
            //                    af.Fields.Add(flow.Key, AresSchemaBuilder.NumberEntry().Build());
            //                }
            //                af.Fields.Add("TotalFlow", AresSchemaBuilder.NumberEntry().Build());
            //            })
            //            .Build())
            //        .AddEntry("Target", AresSchemaBuilder.Entry(AresDataType.Struct)
            //            .WithStructSchema(af =>
            //            {
            //                foreach (var flow in _targetComposition)
            //                {
            //                    af.Fields.Add(flow.Key, AresSchemaBuilder.NumberEntry().Build());
            //                }
            //                af.Fields.Add("TotalFlow", AresSchemaBuilder.NumberEntry().Build());
            //            })
            //            .Build())
            //        .Build();

            _stateSubject.OnNext(
                AresStateBuilder.Create()
                    .Add("Name", Name)
                    .Add("ChangesPending", false)
                    .Add("FlowVerificationState", FlowVerificationState.Unknown.ToString())
                    .AddStruct("Applied", applied =>
                    {
                        foreach (var flow in _appliedComposition)
                        {
                            applied.Add(flow.Key, 0);
                        }
                        applied.Add("TotalFlow", 0);
                    }).AddStruct("Target", target =>
                    {
                        foreach (var flow in _targetComposition)
                        {
                            target.Add(flow.Key, 0);
                        }
                        target.Add("TotalFlow", 0);
                    }
                    )
                    .Build());
        }

        public async ValueTask DisposeAsync()
        {
            _monitorCancellation.Cancel();

            if (_monitorTask is not null)
            {
                try
                {
                    await _monitorTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during disposal.
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "An error occurred while stopping the Alicat bus monitor.");
                }
            }

            _monitorCancellation.Dispose();

            _stateSubject.OnCompleted();
            _stateSubject.Dispose();
        }

        public override Task EnterSafeMode(CancellationToken ct)
        {
            // Controllers handle safe settings independently, so no need to do anything here.
            return Task.CompletedTask;
        }

        public async override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
        {
            var result = new CommandResult() { Success=true};
            try
            {
                switch (command)
                {
                    case nameof(SetTargetComposition):
                        var tartgetGas = arguments.First(a => a.ArgName == GasComponentArg).ArgValue.StringValue;
                        var targetValue = arguments.First(a => a.ArgName == ValueArg).ArgValue.NumberValue;
                        await SetTargetComposition(tartgetGas, targetValue);
                        break;
                    case nameof(SetTargetFlow):
                        var newTartgetFlow = arguments.First(a => a.ArgName == ValueArg).ArgValue.NumberValue;
                        await SetTargetFlow(newTartgetFlow);
                        break;
                    case nameof(GetTargetComposition):
                        var gasComposition = await GetTargetComposition(arguments.First(a=> a.ArgName==GasComponentArg).ArgValue.StringValue);
                        result.Result = AresValueHelper.CreateNumber(gasComposition);
                        break;
                    case nameof(GetTotalFlow):
                        var totalFlow = await GetTotalFlow();
                        result.Result = AresValueHelper.CreateNumber(totalFlow);
                        break;
                    case nameof(ApplyFlow):
                        await ApplyFlow();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unrecognized command");
                }
            } catch (Exception ex) 
            {
                result.Success = false;
                result.Error= $"Error executing command '{command}': {ex.Message}";
            }
            return result;
        }

        public override Task<AresStruct> GetSettings()
        {
            return Task.FromResult(AresStructHelper.CreateEmptyStruct());
        }

        public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

        public Task<double> GetTargetComposition(string gas)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gas);

            gas = gas.Trim();

            lock (_stateLock)
            {
                if (!_targetComposition.TryGetValue(
                        gas,
                        out var concentration))
                {
                    throw new ArgumentException(
                        $"Gas '{gas}' is not configured.",
                        nameof(gas));
                }

                return Task.FromResult(concentration);
            }
        }

        public Task<double> GetTotalFlow()
        {
            lock (_stateLock)
            {
                return Task.FromResult(_targetFlow);
            }
        }

        public Task SetTargetComposition(
            string gas,
            double concentration)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gas);

            gas = gas.Trim();

            if (!_targetComposition.ContainsKey(gas))
            {
                throw new ArgumentException(
                    $"Gas '{gas}' is not available in this flow configuration.",
                    nameof(gas));
            }

            if (!double.IsFinite(concentration))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(concentration),
                    "Concentration must be finite.");
            }

            if (concentration < 0.0 || concentration > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(concentration),
                    "Concentration must be between 0 and 1.");
            }

            lock (_stateLock)
            {
                _targetComposition[gas] = concentration;
                UpdatePendingChanges();
                PublishState();
            }

            return Task.CompletedTask;
        }

        public Task SetTargetFlow(double totalFlow)
        {
            if (!double.IsFinite(totalFlow))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalFlow),
                    "Total flow must be finite.");
            }

            if (totalFlow < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalFlow),
                    "Total flow cannot be negative.");
            }

            lock (_stateLock)
            {
                _targetFlow = totalFlow;
                UpdatePendingChanges();
                PublishState();
            }

            return Task.CompletedTask;
        }

        public override Task UpdateSettings(AresStruct settings)
        {
            // No settings to update for this controller
            return Task.CompletedTask;
        }

        protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
        {
            var descriptors = new List<DeviceCommandDescriptor>
            {
                new()
                {
                    Name=nameof(ApplyFlow),
                    Description="Applies the current target composition and flow to the MFCs"
                },
                new()
                {
                    Name=nameof(GetTotalFlow),
                    Description="Gets the current total flow of all components",
                    OutputSchema=AresSchemaBuilder.NumberEntry()
                        .WithDescription("Current total flow of all components")
                        .Build()
                },
                new()
                {
                    Name=nameof(GetTargetComposition),
                    Description="Gets the current target composition for a specific gas component",
                    InputSchema=AresSchemaBuilder.Empty()
                        .AddEntry(GasComponentArg, AresSchemaBuilder.StringEntry().Build())
                        .Build(),
                    OutputSchema=AresSchemaBuilder.NumberEntry()
                        .WithDescription("Current target composition for the specified gas")
                        .Build()
                },
                new()
                {
                    Name=nameof(SetTargetFlow),
                    Description="Sets the target total flow of all components",
                    InputSchema=AresSchemaBuilder.Empty()
                        .AddEntry(ValueArg, AresSchemaBuilder.NumberEntry().Build())
                        .Build()
                },
                new()
                {
                    Name=nameof(SetTargetComposition),
                    Description="Sets the target composition for a specific gas component",
                    InputSchema=AresSchemaBuilder.Empty()
                        .AddEntry(GasComponentArg, AresSchemaBuilder.StringEntry().Build())
                        .AddEntry(ValueArg, AresSchemaBuilder.NumberEntry().Build())
                        .Build()
                }
            };
            return Task.FromResult(descriptors);
        }
    }
}
