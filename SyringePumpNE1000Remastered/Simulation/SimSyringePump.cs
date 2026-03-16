using System.Globalization;
using System.Text;
using SyringePumpNE1000Remastered.Commands;

namespace SyringePumpNE1000Remastered.Simulation;

internal sealed class SimSyringePump
{
  private readonly Action<byte[]> _byteSender;
  private readonly Lock _gate = new();
  private readonly SyringePumpState _state;

  public SimSyringePump(Action<byte[]> byteSender, int address)
  {
    _byteSender = byteSender;
    _state = new SyringePumpState
    {
      FirmwareVersion = "NE1000V3.929-SIM",
      Address = address,
      DiameterMm = 29.2,
      Status = StatusPrompt.PromptS,
      DispensedVolume = 0.974,
      WithdrawnVolume = 0.959,
      VolumeUnits = VolumeUnit.Ml,
      Phase = new SyringePumpPhaseState
      {
        Number = 1,
        Function = SyringePumpFunction.Rat,
        Rate = 10.0,
        RateUnit = RateUnit.Mm,
        Volume = 5.0,
        VolumeUnit = VolumeUnit.Ml,
        Direction = Direction.Inf
      }
    };
  }

  public void SendCommand(byte[] command)
  {
    var commandText = DecodeCommand(command);
    if(string.IsNullOrWhiteSpace(commandText))
      return;

    lock(_gate)
    {
      TickVolumes();
      var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if(parts.Length == 0)
        return;

      if(parts[0].StartsWith("*", StringComparison.Ordinal))
        HandleBroadcast(parts);
      else
        HandleAddressed(parts);
    }
  }

