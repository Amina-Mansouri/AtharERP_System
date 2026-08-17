using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum JobRank
    {
        // المسار الهندسي (Engineering Track)
        [Display(Name = "مهندس متدرب")]
        E0_TraineeEngineer = 0,

        [Display(Name = "مهندس مبتدئ")]
        E1_JuniorEngineer = 1,

        [Display(Name = "مهندس")]
        E2_Engineer = 2,

        [Display(Name = "مهندس أول / قائد فريق")]
        E3_SeniorLeadEngineer = 3,

        [Display(Name = "مهندس متخصص")]
        E4_SpecializedEngineer = 4,

        [Display(Name = "مدير هندسي")]
        E5_EngineeringManager = 5,

        // المسار المعماري (Architecture Track)
        [Display(Name = "معماري متدرب")]
        AI0_TraineeArchitect = 10,

        [Display(Name = "معماري مبتدئ")]
        AI1_JuniorArchitect = 11,

        [Display(Name = "معماري")]
        AI2_Architect = 12,

        [Display(Name = "معماري أول / قائد فريق")]
        AI3_SeniorLeadArchitect = 13,

        // المسار الإداري (Administrative Track)
        [Display(Name = "متدرب إداري")]
        M0_Trainee = 20,

        [Display(Name = "موظف")]
        M1_Employee = 21,

        [Display(Name = "مشرف")]
        M2_Supervisor = 22,

        [Display(Name = "مدير")]
        M3_Manager = 23,

        [Display(Name = "رئيس قسم")]
        M4_HeadOfDepartment = 24,

        [Display(Name = "الرئيس التنفيذي")]
        M5_CEO = 25,

        [Display(Name = "رئيس مجلس الإدارة")]
        M6_Chairman = 26
    }
}