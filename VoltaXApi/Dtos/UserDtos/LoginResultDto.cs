namespace VoltaXApi.Dtos;

public class LoginResultDto
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
}
