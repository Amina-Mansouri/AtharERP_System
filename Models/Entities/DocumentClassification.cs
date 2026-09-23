using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum DocumentClassification
    {
        [Display(Name = "معماري")]
        AA = 1,

        [Display(Name = "ديكور داخلي")]
        IN = 2,

        [Display(Name = "إنشائي")]
        SE = 3,

        [Display(Name = "ميكانيكي")]
        ME = 4,

        [Display(Name = "كهربائي")]
        EE = 5
    }
}