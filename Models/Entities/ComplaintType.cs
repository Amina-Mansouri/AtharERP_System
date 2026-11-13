using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ComplaintType
    {
        [Display(Name = "شكوى إدارية")]
        Administrative = 1,

        [Display(Name = "اعتراض على قرار إداري")]
        ObjectionToDecision = 2,

        [Display(Name = "تظلم وظيفي")]
        JobGrievance = 3,

        [Display(Name = "شكوى بشأن التعامل المهني")]
        ProfessionalConduct = 4,

        [Display(Name = "اعتراض على إجراءات أو تعليمات العمل")]
        ObjectionToProcedures = 5,

        [Display(Name = "أخرى")]
        Other = 6
    }
}