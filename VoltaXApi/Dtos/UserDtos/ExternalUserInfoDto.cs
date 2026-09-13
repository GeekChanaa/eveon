namespace VoltaXApi.Dtos
{
    /// <summary>
    /// Provider agnostic view of the identity returned by an external OAuth provider.
    /// </summary>
    public class ExternalUserInfoDto
    {
        public string ProviderKey { get; set; }
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PictureUrl { get; set; }
        public bool EmailVerified { get; set; }
    }
}
