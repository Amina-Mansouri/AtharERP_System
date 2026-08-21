using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectTeamMember
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Display(Name = "الدور في الفريق")]
        public TeamRole Role { get; set; } = TeamRole.Member;

        [Display(Name = "تاريخ الانضمام")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}