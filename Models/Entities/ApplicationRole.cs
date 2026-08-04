using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class ApplicationRole : IdentityRole
    {
        [Display(Name = "الوصف")]
        [StringLength(250)]
        public string? Description { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "قالب جاهز")]
        public bool IsTemplate { get; set; } = false;

        // العلاقة: الدور يملك صلاحيات متعددة
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}