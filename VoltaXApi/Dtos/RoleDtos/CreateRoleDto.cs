using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Dtos;

public class CreateRoleDto
{
    [Required, StringLength(64)]
    public string Name { get; set; }
    [MaxLength(500)]
    public List<int> Permissions { get; set; }
}