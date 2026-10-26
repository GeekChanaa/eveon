

using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Exceptions;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Services;

public class PartnerService : IPartnerService
{
    private readonly IFileManagementService _fileManagementService;
    private readonly IPartnerRepository _partnerRepository;
    private readonly IRepository<Image> _imageRepo;
    private readonly VoltaXApiDbContext _db;
    private readonly IMailService _mailService;
    private readonly IAuditLogger _audit;
    private readonly VoltaxApi.Helpers.SupportEmails _supportEmails;
    public PartnerService(
        IFileManagementService fileManagementService,
        IPartnerRepository partnerRepository,
        IRepository<Image> imageRepo,
        VoltaXApiDbContext db,
        IMailService mailService,
        IAuditLogger audit,
        Microsoft.Extensions.Options.IOptions<VoltaxApi.Helpers.SupportEmails> supportEmails
    ){
        _fileManagementService = fileManagementService;
        _partnerRepository = partnerRepository;
        _imageRepo = imageRepo;
        _db = db;
        _mailService = mailService;
        _audit = audit;
        _supportEmails = supportEmails.Value;
    }

    public async Task<PartnerUserCreatedDto> CreatePartnerUser(int partnerID)
    {
        var partner = await _db.Partners.AsNoTracking().SingleOrDefaultAsync(p => p.ID == partnerID && !p.IsDeleted)
            ?? throw new NotFoundException("Partner not found.");
        // Sign in looks accounts up by the lower case email.
        var email = partner.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email))
            throw new ValidationException("The partner has no email address.");

        // Emails are unique across every account, deleted ones included.
        var existing = await _db.Users.IgnoreQueryFilters().Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
            throw new ValidationException(existing.PartnerID == partnerID && existing.Role.Name == "Partner" && !existing.IsDeleted
                ? $"{email} already has a partner account for this partner."
                : $"{email} is already used by another account.");

        // The password is only ever sent to the administrators: without one to receive it, nobody could sign in.
        var now = DateTime.UtcNow;
        var recipients = await _db.Users
            .Where(u => u.Role.Name == "Admin" && !u.IsDeleted && u.IsEmailVerified)
            .Where(User.NotSuspendedAt(now))
            .Select(u => u.Email)
            .ToListAsync();
        if (!string.IsNullOrWhiteSpace(_supportEmails.Admin)) recipients.Add(_supportEmails.Admin);
        recipients = recipients.Select(e => e.Trim().ToLowerInvariant()).Distinct().ToList();
        if (recipients.Count == 0)
            throw new ValidationException("No administrator email address to send the password to.");

        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Name == "Partner" && !r.IsDeleted)
            ?? throw new ValidationException("The Partner role does not exist.");

        var password = AuthHelper.GenerateRandomPassword();
        AuthHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
        var user = new User
        {
            FirstName = partner.Name,
            LastName = "",
            Email = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            PartnerID = partner.ID,
            RoleID = role.ID,
            // The administrator chose the partner's own address, so there is nothing to verify.
            IsEmailVerified = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Partner.CreatePartnerUser", "User", user.ID.ToString(), new { partnerID, email });

        await _mailService.SendPartnerAccountCreatedMail(recipients, partner.Name, email, password);

        return new PartnerUserCreatedDto { UserID = user.ID, Email = email, Recipients = recipients.Count };
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