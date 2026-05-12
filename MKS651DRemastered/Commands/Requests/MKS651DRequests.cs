using Ares.Toolkit.Serial.Commands;
using MKS651DRemastered.Commands.Responses;
using MKS651DRemastered.Commands.Responses.Parsers;
using System.Text;

namespace MKS651DRemastered.Commands.Requests;

public abstract class MKS651DRequest : SerialCommand
{
    protected abstract string SerializeToString();

    protected override byte[] Serialize()
    {
        return Encoding.ASCII.GetBytes(SerializeToString() + "\r");
    }
}

public abstract class MKS651DRequestWithResponse<T> : SerialCommandWithResponse<T> where T : SerialResponse
{
    protected MKS651DRequestWithResponse(SerialResponseParser<T> parser) : base(parser)
    {
    }

    protected abstract string SerializeToString();

    protected override byte[] Serialize()
    {
        return Encoding.ASCII.GetBytes(SerializeToString() + "\r");
    }
}

public class GetPressureCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    public GetPressureCommand() : base(new PressureParser()) { }
    protected override string SerializeToString() => "R5";
}

public class GetValvePositionCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    public GetValvePositionCommand() : base(new GenericNumericParser()) { }
    protected override string SerializeToString() => "R6";
}

public class GetMaxSensorRangeCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    public GetMaxSensorRangeCommand() : base(new GenericNumericParser()) { }
    protected override string SerializeToString() => "R33";
}

public class GetMinSensorRangeCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    public GetMinSensorRangeCommand() : base(new GenericNumericParser()) { }
    protected override string SerializeToString() => "R55";
}

public class OpenValveCommand : MKS651DRequest
{
    protected override string SerializeToString() => "O";
}

public class CloseValveCommand : MKS651DRequest
{
    protected override string SerializeToString() => "C";
}

public class SetMaxSensorRangeCommand : MKS651DRequest
{
    private readonly double _value;
    public SetMaxSensorRangeCommand(double value) => _value = value;
    protected override string SerializeToString() => $"EH {_value}";
}

public class SetMinSensorRangeCommand : MKS651DRequest
{
    private readonly double _value;
    public SetMinSensorRangeCommand(double value) => _value = value;
    protected override string SerializeToString() => $"EL {_value}";
}

public class SetSetpointActiveCommand : MKS651DRequest
{
    private readonly int _index;
    public SetSetpointActiveCommand(int index) => _index = index;
    protected override string SerializeToString() => $"D{_index}";
}

public class GetSetpointPressureCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    private readonly int _index;
    public GetSetpointPressureCommand(int index) : base(new GenericNumericParser()) => _index = index;
    protected override string SerializeToString() => _index == 5 ? "R10" : $"R{_index}";
}

public class GetSetpointGainCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    private readonly int _index;
    public GetSetpointGainCommand(int index) : base(new GenericNumericParser()) => _index = index;
    protected override string SerializeToString() => $"R{_index + 45}";
}

public class GetSetpointSoftCommand : MKS651DRequestWithResponse<MKS651DNumericResponse>
{
    private readonly int _index;
    public GetSetpointSoftCommand(int index) : base(new GenericNumericParser()) => _index = index;
    protected override string SerializeToString() => $"R{_index + 14}";
}

public class SetSetpointPressureCommand : MKS651DRequest
{
    private readonly int _index;
    private readonly double _value;
    public SetSetpointPressureCommand(int index, double value) { _index = index; _value = value; }
    protected override string SerializeToString() => $"S{_index} {_value}";
}

public class SetSetpointGainCommand : MKS651DRequest
{
    private readonly int _index;
    private readonly double _value;
    public SetSetpointGainCommand(int index, double value) { _index = index; _value = value; }
    protected override string SerializeToString() => $"M{_index} {_value}";
}

public class SetSetpointSoftCommand : MKS651DRequest
{
    private readonly int _index;
    private readonly double _value;
    public SetSetpointSoftCommand(int index, double value) { _index = index; _value = value; }
    protected override string SerializeToString() => $"I{_index} {_value}";
}
