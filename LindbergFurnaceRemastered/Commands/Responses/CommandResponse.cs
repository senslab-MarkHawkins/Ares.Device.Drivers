using Ares.Toolkit.Serial.Commands;

namespace LindbergFurnaceRemastered.Commands.Responses
{
    public abstract class CommandResponse : SerialResponse
    {

      public CommandResponse(int address, FunctionCode functionCode)
      {
        Address = address;
        FunctionCode = functionCode;
      }


      public int Address { get; }
      public FunctionCode FunctionCode { get; }
    }
}
