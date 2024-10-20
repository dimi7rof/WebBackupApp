namespace WebBackUp.Models;

public class UserData
{
    public PhoneData Phone { get; set; } = new();
    public HddData HDD { get; set; } = new();
    public SdData SD { get; set; } = new();
}

public class PhoneData : BaseUserData { }

public class HddData : BaseUserData
{
    public string DeviceLetter { get; set; } = "F";
}

public class SdData : BaseUserData
{
    public string DeviceLetter { get; set; } = "F";
    public bool Sync { get; set; } = false;
}

public class BaseUserData
{
    public PathData Paths { get; set; } = new PathData();
}

public class PathData
{
    public List<string> SourcePaths { get; set; } = [];
    public List<string> DestinationPaths { get; set; } = [];
}
