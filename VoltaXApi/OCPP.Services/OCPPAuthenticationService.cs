using System.Security.Cryptography.X509Certificates;
using VoltaXApi.Models;

public class AuthenticationService
{
    public bool AuthenticateChargePoint(HttpContext context, ChargePoint chargePoint)
    {
        if (!string.IsNullOrWhiteSpace(chargePoint.Username))
        {
            return AuthenticateBasic(context, chargePoint);
        }
        else if (!string.IsNullOrWhiteSpace(chargePoint.ClientCertThumb))
        {
            return AuthenticateCertificate(context, chargePoint);
        }

        return false;
    }

    private bool AuthenticateBasic(HttpContext context, ChargePoint chargePoint)
    {
        string authHeader = context.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authHeader))
        {
            string[] cred = System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(authHeader.Substring(6))).Split(':');
            return cred.Length == 2 && chargePoint.Username == cred[0] && chargePoint.Password == cred[1];
        }
        return false;
    }

    private bool AuthenticateCertificate(HttpContext context, ChargePoint chargePoint)
    {
        X509Certificate2 clientCert = context.Connection.ClientCertificate;
        return clientCert != null && clientCert.Thumbprint.Equals(chargePoint.ClientCertThumb, StringComparison.InvariantCultureIgnoreCase);
    }
}
