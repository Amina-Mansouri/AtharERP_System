using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    public class ProjectCalculationService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notify;

        public ProjectCalculationService(AppDbContext context, NotificationService notify)
        {
            _context = context;
            _notify = notify;
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

            var wasCompleted = stage.Status == StageStatus.Completed;

            var totalValue = stage.Assignments.Sum(a => a.FinalAmount);
            var completedValue = stage.Assignments
                .Where(a => a.Status == AssignmentStatus.Completed)
                .Sum(a => a.FinalAmount);

            stage.CompletionPercentage = totalValue > 0
                ? Math.Round((completedValue / totalValue) * 100, 2)
                : 0;

            stage.ActualCost = stage.Assignments.Sum(a => a.FinalAmount);

            var allStages = await _context.ProjectStages.Where(s => s.ProjectId == stage.ProjectId).ToListAsync();
            ApplyAutomaticStageStatus(stage, allStages);

            await _context.SaveChangesAsync();
            await RecalculateProjectAsync(stage.ProjectId);

            if (!wasCompleted && stage.Status == StageStatus.Completed)
            {
                var teamIds = await _context.ProjectTeamMembers
                    .Where(tm => tm.ProjectId == stage.ProjectId)
                    .Select(tm => tm.UserId)
                    .ToListAsync();
                var adminIds = await (from ur in _context.UserRoles
                                      join r in _context.Roles on ur.RoleId equals r.Id
                                      where r.Name == "مدير النظام"
                                      select ur.UserId)
                                      .Distinct()
                                      .ToListAsync();
                var recipients = teamIds.Union(adminIds).Distinct();

                await _notify.NotifyManyAsync(recipients, $"اكتملت المرحلة: {stage.Name}", NotificationEventType.StageCompleted, $"/Projects/Details/{stage.ProjectId}", entityType: "ProjectStage", entityId: stage.Id);
            }
        }

        // الحالة التلقائية الكاملة للمرحلة — لا تدخّل يدوي إطلاقاً
        public void ApplyAutomaticStageStatus(ProjectStage stage, IEnumerable<ProjectStage> allProjectStages)
        {
            if (stage.CompletionPercentage >= 100)
            {
                stage.Status = StageStatus.Completed;
                return;
            }

            if (stage.PlannedEndDate.HasValue && DateTime.UtcNow.Date > stage.PlannedEndDate.Value.Date)
            {
                stage.Status = StageStatus.Delayed;
                return;
            }

            var priorStagesCompleted = allProjectStages
                .Where(s => s.Id != stage.Id && s.Sequence < stage.Sequence)
                .All(s => s.Status == StageStatus.Completed);

            stage.Status = (stage.PlannedStartDate.HasValue && priorStagesCompleted)
                ? StageStatus.InProgress
                : StageStatus.New;
        }

        // إكمال تلقائي: أول بند to-do يُضاف لأي مهمة تابعة للتكليف ينقله من "معلّق" إلى "قيد التنفيذ"
        public async Task MarkAssignmentInProgressAsync(int assignmentId)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(assignmentId);
            if (assignment != null && assignment.Status == AssignmentStatus.Pending)
            {
                assignment.Status = AssignmentStatus.InProgress;
                await _context.SaveChangesAsync();
            }
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

            // إكمال تلقائي (بند حالة المشروع): تصل نسبة الإنجاز الكلية 100% وليس ملغى أو متوقفاً مؤقتاً
            if (project.CompletionPercentage >= 100 && project.Status != ProjectStatus.Cancelled && project.Status != ProjectStatus.OnHold)
            {
                project.Status = ProjectStatus.Completed;
            }

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

            var oldStatus = task.Status;

            if (task.Status != ProjectTaskStatus.Blocked)
            {
                task.Status = task.CompletionPercentage >= 100
                    ? ProjectTaskStatus.Completed
                    : task.CompletionPercentage > 0
                        ? ProjectTaskStatus.InProgress
                        : ProjectTaskStatus.NotStarted;
            }

            await _context.SaveChangesAsync();

            if (task.Status != oldStatus)
            {
                var pmIds = await _context.ProjectTeamMembers
                    .Where(tm => tm.ProjectId == task.ProjectId && tm.Role == TeamRole.ProjectManager)
                    .Select(tm => tm.UserId)
                    .ToListAsync();

                if (pmIds.Count > 0)
                {
                    var statusLabel = task.Status switch
                    {
                        ProjectTaskStatus.NotStarted => "لم تبدأ",
                        ProjectTaskStatus.InProgress => "قيد التنفيذ",
                        ProjectTaskStatus.Completed => "مكتملة",
                        ProjectTaskStatus.Blocked => "محظورة",
                        _ => task.Status.ToString()
                    };
                    await _notify.NotifyManyAsync(pmIds, $"تغيّرت حالة المهمة \"{task.Title}\" إلى: {statusLabel}", NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}", entityType: "ProjectTask", entityId: task.Id);
                }
            }

            if (task.ProjectAssignmentId.HasValue)
            {
                await RecalculateAssignmentCompletionAsync(task.ProjectAssignmentId.Value);
            }
        }

        // إكمال تلقائي للتكليف عندما تكتمل كل مهامه (بنود to-do) 100% — ثم تصعيد الحساب للمرحلة
        private async Task RecalculateAssignmentCompletionAsync(int assignmentId)
        {
            var assignment = await _context.ProjectAssignments
                .Include(a => a.Tasks)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null || !assignment.Tasks.Any())
                return;

            var allTasksDone = assignment.Tasks.All(t => t.CompletionPercentage >= 100);

            if (allTasksDone && assignment.Status != AssignmentStatus.Completed)
            {
                assignment.Status = AssignmentStatus.Completed;
                await _context.SaveChangesAsync();

                if (assignment.StageId.HasValue)
                {
                    await RecalculateStageAsync(assignment.StageId.Value);
                }
            }
        }

        // الترحيل التلقائي للمالية عند اكتمال التكليف (القسم 5.7)
        private async Task TransferToFinanceAsync(ProjectAssignment assignment)
        {
            assignment.IsTransferredToFinance = true;
            assignment.TransferredToFinanceAt = DateTime.UtcNow;

            _context.FinancialRecords.Add(new FinancialRecord
            {
                ProjectId = assignment.ProjectId,
                ProjectAssignmentId = assignment.Id,
                CostType = assignment.CostType,
                Value = assignment.FinalAmount,
                IsCleared = false,
                CreatedAt = DateTime.UtcNow
            });

            var project = await _context.Projects.FindAsync(assignment.ProjectId);
            if (project != null)
                project.ActualCost += assignment.FinalAmount;

            await _context.SaveChangesAsync();

            var financeUserIds = await GetUsersWithPermissionAsync("Finance.View");
            if (financeUserIds.Count > 0)
                await _notify.NotifyManyAsync(financeUserIds, $"تم ترحيل تكليف {assignment.CostType} إلى المالية بقيمة {assignment.FinalAmount:N2}", NotificationEventType.CostCompleted, $"/ProjectAssignments/Overview?projectId={assignment.ProjectId}", entityType: "ProjectAssignment", entityId: assignment.Id);
        }

        private async Task<List<string>> GetUsersWithPermissionAsync(string permissionCode)
        {
            var roleIds = await _context.RolePermissions
                .Where(rp => rp.IsGranted && rp.Permission.Code == permissionCode)
                .Select(rp => rp.RoleId)
                .ToListAsync();

            var userIds = await _context.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            return userIds;
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


        // إعادة حساب قيمة التكليف = مجموع القيم التقديرية لكل مهامه المرتبطة به
        public async Task RecalculateAssignmentValueAsync(int assignmentId)
        {
            var assignment = await _context.ProjectAssignments
                .Include(a => a.Tasks)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return;

            assignment.FinalAmount = assignment.Tasks.Sum(t => t.EstimatedValue);
            await _context.SaveChangesAsync();

            if (assignment.StageId.HasValue)
                await RecalculateStageAsync(assignment.StageId.Value);
        }
    }
}