using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using VoltaXApi.Helpers;

namespace VoltaXApi.Services;

public class SnsService : ISnsService
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _topicArn;

    public SnsService(IOptions<AwsSnsOptions> options)
    {
        var snsOptions = options.Value;

        _topicArn = snsOptions.TopicArn;

        var config = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(snsOptions.Region)
        };

        _snsClient = new AmazonSimpleNotificationServiceClient(
            snsOptions.AccessKey,
            snsOptions.SecretKey,
            config
        );
    }

    public async Task PublishMessageAsync(string subject, string message)
    {
        var request = new PublishRequest
        {
            TopicArn = _topicArn,
            Subject = subject,
            Message = message
        };

        await _snsClient.PublishAsync(request);
    }
    
    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        var request = new PublishRequest
        {
            Message = message,
            PhoneNumber = phoneNumber
        };

        await _snsClient.PublishAsync(request);
    }
}
