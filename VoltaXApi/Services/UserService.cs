

using System.Net.Http.Headers;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Factories;
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

        public UserService(
            IFileManagementService fileManagementService,
            IRepository<Image> imageRepo,
            IUserRepository userRepository,
            IAuthService authService,
            ICardRepository cardRepository,
            IMailService mailService,
            IMailRequestFactory mailRequestFactory
        )
        {
            _fileManagementService = fileManagementService;
            _imageRepo = imageRepo;
            _userRepository = userRepository;
            _authService = authService;
            _cardRepository = cardRepository;
            _mailService = mailService;
            _mailRequestFactory = mailRequestFactory;
        }
        public async Task UploadUserAvatar(IFormFile file, int partnerID)
        {
            string folderName = "ProfilePictures/";
            string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            fileName = partnerID + "" + fileName.Substring(fileName.LastIndexOf("."), fileName.Length - fileName.LastIndexOf("."));
            this._fileManagementService.UploadFile(fileName, folderName, file);
            var fileExtension = Path.GetExtension(file.FileName);

            var newImage = new Image
            {
                Url = $"ProfilePictures/{fileName}",
                UploadDate = DateTime.UtcNow,
                Format = fileExtension,
                Priority = ImagePriorityEnum.Principal,
                IsActive = true,
                AltText = "User Image " + partnerID,
                Description = "none"
            };

            await _imageRepo.AddAsync(newImage);

            // Getting partner
            var partner = await _userRepository.GetByIdAsync(partnerID);
            partner.ImageID = newImage.ID;
            await _userRepository.Update(partner);
        }

        public async Task<int> CreateUserDashboard(UserDashboardCreateDto userToCreate)
        {
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

            if (user.SuspendedAt != userDto.SuspendedAt && user.SuspendedAt < userDto.SuspendedAt)
            {
                await SuspendUser(user, user.SuspendedAt, user.SuspensionReason);
            }

            await _userRepository.EditUserDashboardInformations(userID, userDto);
        }

    }
}