  private void HandleBroadcast(string[] parts)
  {
    var command = parts[0][1..].ToUpperInvariant();
    if(command != SyringePumpFunction.Adr.ToProtocolString())
      return;

    if(parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var newAddress))
    {
      _state.Address = newAddress;
      SendResponse(_state.Address, _state.Status);
    }
    else
    {
      SendResponse(_state.Address, _state.Status, _state.Address.ToString("00", CultureInfo.InvariantCulture));
    }
  }

  private void HandleAddressed(string[] parts)
  {
    if(parts.Length < 2 ||
       !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var address) ||
       address != _state.Address)
      return;

    var command = parts[1].ToUpperInvariant();
    switch(command)
    {
      case "DIA":
        if(parts.Length >= 3 && TryParseFloat(parts[2], out var diameterMm))
        {
          _state.DiameterMm = diameterMm;
          SendResponse(_state.Address, _state.Status);
        }
        else
        {
          SendResponse(_state.Address, _state.Status, _state.DiameterMm.ToString("0.00", CultureInfo.InvariantCulture));
        }
        break;

      case "VER":
        SendResponse(_state.Address, _state.Status, _state.FirmwareVersion);
        break;

      case "PHN":
        if(parts.Length >= 3 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var phase))
        {
          _state.Phase.Number = phase;
          SendResponse(_state.Address, _state.Status);
        }
        else
        {
          SendResponse(_state.Address, _state.Status, _state.Phase.Number.ToString("00", CultureInfo.InvariantCulture));
        }
        break;

      case "FUN":
        if(parts.Length >= 3 && Enum.TryParse<SyringePumpFunction>(parts[2], true, out var function))
        {
          _state.Phase.Function = function;
          SendResponse(_state.Address, _state.Status);
        }
        else
        {
          SendResponse(_state.Address, _state.Status, _state.Phase.Function.ToProtocolString());
        }
        break;

      case "RAT":
        if(parts.Length >= 5 && TryParseFloat(parts[3], out var rate) && Enum.TryParse<RateUnit>(parts[4], true, out var rateUnit))
        {
          _state.Phase.Rate = rate;
          _state.Phase.RateUnit = rateUnit;
          SendResponse(_state.Address, _state.Status);
        }
        else
        {
          SendResponse(_state.Address, _state.Status, $"{_state.Phase.Rate:0.00}{_state.Phase.RateUnit.ToProtocolString()}");
        }
        break;

      case "VOL":
        if(parts.Length >= 3 && TryParseFloat(parts[2], out var volume))
        {
          _state.Phase.Volume = volume;
          _state.Phase.VolumeUnit = VolumeUnit.Ml;
          SendResponse(_state.Address, _state.Status);
        }
        else
        {
          SendResponse(_state.Address, _state.Status, $"{_state.Phase.Volume:0.000}{_state.Phase.VolumeUnit.ToProtocolString()}");
        }
        break;

      case "DIR":
        if(parts.Length >= 3 && Enum.TryParse<Direction>(parts[2], true, out var direction))
        {
          _state.Phase.Direction = direction switch
          {
            Direction.Rev => _state.Phase.Direction == Direction.Wdr ? Direction.Inf : Direction.Wdr,
            Direction.Stk => _state.Phase.Direction,
            _ => direction
          };
          SendResponse(_state.Address, _state.Status);
        }
        else
        {
          SendResponse(_state.Address, _state.Status, _state.Phase.Direction.ToProtocolString());
        }
        break;

      case "RUN":
        _state.Status = _state.Phase.Direction == Direction.Wdr ? StatusPrompt.PromptW : StatusPrompt.PromptI;
        SendResponse(_state.Address, _state.Status);
        break;

      case "PUR":
        _state.Status = StatusPrompt.PromptX;
        SendResponse(_state.Address, _state.Status);
        break;

      case "STP":
        _state.Status = StatusPrompt.PromptS;
        SendResponse(_state.Address, _state.Status);
        break;

      case "DIS":
        SendResponse(
          _state.Address,
          _state.Status,
          $"I{_state.DispensedVolume:0.000}W{_state.WithdrawnVolume:0.000}{_state.VolumeUnits.ToProtocolString()}");
        break;

      case "CLD":
        if(parts.Length >= 3 && Enum.TryParse<Direction>(parts[2], true, out var clearDirection))
        {
          if(clearDirection == Direction.Inf)
            _state.DispensedVolume = 0;
          else if(clearDirection == Direction.Wdr)
            _state.WithdrawnVolume = 0;
        }

        SendResponse(_state.Address, _state.Status);
        break;
    }
  }

  private void TickVolumes()
  {
    if(_state.Status == StatusPrompt.PromptI || _state.Status == StatusPrompt.PromptX)
      _state.DispensedVolume = Math.Round(_state.DispensedVolume + 0.025, 3);
    else if(_state.Status == StatusPrompt.PromptW)
      _state.WithdrawnVolume = Math.Round(_state.WithdrawnVolume + 0.025, 3);
  }

  private void SendResponse(int address, StatusPrompt status, string content = "")
  {
    var payload = $"{(char)SpecialAsciiCharacter.STX}{address:00}{GetStatusCode(status)}{content}{(char)SpecialAsciiCharacter.ETX}";
    _byteSender(Encoding.ASCII.GetBytes(payload));
  }

  private static string DecodeCommand(byte[] bytes)
  {
    if(bytes.Length >= 5 && bytes[0] == (byte)SpecialAsciiCharacter.STX && bytes[^1] == (byte)SpecialAsciiCharacter.ETX)
      return Encoding.ASCII.GetString(bytes[2..^3]).Trim();

    return Encoding.ASCII.GetString(bytes).Trim();
  }

  private static bool TryParseFloat(string value, out double result)
    => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

  private static char GetStatusCode(StatusPrompt status)
    => status switch
    {
      StatusPrompt.PromptI => 'I',
      StatusPrompt.PromptW => 'W',
      StatusPrompt.PromptS => 'S',
      StatusPrompt.PromptP => 'P',
      StatusPrompt.PromptT => 'T',
      StatusPrompt.PromptU => 'U',
      StatusPrompt.PromptX => 'X',
      _ => 'S'
    };
}
