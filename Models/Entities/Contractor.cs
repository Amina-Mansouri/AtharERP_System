using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // حساب دخول المقاول — هوية واحدة يمكن ربطها بعدة مواقع عبر SiteContractor
    public class Contractor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المقاول مطلوب")]
        [StringLength(255)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "اسم الشركة")]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [StringLength(255)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SiteContractor> SiteAssignments { get; set; } = new List<SiteContractor>();
    }
}