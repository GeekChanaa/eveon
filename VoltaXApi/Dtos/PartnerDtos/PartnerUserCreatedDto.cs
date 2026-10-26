namespace VoltaXApi.Dtos;

/// <summary>The partner account created from a partner's email. The password is only sent to the administrators by email.</summary>
public class PartnerUserCreatedDto
{
    public int UserID { get; set; }
    public string Email { get; set; } = null!;
    public int Recipients { get; set; }
}
