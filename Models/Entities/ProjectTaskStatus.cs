using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    // ملاحظة: اسم هذا الـ enum في المواصفة هو "TaskStatus"، لكنه هنا "ProjectTaskStatus"
    // لتفادي التعارض مع System.Threading.Tasks.TaskStatus المستخدم ضمنياً في كل كود async
    public enum ProjectTaskStatus
    {
        [Display(Name = "لم تبدأ")]
        NotStarted = 1,

        [Display(Name = "قيد التنفيذ")]
        InProgress = 2,

        [Display(Name = "مكتملة")]
        Completed = 3,

        [Display(Name = "محظورة")]
        Blocked = 4
    }
}