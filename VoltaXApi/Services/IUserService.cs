
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public interface IUserService
    {
        Task UploadUserAvatar(IFormFile file, int partnerID);
        Task<int> CreateUserDashboard(UserDashboardCreateDto userToCreate);
        Task<bool> UpdateEmail(UpdateUserEmailDto user);
        Task<bool> UpdatePhone(UpdateUserPhoneDto user);
        Task SuspendUser(User user, DateTime? suspendedAt, string suspensionReason = null);
        Task EditUserDashboardInformations(int userID, UserDashboardEditInformationsDto userDto);

    }
}