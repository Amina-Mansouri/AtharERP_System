using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ProposalStatus
    {
        [Display(Name = "مُسلَّم")]
        Submitted = 1,

        [Display(Name = "معتمد")]
        Approved = 2,

        [Display(Name = "مرفوض")]
        Rejected = 3
    }
}