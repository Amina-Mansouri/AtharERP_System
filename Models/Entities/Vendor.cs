using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class Vendor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المورّد مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المورّد")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "جهة التواصل")]
        public string? ContactName { get; set; }

        [StringLength(50)]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}