// Models/Entities/SiteDocumentType.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum SiteDocumentType
    {
        [Display(Name = "خريطة معتمدة")] ApprovedMap = 1,
        [Display(Name = "عقد مقاول")] ContractorContract = 2,
        [Display(Name = "تقرير جودة")] QualityReport = 3,
        [Display(Name = "تقرير سلامة")] SafetyReport = 4,
        [Display(Name = "تقرير يومي")] DailyReport = 5,
        [Display(Name = "أخرى")] Other = 6
    }
}