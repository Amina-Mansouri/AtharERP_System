using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // صلاحية إضافية يمنحها المدير لموظف بعينه، فوق صلاحيات دوره (اتحاد Union وليس استبدالاً)
    public class UserPermission
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public int PermissionId { get; set; }

        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;

        [Display(Name = "تاريخ المنح")]
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }
}