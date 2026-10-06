

using VoltaXApi.Data;
using VoltaXApi.Exceptions;
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
        var partner = await _partnerRepository.GetByIdAsync(partnerID)
            ?? throw new NotFoundException("Partner not found.");
        var stored = await _fileManagementService.SaveImageAsync(file, "PartnersLogos");

        var newImage = new Image
        {
            Url = stored.RelativeUrl,
            UploadDate = DateTime.UtcNow,
            Format = stored.Extension,
            Priority = ImagePriorityEnum.Principal,
            IsActive = true,
            AltText = "Partner Image "+partnerID,
            Description = "none"
        };

        await _imageRepo.AddAsync(newImage);

        partner.ImageID = newImage.ID;
        await _partnerRepository.Update(partner);
    }
}