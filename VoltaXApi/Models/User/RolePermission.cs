
namespace VoltaXApi.Models;

public class RolePermission : IEntity
{
    public int ID { get; set; }
    public int RoleID { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionID { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
