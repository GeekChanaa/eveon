

using System.Net.Http.Headers;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class UserService : IUserService
    {
        private readonly IFileManagementService _fileManagementService;
        private readonly IRepository<Image> _imageRepo;
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;

        public UserService(
            IFileManagementService fileManagementService,
            IRepository<Image> imageRepo,
            IUserRepository userRepository,
            IAuthService authService
        ){
            _fileManagementService = fileManagementService;
            _imageRepo = imageRepo;
            _userRepository = userRepository;
            _authService = authService;
        }
        public async Task UploadUserAvatar(IFormFile file, int partnerID)
        {
            string folderName = "ProfilePictures/";
            string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            fileName = partnerID +""+ fileName.Substring(fileName.LastIndexOf("."),fileName.Length - fileName.LastIndexOf("."));
            this._fileManagementService.UploadFile(fileName, folderName, file);
            var fileExtension = Path.GetExtension(file.FileName);

            var newImage = new Image
            {
                Url = $"ProfilePictures/{fileName}",
                UploadDate = DateTime.UtcNow,
                Format = fileExtension,
                Priority = ImagePriorityEnum.Principal,
                IsActive = true,
                AltText = "User Image "+partnerID,
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
            _authService.CreatePasswordHash(userToCreate.Password, out byte[] passwordHash, out byte[] passwordSalt);
            User user = new User{
                FirstName = userToCreate.FirstName,
                LastName = userToCreate.LastName,
                Email = userToCreate.Email,
                Gender = userToCreate.Gender,
                City = userToCreate.City,
                Car = userToCreate.Car,
                Birthday = userToCreate.Birthday,
                Phone = userToCreate.Phone,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                PartnerID = userToCreate.PartnerID,
                IsEmailVerified = userToCreate.IsEmailVerified,
                IsPhoneNumberVerified = userToCreate.IsPhoneNumberVerified,
                Role = userToCreate.Role,
            };
            await _userRepository.AddAsync(user);
            return user.ID;
        }

    }
}