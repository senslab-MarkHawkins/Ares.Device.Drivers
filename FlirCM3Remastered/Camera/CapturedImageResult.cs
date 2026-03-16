namespace FlirCM3Remastered.Camera;

internal sealed class CapturedImageResult
{
  public required byte[] ImageData { get; init; }
  public required byte[] DisplayImageData { get; init; }
  public required string LatestImagePath { get; init; }
}
