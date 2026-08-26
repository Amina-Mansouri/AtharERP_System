// Models/Entities/SiteQualityType.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // اسمها "QualityType" في المواصفة، وأُضيفت بادئة "Site" لتفادي أي تعارض تسمية مستقبلي مع وحدات أخرى
    public enum SiteQualityType
    {
        [Display(Name = "جودة فنية")] Technical = 1,
        [Display(Name = "جودة موقع")] Site = 2,
        [Display(Name = "جودة مالية")] Financial = 3,
        [Display(Name = "جودة إدارية")] Administrative = 4
    }
}