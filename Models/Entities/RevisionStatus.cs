using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum RevisionStatus
    {
        [Display(Name = "معتمد")]
        Approved = 1,

        [Display(Name = "بانتظار")]
        Pending = 2,

        [Display(Name = "مؤرشف")]
        Archived = 3
    }
}