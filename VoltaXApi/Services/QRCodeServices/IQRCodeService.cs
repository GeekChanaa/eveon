namespace VoltaXApi.Services
{
    public interface IQRCodeService
    {
        byte[] GenerateQr(string text);

        /// <summary>A new QR value for a charge point, on this environment's API address.</summary>
        string NewChargePointQrValue();
    }
}
