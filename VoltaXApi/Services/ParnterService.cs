

using System.Net.Http.Headers;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services;

public class PartnerService : IPartnerService
{
    private readonly IFileManagementService _fileManagementService;
    private readonly IPartnerRepository _partnerRepository;
    private readonly IRepository<Image> _imageRepo;
    public PartnerService(
        IFileManagementService fileManagementService,
        IPartnerRepository partnerRepository,
        IRepository<Image> imageRepo
    ){
        _fileManagementService = fileManagementService;
        _partnerRepository = partnerRepository;
        _imageRepo = imageRepo;
    }

    public async Task UploadPartnerLogo(IFormFile file, int partnerID)
    {
        string folderName = "PartnersLogos/";
        string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
        fileName = partnerID +""+ fileName.Substring(fileName.LastIndexOf("."),fileName.Length - fileName.LastIndexOf("."));
        this._fileManagementService.UploadFile(fileName, folderName, file);
        var fileExtension = Path.GetExtension(file.FileName);

        var newImage = new Image
        {
            Url = $"PartnersLogos/{fileName}",
            UploadDate = DateTime.UtcNow,
            Format = fileExtension,
            Priority = ImagePriorityEnum.Principal,
            IsActive = true,
            AltText = "Partner Image "+partnerID,
            Description = "none"
        };

        await _imageRepo.AddAsync(newImage);

        // Getting partner
        var partner = await _partnerRepository.GetByIdAsync(partnerID);
        partner.ImageID = newImage.ID;
        await _partnerRepository.Update(partner);
    }
}