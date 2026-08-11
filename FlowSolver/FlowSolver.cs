using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using FlowSolver.Calculations;
using FlowSolver.Composition;
using FlowSolver.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FlowSolver;

public class FlowSolver :
    AresDevice,
    IFlowSolver
{
    private const string GasArg = "gas";
    private const string MfcArg = "mfc";
    private const string ValueArg = "value";
    private const string UnitArg = "unit";

    private readonly ILogger _logger;

    private readonly object _stateLock = new();

    private readonly IReadOnlyList<FlowComponent> _flowComponents;

    private readonly FlowCompositionSolver _solver;

    private readonly FlowCompositionModel _model;

    private readonly TargetComposition _targetComposition = new();

    private readonly Dictionary<string, double> _calculatedSetpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly CompositionBuilder _compositionBuilder;

    private double _targetFlow;

    private bool _calculationValid;

    private string _calculationError = string.Empty;

    private double _residualNorm;

    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

    public FlowSolver(
        DeviceConnectionInfo info,
        ILogger logger)
        : base(info)
    {
        _logger = logger;

        _flowComponents = FlowComponent.ComponentsFromAres( info.DeviceSettings);
_compositionBuilder =    new CompositionBuilder(        null);

_solver =
    new FlowCompositionSolver(        _compositionBuilder);

        _model = _solver.BuildModel( _flowComponents);

        _targetFlow = 0.0;

        StateStream =
            _stateSubject.AsObservable();

        BuildStateSchema();

        PublishState();
    }

    public override IObservable<AresStruct>
        StateStream
    { get; }

    public override Task<bool> Activate(CancellationToken ct) => Task.FromResult(true);

    /// <summary>
    /// Calculates the component setpoints using the
    /// current target composition and total flow.
    /// </summary>
    public Task CalculateSetpoints()
    {
        CompositionComponent[] targetComponents;
        double targetFlow;

        lock (_stateLock)
        {
            targetComponents = _targetComposition.Components.ToArray();

            targetFlow = _targetFlow;
        }

        IReadOnlyDictionary<string, double> normalizedComposition;

        try
        {
            normalizedComposition = _compositionBuilder.Build(targetComponents);
        }
        catch (Exception exception)
        {
            lock (_stateLock)
            {
                _calculatedSetpoints.Clear();

                _calculationValid = false;
                _residualNorm = double.NaN;
                _calculationError = exception.Message;

                PublishState();
            }

            return Task.CompletedTask;
        }

        var result =
            _solver.Calculate(_model, normalizedComposition, targetFlow);

        lock (_stateLock)
        {
            _calculatedSetpoints.Clear();

            _calculationValid = result.Success;

            _residualNorm = result.ResidualNorm;

            if (result.Success)
            {
                foreach (var setpoint in result.Setpoints)
                {
                    _calculatedSetpoints[setpoint.DeviceName] = setpoint.Flow;
                }

                _calculationError = string.Empty;
            }
            else
            {
                _calculationError = string.Join("; ", result.Errors);
            }

            PublishState();
        }

        return Task.CompletedTask;
    }

    public Task<double> GetSetpoint( string mfc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            mfc);

        lock (_stateLock)
        {
            if (!_calculatedSetpoints.TryGetValue(
                    mfc.Trim(),
                    out var setpoint))
            {
                throw new ArgumentException(
                    $"No calculated setpoint exists for MFC '{mfc}'.",
                    nameof(mfc));
            }

            return Task.FromResult(
                setpoint);
        }
    }

    public Task ClearTarget()
    {
        lock (_stateLock)
        {
            _calculatedSetpoints.Clear();
            InvalidateCalculation();
            PublishState();
        }
        return Task.CompletedTask;
    }
    public Task SetTargetComponent(
        string gas,
        double? concentration,
        string unit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gas);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        gas = gas.Trim();
        unit = unit.Trim();

        if (!Enum.TryParse<CompositionUnit>(
                unit,
                ignoreCase: true,
                out var parsedUnit))
        {
            throw new ArgumentException(
                $"Unknown composition unit '{unit}'.",
                nameof(unit));
        }

        if (parsedUnit != CompositionUnit.Balance)
        {
            if (!concentration.HasValue)
            {
                throw new ArgumentException(
                    $"A concentration value is required for gas '{gas}'.",
                    nameof(concentration));
            }

            if (!double.IsFinite(concentration.Value) ||
                concentration.Value < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(concentration),
                    "Concentration must be finite and non-negative.");
            }
        }
        else
        {
            /*
             * Balance does not use a numeric concentration.
             */
            concentration = null;
        }

        lock (_stateLock)
        {
            _targetComposition.Set(
                gas,
                concentration,
                parsedUnit);

            /*
             * Any previously calculated setpoints are now stale.
             */
            InvalidateCalculation();

            PublishState();
        }

        return Task.CompletedTask;
    }

    public Task SetTargetFlow(        double flow)
    {
        if (!double.IsFinite(flow) ||
            flow < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(flow),
                "Flow must be finite and non-negative.");
        }

        lock (_stateLock)
        {
            _targetFlow =
                flow;

            InvalidateCalculation();

            PublishState();
        }

        return Task.CompletedTask;
    }
    private void InvalidateCalculation()
    {
        _calculationValid = false;
        _calculationError =
            "Calculation is stale.";
        _residualNorm =
            double.NaN;

        _calculatedSetpoints.Clear();
    }


    public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);
    public override Task<AresStruct> GetSettings() => Task.FromResult(AresStructHelper.CreateEmptyStruct());
    public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;
    public override Task EnterSafeMode(CancellationToken ct) => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        _stateSubject.OnCompleted();
        _stateSubject.Dispose();

        return ValueTask.CompletedTask;
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        var result =
            new CommandResult
            {
                Success = true
            };

        try
        {
            switch (command)
            {
                case nameof(SetTargetComponent):
                    {
                        var gas = arguments.First(argument => argument.ArgName == GasArg).ArgValue.StringValue;
                        var concentration = arguments.First(argument => argument.ArgName == ValueArg).ArgValue.NumberValue;
                        var unit = arguments.First(argument => argument.ArgName == UnitArg).ArgValue.StringValue;
                        await SetTargetComponent(gas, concentration, unit);
                        break;
                    }

                case nameof(SetTargetFlow):
                    {
                        var flow = arguments.First(argument => argument.ArgName == ValueArg).ArgValue.NumberValue;
                        await SetTargetFlow(flow);
                        break;
                    }

                case nameof(CalculateSetpoints):
                    {
                        await CalculateSetpoints();

                        if (!_calculationValid)
                        {
                            result.Success = false;
                            result.Error = _calculationError;
                        }

                        break;
                    }

                case nameof(GetSetpoint):
                    {
                        var mfc = arguments.First(argument => argument.ArgName == MfcArg).ArgValue.StringValue;
                        var setpoint = await GetSetpoint(mfc);
                        result.Result = AresValueHelper.CreateNumber(setpoint);

                        break;
                    }
                case nameof(ClearTarget):
                    {
                        await ClearTarget();
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(command),
                        command,
                        "Unrecognized FlowSolver command.");
            }
        }
        catch (Exception exception)
        {
            result.Success = false;
            result.Error =
                $"Error executing '{command}': " +
                exception.Message;
        }

        return result;
    }

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        var descriptors =
            new List<DeviceCommandDescriptor>
            {
                new()
                {
                    Name =
                        nameof(SetTargetComponent),

                    Description =
                        "Sets the target concentration for a gas.",

                    InputSchema =
                        AresSchemaBuilder.Empty()
                            .AddEntry(
                                GasArg,
                                AresSchemaBuilder
                                    .StringEntry()
                                    .Build())
                            .AddEntry(
                                ValueArg,
                                AresSchemaBuilder
                                    .NumberEntry()
                                    .Build())
                            .AddEntry(
                                UnitArg,
                                AresSchemaBuilder
                                    .StringEntry()
                                    .Build())
                            .Build()
                },

                new()
                {
                    Name =
                        nameof(SetTargetFlow),

                    Description =
                        "Sets the target total flow.",

                    InputSchema =
                        AresSchemaBuilder.Empty()
                            .AddEntry(
                                ValueArg,
                                AresSchemaBuilder
                                    .NumberEntry()
                                    .Build())
                            .Build()
                },

                new()
                {
                    Name =
                        nameof(CalculateSetpoints),

                    Description =
                        "Calculates component setpoints from the " +
                        "current target composition and total flow."
                },
                new()
                {
                    Name =
                        nameof(ClearTarget),

                    Description =
                        "Clears current flow description."
                },
                new()
                {
                    Name =
                        nameof(GetSetpoint),

                    Description =
                        "Gets the most recently calculated setpoint " +
                        "for a configured flow controller.",

                    InputSchema =
                        AresSchemaBuilder.Empty()
                            .AddEntry(
                                MfcArg,
                                AresSchemaBuilder
                                    .StringEntry()
                                    .Build())
                            .Build(),

                    OutputSchema =
                        AresSchemaBuilder
                            .NumberEntry()
                            .WithDescription(
                                "Calculated flow setpoint")
                            .Build()
                }
            };

        return Task.FromResult(
            descriptors);
    }

    private void BuildStateSchema()
    {
        StateSchema =
     AresSchemaBuilder.Empty()
         .AddEntry(
             "Name",
             AresSchemaBuilder
                 .StringEntry()
                 .Build())

         .AddEntry(
             "CalculationValid",
             AresSchemaBuilder
                 .Entry(AresDataType.Boolean)
                 .Build())

         .AddEntry(
             "CalculationError",
             AresSchemaBuilder
                 .StringEntry()
                 .Build())

         .AddEntry(
             "ResidualNorm",
             AresSchemaBuilder
                 .NumberEntry()
                 .Build())

         .AddEntry(
             "TargetFlow",
             AresSchemaBuilder
                 .NumberEntry()
                 .Build())

         .AddEntry(
             "TargetComposition",
             AresSchemaBuilder
                 .Entry(AresDataType.List)
                 .WithListElementSchema(element =>
                 {
                     element.WithStructSchema(component =>
                     {
                         component.Fields.Add(
                             "Component",
                             AresSchemaBuilder
                                 .StringEntry()
                                 .Build());

                         component.Fields.Add(
                             "Unit",
                             AresSchemaBuilder
                                 .StringEntry()
                                 .Build());

                         component.Fields.Add(
                             "Value",
                             AresSchemaBuilder
                                 .NumberEntry()
                                 .Build());
                     });
                 })
                 .Build())

         .AddEntry(
             "Setpoints",
             AresSchemaBuilder
                 .Entry(AresDataType.Struct)
                 .WithStructSchema(setpoints =>
                 {
                     foreach (var component in
                              _model.FlowComponents)
                     {
                         setpoints.Fields.Add(
                             component.DeviceName,
                             AresSchemaBuilder
                                 .NumberEntry()
                                 .Build());
                     }
                 })
                 .Build())

         .Build();
    }

    private void PublishState()
    {
        var state =
            AresStateBuilder.Create()
                .Add(
                    "Name",
                    Name)

                .Add(
                    "CalculationValid",
                    _calculationValid)

                .Add(
                    "CalculationError",
                    _calculationError)

                .Add(
                    "ResidualNorm",
                    double.IsFinite(_residualNorm)
                        ? _residualNorm
                        : 0.0)

                .Add(
                    "TargetFlow",
                    _targetFlow)

                .AddList(
                    "TargetComposition",
                    _targetComposition.Components,
                    component =>
                        new AresValue
                        {
                            StructValue =
                                AresStateBuilder.Create()
                                    .Add(
                                        "Component",
                                        component.Component)
                                    .Add(
                                        "Unit",
                                        component.Unit.ToString())
                                    .Add(
                                        "Value",
                                        component.Value ?? 0.0)
                                    .Build()
                        })

                .AddStruct(
                    "Setpoints",
                    setpoints =>
                    {
                        foreach (var component in
                                 _model.FlowComponents)
                        {
                            setpoints.Add(
                                component.DeviceName,
                                _calculatedSetpoints
                                    .TryGetValue(
                                        component.DeviceName,
                                        out var value)
                                    ? value
                                    : 0.0);
                        }
                    })

                .Build();

        _stateSubject.OnNext(state);
    }


}