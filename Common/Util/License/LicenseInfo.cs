namespace March7thHoney.Util.License;

public class LicenseInfo
{
    public string Hwid { get; set; } = "";
    public long IssuedAt { get; set; }
    public int TrialDays { get; set; } = 30;
    public long ExpireAt { get; set; }
    public string Holder { get; set; } = "";
}

