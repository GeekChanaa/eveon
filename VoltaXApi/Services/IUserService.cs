
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IUserService
    {
        Task UploadUserAvatar(IFormFile file, int partnerID);
        Task<int> CreateUserDashboard(UserDashboardCreateDto userToCreate);
        Task<bool> UpdateEmail(UpdateUserEmailDto user);
        Task<bool> UpdatePhone(UpdateUserPhoneDto user);
    }
}