namespace VoltaXApi.Services
{
    public interface IQRCodeService
    {
        byte[] GenerateQr(string text);
    }
}
