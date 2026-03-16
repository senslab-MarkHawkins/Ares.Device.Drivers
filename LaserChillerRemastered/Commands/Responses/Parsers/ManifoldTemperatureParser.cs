namespace LaserChillerRemastered.Commands.Responses.Parsers;

public class ManifoldTemperatureParser : ResponseParser<GetManifoldTemperatureResponse>
{
  protected override bool TryParseResponse(string line, out GetManifoldTemperatureResponse? response)
  {
    if(string.IsNullOrWhiteSpace(line) || !line.StartsWith("#I0", StringComparison.Ordinal))
    {
      response = null;
      return false;
    }

    if(line.Length < "#I0".Length + 5)
    {
      response = null;
      return false;
    }

    var formattedResponse = line.Substring("#I0".Length, 5);
    var signCharacter = formattedResponse[0];
    var formattedTempData = formattedResponse.Substring(1).Insert(3, ".");
    var temp = Convert.ToDouble(formattedTempData);

    if(signCharacter == '-')
      temp *= -1;

    response = new GetManifoldTemperatureResponse(temp);
    return true;
  }
}
