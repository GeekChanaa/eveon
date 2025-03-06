
namespace VoltaXApi.Services
{
    public interface IUserService
    {
        Task UploadUserAvatar(IFormFile file, int partnerID);
    }
}