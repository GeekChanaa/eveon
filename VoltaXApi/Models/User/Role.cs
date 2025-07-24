
namespace VoltaXApi.Models;

public class Role : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}