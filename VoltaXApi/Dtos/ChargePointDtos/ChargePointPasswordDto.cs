namespace VoltaXApi.Dtos
{
    public class ChargePointPasswordSetDto
    {
        /// <summary>When true a strong random password is generated and Password is ignored.</summary>
        public bool Generate { get; set; }
        public string? Password { get; set; }
    }

    /// <summary>Returned once, right after the password is set. The stored value is a hash and cannot be read back.</summary>
    public class ChargePointPasswordResultDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
