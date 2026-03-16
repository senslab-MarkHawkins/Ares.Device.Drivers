using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace TC0304Remastered.Commands;

internal class DataRequest : SerialCommandWithResponse<DataResponse>
{
  public DataRequest() : base(new DataResponseParser())
  {
  }

  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes("A");
}
