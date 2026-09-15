using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class JobRank
    {
        public int Id { get; set; }

        [Display(Name = "المسار")]
        public int CareerTrackId { get; set; }

        [ForeignKey("CareerTrackId")]
        public virtual CareerTrack CareerTrack { get; set; } = null!;

        [Required, StringLength(10)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "اسم الرتبة")]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "الاسم الإنجليزي")]
        public string? NameEn { get; set; }

        [Display(Name = "ترتيب العرض")]
        public int DisplayOrder { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الراتب الأساسي")]
        public decimal? BaseSalary { get; set; }
    }
}