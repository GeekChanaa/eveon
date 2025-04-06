namespace VoltaXApi.Helpers;

public class CardExpirationSettings
{
    public List<WarningInterval> WarningIntervals { get; set; } = new List<WarningInterval>();
    public int DefaultCardValidityYears { get; set; } = 5;
    public TimeSpan CheckTime { get; set; } = TimeSpan.Parse("09:00:00");
}