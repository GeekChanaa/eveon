

namespace VoltaXApi.Services;

public interface IPartnerService
{
    Task UploadPartnerLogo(IFormFile file, int partnerID);
}