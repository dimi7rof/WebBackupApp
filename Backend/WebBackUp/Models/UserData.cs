namespace WebBackUp.Models;

internal class UserData
{
    public PhoneData Phone { get; set; } = new();
    public HddData HDD { get; set; } = new();
    public SdData SD { get; set; } = new();
}

internal class PhoneData : BaseUserData { }

internal class HddData : BaseUserData
{
    public string DeviceLetter { get; set; } = "F";
}

internal class SdData : BaseUserData
{
    public string DeviceLetter { get; set; } = "F";
}

internal class BaseUserData
{
    public PathData Paths { get; set; } = new PathData();
}

internal class PathData
{
    public List<string> SourcePaths { get; set; } = [];
    public List<string> DestinationPaths { get; set; } = [];
}
