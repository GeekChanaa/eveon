namespace VoltaXApi.Helpers;

public class WarningInterval
{
    public string Name { get; set; }
    public int DaysBeforeExpiration { get; set; }
    public bool Enabled { get; set; } = true;
}