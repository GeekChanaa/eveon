

namespace VoltaXApi.Services;

public interface IPartnerService
{
    Task UploadPartnerLogo(IFormFile file, int partnerID);
    /// <summary>Creates the partner's sign in account (role Partner) on the partner's email and mails its password to the administrators.</summary>
    Task<VoltaXApi.Dtos.PartnerUserCreatedDto> CreatePartnerUser(int partnerID);
}