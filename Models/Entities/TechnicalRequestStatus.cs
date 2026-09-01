using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum TechnicalRequestStatus
    {
        [Display(Name = "مفتوح")]
        Open = 1,

        [Display(Name = "يحتاج توضيح")]
        NeedsClarification = 2,

        [Display(Name = "معتمد")]
        Approved = 3,

        [Display(Name = "مغلق")]
        Closed = 4
    }
}