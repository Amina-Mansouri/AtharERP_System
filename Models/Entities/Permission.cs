using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        // العلاقة: الصلاحية تنتمي لأدوار متعددة
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}