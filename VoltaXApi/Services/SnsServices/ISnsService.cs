namespace VoltaXApi.Services;

public interface ISnsService
{
    Task PublishMessageAsync(string subject, string message);
    Task SendSmsAsync(string phoneNumber, string message);
}
