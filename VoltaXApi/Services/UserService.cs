

using System.Net.Http.Headers;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class UserService : IUserService
    {
        private readonly IFileManagementService _fileManagementService;
        private readonly IRepository<Image> _imageRepo;
        private readonly IUserRepository _userRepository;
      

        public UserService(
            IFileManagementService fileManagementService,
            IRepository<Image> imageRepo,
            IUserRepository userRepository
        ){
            _fileManagementService = fileManagementService;
            _imageRepo = imageRepo;
            _userRepository = userRepository;
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
    }
}