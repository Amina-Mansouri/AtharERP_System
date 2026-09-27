using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    // طلب إعادة تصميم من الزبون بعد اعتماد المرحلة — يُسجَّل يدوياً، كل تسجيل يحمل نسبة زيادته الخاصة
    public class ClientRedesignRequest
    {
        public int Id { get; set; }

        [Required]
        public int ProjectStageId { get; set; }

        [ForeignKey("ProjectStageId")]
        [ValidateNever]
        public virtual ProjectStage ProjectStage { get; set; } = null!;

        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الزيادة")]
        public decimal IncreasePercentage { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}