

using System.Net.Http.Headers;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Factories;
using VoltaXApi.Exceptions;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class UserService : IUserService
    {
        private readonly IFileManagementService _fileManagementService;
        private readonly IRepository<Image> _imageRepo;
        private readonly IUserRepository _userRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IAuthService _authService;
        private readonly IMailService _mailService;
        private readonly IMailRequestFactory _mailRequestFactory;
        private readonly IRefreshTokenService _refreshTokenService;

        public UserService(
            IFileManagementService fileManagementService,
            IRepository<Image> imageRepo,
            IUserRepository userRepository,
            IAuthService authService,
            ICardRepository cardRepository,
            IMailService mailService,
            IMailRequestFactory mailRequestFactory,
            IRefreshTokenService refreshTokenService
        )
        {
            _refreshTokenService = refreshTokenService;
            _fileManagementService = fileManagementService;
            _imageRepo = imageRepo;
            _userRepository = userRepository;
            _authService = authService;
            _cardRepository = cardRepository;
            _mailService = mailService;
            _mailRequestFactory = mailRequestFactory;
        }
        public async Task UploadUserAvatar(IFormFile file, int userID)
        {
            var user = await _userRepository.GetByIdAsync(userID)
                ?? throw new NotFoundException("User not found.");

            // Random file name, linked from the user's Image row: avatar URLs cannot be guessed from user ids.
            var stored = await _fileManagementService.SaveImageAsync(file, "ProfilePictures");

            var newImage = new Image
            {
                Url = stored.RelativeUrl,
                UploadDate = DateTime.UtcNow,
                Format = stored.Extension,
                Priority = ImagePriorityEnum.Principal,
                IsActive = true,
                AltText = "User Image",
                Description = "none"
            };

            await _imageRepo.AddAsync(newImage);

            user.ImageID = newImage.ID;
            await _userRepository.Update(user);
        }

        public async Task<int> CreateUserDashboard(UserDashboardCreateDto userToCreate)
        {
            if (await _userRepository.PhoneExists(userToCreate.Phone))
            {
                throw new ValidationException("This phone number is already used by another account");
            }

            AuthHelper.CreatePasswordHash(userToCreate.Password, out byte[] passwordHash, out byte[] passwordSalt);
            User user = new User
            {
                FirstName = userToCreate.FirstName,
                LastName = userToCreate.LastName,
                Email = userToCreate.Email,
                Gender = userToCreate.Gender,
                City = userToCreate.City,
                Birthday = userToCreate.Birthday,
                Phone = userToCreate.Phone,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PartnerID = userToCreate.PartnerID,
                IsEmailVerified = userToCreate.IsEmailVerified,
                IsPhoneNumberVerified = userToCreate.IsPhoneNumberVerified,
                RoleID = userToCreate.RoleID,
                ElectricVehicleModelID = userToCreate.ElectricVehicleModelID
            };
            await _userRepository.AddAsync(user);
            await _cardRepository.CreateCardForUser(user);
            return user.ID;
        }

        public async Task<bool> UpdateEmail(UpdateUserEmailDto user)
        {
            var userToUpdate = await this._userRepository.GetByIdAsync(user.ID);
            userToUpdate.Email = user.Email;
            userToUpdate.IsEmailVerified = false;
            await this._userRepository.Update(userToUpdate);
            await this._authService.CreateEmailVerificationToken(user.ID);
            var mailRequest = _mailRequestFactory.CreateChangedEmailMailRequest(userToUpdate.Email);
            await this._mailService.SendPasswordChangedMail(mailRequest, userToUpdate.FirstName);
            return true;
        }
        public async Task<bool> UpdatePhone(UpdateUserPhoneDto user)
        {
            var userToUpdate = await this._userRepository.GetByIdAsync(user.ID);
            var userDto = new AddPhoneNumberDto
            {
                Phone = user.Phone,
                Email = userToUpdate.Email
            };
            await this._authService.SendPhoneVerificationToken(userDto);
            return true;
        }
        
        public async Task SuspendUser(User user, DateTime? suspendedAt, string suspensionReason = null)
        {
            var suspendedAccountDto = new UserSuspendedForMailDto
            {
                UserName = user.FullName,
                SuspensionReason = user.SuspensionReason,
                SuspendedAt = user.SuspendedAt 
            };

            // Send suspension email
            var mailRequest = _mailRequestFactory.CreateAccountSuspendedMailRequest(user.Email);
            await _mailService.SendSuspendedAccountMail(mailRequest,suspendedAccountDto);
        }
        
        public async Task EditUserDashboardInformations(int userID, UserDashboardEditInformationsDto userDto)
        {
            var user = await _userRepository.GetByIdAsync(userID);

            var now = DateTime.UtcNow;
            var newlySuspended = userDto.SuspendedAt > now && userDto.SuspendedAt != user.SuspendedAt;

            await _userRepository.EditUserDashboardInformations(userID, userDto);

            // Suspension ends every session: refresh tokens are revoked here and the next
            // request with a still valid access token is refused by AccessService.
            if (newlySuspended)
            {
                await _refreshTokenService.RevokeAllForUser(userID, "suspended");
                await SuspendUser(user, user.SuspendedAt, user.SuspensionReason);
            }
        }

    }
}