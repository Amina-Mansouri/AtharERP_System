using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // العهدة (بند L6) — منفصلة تماماً عن طلبات صيانة معدات المواقع (٠٣).
    public class Custody
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العهدة مطلوب")]
        [StringLength(255)]
        [Display(Name = "العهدة")]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "بحوزة")]
        public string HolderId { get; set; } = string.Empty;

        [ForeignKey("HolderId")]
        public virtual ApplicationUser Holder { get; set; } = null!;

        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التسليم")]
        public DateTime HandedDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الإعادة")]
        public DateTime? ReturnedDate { get; set; }

        [Display(Name = "الحالة")]
        public CustodyStatus Status { get; set; } = CustodyStatus.WithEmployee;
    }
}