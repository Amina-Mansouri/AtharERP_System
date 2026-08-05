using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العميل مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم العميل")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "اسم الشركة")]
        public string? CompanyName { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

        [StringLength(200)]
        [EmailAddress]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [StringLength(300)]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [StringLength(50)]
        [Display(Name = "الرقم الضريبي")]
        public string? TaxNumber { get; set; }

        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}