using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class CareerTrack
    {
        public int Id { get; set; }

        [Required, StringLength(10)]
        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "اسم المسار")]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "الاسم الإنجليزي")]
        public string? NameEn { get; set; }

        [Display(Name = "ترتيب العرض")]
        public int DisplayOrder { get; set; }

        public virtual ICollection<JobRank> Ranks { get; set; } = new List<JobRank>();
    }
}