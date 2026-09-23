using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ProposalStatus
    {
        [Display(Name = "مُسلَّم")]
        Submitted = 1,

        [Display(Name = "معتمد")]
        Approved = 2,

        [Display(Name = "معتمد مع تعديل")]
        ApprovedWithModification = 3,

        [Display(Name = "إعادة تقديم")]
        Resubmission = 4,

        [Display(Name = "إعادة تصميم")]
        Redesign = 5
    }
}