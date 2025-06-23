namespace VoltaXApi.Models;

public class Permission : IEntity
{
    public int ID { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}