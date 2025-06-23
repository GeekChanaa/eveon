
namespace VoltaXApi.Helpers;

public class AwsSnsOptions
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Region { get; set; }
    public string TopicArn { get; set; }
}
