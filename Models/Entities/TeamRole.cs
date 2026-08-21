using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum TeamRole
    {
        [Display(Name = "مدير المشروع")]
        ProjectManager = 1,

        [Display(Name = "مهندس رئيسي")]
        LeadEngineer = 2,

        [Display(Name = "مهندس")]
        Engineer = 3,

        [Display(Name = "عضو")]
        Member = 4
    }
}