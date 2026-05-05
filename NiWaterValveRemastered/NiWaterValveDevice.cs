using Ares.Datamodel;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NiWaterValve.Enums;

namespace NiWaterValve;

public class NiWaterValveDevice : AresDevice
{
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private readonly ILogger _logger;

    public NiWaterValveDevice(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        StateStream = _stateSubject.AsObservable();
        
        _stateSubject.OnNext(AresStateBuilder.Create()
            .Add("ValveVoltage", 0.0)
            .Add("WaterTarget", 0.0)
            .Build());
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override Task<bool> Activate(CancellationToken ct)
    {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "NI Water Valve Active" };
        return Task.FromResult(true);
    }

    public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<NiWaterValveCommand>(command, out var deviceCommandEnum))
        {
            return new CommandResult { Success = false, Error = $"Invalid or unsupported command: '{command}'" };
        }

        switch (deviceCommandEnum)
        {
            case NiWaterValveCommand.SetValveVoltage:
                var voltage = arguments.FirstOrDefault(a => a.ArgName == "Voltage")?.ArgValue.NumberValue ?? 0;
                await SetValveVoltage(voltage);
                return new CommandResult { Success = true };
            case NiWaterValveCommand.SetWaterTarget:
                var target = arguments.FirstOrDefault(a => a.ArgName == "Target")?.ArgValue.NumberValue ?? 0;
                await SetWaterTarget(target);
                return new CommandResult { Success = true };
            default:
                return new CommandResult { Success = false, Error = "Command not supported by this device" };
        }
    }

    private Task SetValveVoltage(double voltage)
    {
        // Legacy: Write to NI-DAQmx (not implemented here, placeholders used)
        _logger.LogInformation("Setting valve voltage to {Voltage}V", voltage);
        var next = AresStateBuilder.From(_stateSubject.Value)
            .Add("ValveVoltage", voltage)
            .Build();
        _stateSubject.OnNext(next);
        return Task.CompletedTask;
    }

    private Task SetWaterTarget(double target)
    {
        _logger.LogInformation("Setting water target to {Target} PPM", target);
        var next = AresStateBuilder.From(_stateSubject.Value)
            .Add("WaterTarget", target)
            .Build();
        _stateSubject.OnNext(next);
        return Task.CompletedTask;
    }

    public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;

    public override Task<AresStruct> GetSettings() => Task.FromResult(new AresStruct());

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new()
            {
                Name = NiWaterValveCommand.SetValveVoltage.ToString(),
                Description = "Sets the valve output voltage",
                InputSchema = AresSchemaBuilder.Empty()
                    .AddEntry("Voltage", AresSchemaBuilder.NumberEntry().Build())
                    .Build()
            },
            new()
            {
                Name = NiWaterValveCommand.SetWaterTarget.ToString(),
                Description = "Sets the target water PPM for PID control",
                InputSchema = AresSchemaBuilder.Empty()
                    .AddEntry("Target", AresSchemaBuilder.NumberEntry().Build())
                    .Build()
            }
        });
    }

    public override async Task EnterSafeMode(CancellationToken ct)
    {
        await SetValveVoltage(0);
    }
}
