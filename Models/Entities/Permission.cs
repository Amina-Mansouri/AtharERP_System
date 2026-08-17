using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "الوحدة")]
        public string Module { get; set; } = string.Empty;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        // العلاقة: الصلاحية تنتمي لأدوار متعددة
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}