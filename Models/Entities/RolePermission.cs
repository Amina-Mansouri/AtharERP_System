using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoleId { get; set; } = string.Empty;

        [Required]
        public int PermissionId { get; set; }

        [Display(Name = "ممنوحة")]
        public bool IsGranted { get; set; } = true;

        // خصائص التنقل
        [ForeignKey("RoleId")]
        public virtual ApplicationRole Role { get; set; } = null!;

        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
    }
}