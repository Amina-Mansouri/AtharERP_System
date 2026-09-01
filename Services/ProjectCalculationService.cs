using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    public class ProjectCalculationService
    {
        private readonly AppDbContext _context;

        public ProjectCalculationService(AppDbContext context)
        {
            _context = context;
        }

        // نسبة إنجاز المرحلة = مجموع قيمة التكليفات المكتملة ÷ مجموع قيمة كل التكليفات × 100
        // (بعد 06-CONFLICTS.md · C6: ProjectAssignment حلّ محل ProjectStep في مسار الحساب.
        // FinalAmount بديل مؤقت لوزن الخطوة القديم — الحساب التصاعدي الكامل عبر
        // CompletionPercentage خاص بالتكليف نفسه يُبنى في المرحلة ٤ حسب 04-BACKEND.md §8)
        public async Task RecalculateStageAsync(int stageId)
        {
            var stage = await _context.ProjectStages
                .Include(s => s.Assignments)
                .FirstOrDefaultAsync(s => s.Id == stageId);

            if (stage == null) return;

            var totalValue = stage.Assignments.Sum(a => a.FinalAmount);
            var completedValue = stage.Assignments
                .Where(a => a.Status == AssignmentStatus.Completed)
                .Sum(a => a.FinalAmount);

            stage.CompletionPercentage = totalValue > 0
                ? Math.Round((completedValue / totalValue) * 100, 2)
                : 0;

            // تجميع التكلفة الفعلية للمرحلة من مجموع القيم النهائية لتكليفاتها
            stage.ActualCost = stage.Assignments.Sum(a => a.FinalAmount);

            await _context.SaveChangesAsync();
            await RecalculateProjectAsync(stage.ProjectId);
        }

        // نسبة إنجاز المشروع = مجموع (وزن المرحلة × نسبة إنجازها) ÷ مجموع الأوزان (القسم 5.3)
        public async Task RecalculateProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Stages)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return;

            var totalWeight = project.Stages.Sum(s => s.Weight);
            var weightedSum = project.Stages.Sum(s => s.Weight * s.CompletionPercentage);

            project.CompletionPercentage = totalWeight > 0
                ? Math.Round(weightedSum / totalWeight, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        // نسبة إنجاز المهمة = بنود To-Do المكتملة ÷ إجمالي البنود × 100 (القسم 5.6)
        public async Task RecalculateTaskCompletionAsync(int taskId)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Todos)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            var totalTodos = task.Todos.Count;
            var completedTodos = task.Todos.Count(t => t.IsCompleted);

            task.CompletionPercentage = totalTodos > 0
                ? Math.Round((decimal)completedTodos / totalTodos * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
        }

        // حساب أيام التأخير/التبكير عند التسليم الفعلي (القسم 5.4/5.5)
        public void UpdateDeliveryMetrics(ProjectTask task)
        {
            if (task.ActualDeliveryDate == null || task.PlannedEndDate == null)
            {
                task.DelayDays = 0;
                task.EarlyDeliveryDays = 0;
                return;
            }

            var diff = (task.ActualDeliveryDate.Value.Date - task.PlannedEndDate.Value.Date).Days;

            if (diff > 0)
            {
                task.DelayDays = diff;
                task.EarlyDeliveryDays = 0;
            }
            else if (diff < 0)
            {
                task.DelayDays = 0;
                task.EarlyDeliveryDays = -diff;
            }
            else
            {
                task.DelayDays = 0;
                task.EarlyDeliveryDays = 0;
            }
        }
    }
}