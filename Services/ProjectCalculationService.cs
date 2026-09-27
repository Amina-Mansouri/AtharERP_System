using AtharERP_System.Data;
using AtharERP_System.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Services
{
    public class ProjectCalculationService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notify;
        private readonly PermissionService _permission;
        private readonly SiteCalculationService _siteCalc;

        public ProjectCalculationService(AppDbContext context, NotificationService notify, PermissionService permission, SiteCalculationService siteCalc)
        {
            _context = context;
            _notify = notify;
            _permission = permission;
            _siteCalc = siteCalc;
        }

        private static bool IsTaskFrozen(ProjectTask t)
        {
            return t.Status == ProjectTaskStatus.Blocked
                || (t.ProjectAssignment != null && t.ProjectAssignment.Status == AssignmentStatus.Cancelled);
        }

        public async Task RecalculateStageAsync(int stageId)
        {
            var stage = await _context.ProjectStages
                .Include(s => s.Tasks).ThenInclude(t => t.ProjectAssignment)
                .FirstOrDefaultAsync(s => s.Id == stageId);

            if (stage == null) return;

            var wasCompleted = stage.Status == StageStatus.Completed;

            // المهام المحظورة، أو التابعة لتكليف معلَّق/ملغى، تُستبعد كلياً من الحساب — لا تعرقل نسبة إنجاز باقي المهام
            var activeTasks = stage.Tasks.Where(t => !IsTaskFrozen(t)).ToList(); 
            var totalWeight = activeTasks.Sum(t => t.Weight);
            var completedWeight = activeTasks
                .Where(t => t.Status == ProjectTaskStatus.Completed)
                .Sum(t => t.Weight);

            stage.CompletionPercentage = totalWeight > 0
                ? Math.Round((completedWeight / totalWeight) * 100, 2)
                : 0;

            var allStages = await _context.ProjectStages.Include(s => s.Tasks).Where(s => s.ProjectId == stage.ProjectId).ToListAsync();
            ApplyAutomaticStageStatus(stage, allStages);

            await _context.SaveChangesAsync();
            await RecalculateProjectAsync(stage.ProjectId);

            if (!wasCompleted && stage.Status == StageStatus.Completed)
            {
                var recipientIds = await _permission.GetProjectRecipientsAsync(stage.ProjectId);

                await _notify.NotifyManyAsync(recipientIds, $"اكتملت المرحلة: {stage.Name}", NotificationEventType.StageCompleted, $"/Projects/Details/{stage.ProjectId}", entityType: "ProjectStage", entityId: stage.Id);
            }
        }

        // الحالة التلقائية الكاملة للمرحلة — لا تدخّل يدوي إطلاقاً
        public void ApplyAutomaticStageStatus(ProjectStage stage, IEnumerable<ProjectStage> allProjectStages)
        {
            // 100% لكن توجد مهمة محظورة معلَّقة — لا تُعتبر مكتملة فعلياً حتى تُحل
            if (stage.CompletionPercentage >= 100 && !stage.Tasks.Any(IsTaskFrozen))
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
    .Include(p => p.Stages).ThenInclude(s => s.RedesignRequests)
    .FirstOrDefaultAsync(p => p.Id == projectId);
           

            if (project == null) return;

            var wasDelayed = project.Status == ProjectStatus.Delayed;

            project.ActualCost = project.Stages.Sum(s => s.ActualCost);
            project.Budget = project.Stages.Sum(s => CalculateStageSaleAfterPercentage(s));
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
            else if (project.Status == ProjectStatus.InProgress || project.Status == ProjectStatus.Delayed)
            {
                // متأخر: باقي 30 يوماً أو أقل على تاريخ التسليم (أو تجاوزه فعلاً) ونسبة الإنجاز أقل من 70%
                var daysRemaining = project.PlannedEndDate.HasValue
                    ? (int?)(project.PlannedEndDate.Value.Date - DateTime.UtcNow.Date).Days
                    : null;
                var isAtRisk = daysRemaining.HasValue && daysRemaining.Value <= 30 && project.CompletionPercentage < 70;
                project.Status = isAtRisk ? ProjectStatus.Delayed : ProjectStatus.InProgress;
            }

            await _context.SaveChangesAsync();

            if (!wasDelayed && project.Status == ProjectStatus.Delayed)
            {
                var recipientIds = await _permission.GetProjectRecipientsAsync(project.Id);
                await _notify.NotifyManyAsync(recipientIds, $"المشروع \"{project.Name}\" أصبح متأخراً", NotificationEventType.TaskDelayed, $"/Projects/Details/{project.Id}", requiresAction: true, entityType: "Project", entityId: project.Id);
            }
        }

        public async Task RecalculateTaskCompletionAsync(int taskId, bool notifyOnStatusChange = true)
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

            if (task.Status != ProjectTaskStatus.Blocked && task.Status != ProjectTaskStatus.Completed)
            {
                task.Status = task.CompletionPercentage >= 100
                    ? ProjectTaskStatus.PendingReview
                    : task.CompletionPercentage > 0
                        ? ProjectTaskStatus.InProgress
                        : ProjectTaskStatus.NotStarted;
            }

            if (task.Status == ProjectTaskStatus.PendingReview && oldStatus != ProjectTaskStatus.PendingReview && task.ActualDeliveryDate == null)
            {
                task.ActualDeliveryDate = DateTime.UtcNow.Date;
                UpdateDeliveryMetrics(task);
            }

            await _context.SaveChangesAsync();

            if (task.Status != oldStatus && notifyOnStatusChange)
            {
                var recipientIds = await _permission.GetProjectRecipientsAsync(task.ProjectId);

                if (recipientIds.Count > 0)
                {
                    var statusLabel = task.Status switch
                    {
                        ProjectTaskStatus.NotStarted => "لم تبدأ",
                        ProjectTaskStatus.InProgress => "قيد التنفيذ",
                        ProjectTaskStatus.PendingReview => "قيد المراجعة",
                        ProjectTaskStatus.Completed => "مكتملة",
                        ProjectTaskStatus.Blocked => "محظورة",
                        _ => task.Status.ToString()
                    };
                    await _notify.NotifyManyAsync(recipientIds, $"تغيّرت حالة المهمة \"{task.Title}\" إلى: {statusLabel}", NotificationEventType.TaskStatusChanged, $"/ProjectTasks/Edit/{task.Id}", entityType: "ProjectTask", entityId: task.Id);
                }
            }

            if (task.StageId.HasValue)
            {
                await RecalculateStageAsync(task.StageId.Value);
            }

            if (task.ProjectAssignmentId.HasValue)
            {
                await RecalculateAssignmentCompletionAsync(task.ProjectAssignmentId.Value);
            }
        }

        private async Task RecalculateAssignmentCompletionAsync(int assignmentId)
        {
            var assignment = await _context.ProjectAssignments
                .Include(a => a.Tasks)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
                return;

            var activeTasks = assignment.Tasks.Where(t => t.Status != ProjectTaskStatus.Blocked).ToList();
            if (!activeTasks.Any())
                return;

            var allTasksDone = activeTasks.All(t => t.CompletionPercentage >= 100);

            if (allTasksDone && assignment.Status != AssignmentStatus.Completed)
            {
                assignment.Status = AssignmentStatus.Completed;
                await _context.SaveChangesAsync();
                await RecalculateAssignmentFinanceAsync(assignment.Id);
            }
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

        private static decimal CalculateStageSaleAfterPercentage(ProjectStage stage)
        {
            var redesignPercent = stage.RedesignRequests?.Sum(r => r.IncreasePercentage) ?? 0;
            return stage.SaleValue
                * (1 + (stage.SaleMarkupPercent1 ?? 0) / 100)
                * (1 + (stage.SaleMarkupPercent2 ?? 0) / 100)
                * (1 + redesignPercent / 100);
        }

        // نقطة الدخول المركزية لأي تعديل مالي على تكليف: مساحة/سعر/نسبة مساهمة مهندسة
        public async Task RecalculateAssignmentFinanceAsync(int assignmentId)
        {
            var assignment = await _context.ProjectAssignments
                .Include(a => a.Engineers)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return;

            if (assignment.IsTransferredToFinance)
            {
                await SyncAssignmentFinancialRecordsAsync(assignment);
            }

            if (assignment.StageId.HasValue)
            {
                await RecalculateStageFinanceAsync(assignment.StageId.Value);
            }

            if (assignment.Status == AssignmentStatus.Completed && !assignment.IsTransferredToFinance)
            {
                await TransferAssignmentToFinanceAsync(assignment);
            }
        }

        // يُستدعى أيضاً عند إضافة طلب إعادة تصميم جديد على مرحلة (يزامن كل تكليفاتها المُرحَّلة)
        public async Task SyncStageRedesignPercentageAsync(int stageId)
        {
            var assignments = await _context.ProjectAssignments
                .Include(a => a.Engineers)
                .Where(a => a.StageId == stageId && a.IsTransferredToFinance)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                await SyncAssignmentFinancialRecordsAsync(assignment);
            }

            await RecalculateStageFinanceAsync(stageId);
        }

        private async Task RecalculateStageFinanceAsync(int stageId)
        {
            var stage = await _context.ProjectStages
                .Include(s => s.Assignments)
                .FirstOrDefaultAsync(s => s.Id == stageId);

            if (stage == null) return;

            stage.ActualCost = stage.Assignments.Sum(a => a.AssignmentValue);
            stage.SaleValue = stage.Assignments.Sum(a => a.AssignmentSaleValue);
            await _context.SaveChangesAsync();

            await RecalculateProjectAsync(stage.ProjectId);
        }

        private async Task TransferAssignmentToFinanceAsync(ProjectAssignment assignment)
        {
            if (!assignment.Engineers.Any())
                return; // لا يمكن الترحيل بلا مهندسة واحدة على الأقل

            var stage = assignment.StageId.HasValue
                ? await _context.ProjectStages.Include(s => s.RedesignRequests)
                    .FirstOrDefaultAsync(s => s.Id == assignment.StageId.Value)
                : null;

            var redesignPercent = stage?.RedesignRequests.Sum(r => r.IncreasePercentage) ?? 0;

            foreach (var engineer in assignment.Engineers)
            {
                _context.FinancialRecords.Add(new FinancialRecord
                {
                    ProjectId = assignment.ProjectId,
                    ProjectAssignmentId = assignment.Id,
                    EngineerId = engineer.UserId,
                    Area = assignment.Area,
                    PricePerMeter = assignment.PricePerMeter,
                    Value = assignment.AssignmentValue,
                    ContributionPercentage = engineer.ContributionPercentage
                });
            }

            _context.FinancialClaims.Add(new FinancialClaim
            {
                ProjectId = assignment.ProjectId,
                ProjectAssignmentId = assignment.Id,
                Code = $"CLM-{assignment.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Area = assignment.Area,
                SalePricePerMeter = assignment.SalePricePerMeter,
                Value = assignment.AssignmentSaleValue,
                SaleMarkupPercent1 = stage?.SaleMarkupPercent1,
                SaleMarkupPercent2 = stage?.SaleMarkupPercent2,
                RedesignIncreasePercentage = redesignPercent,
                IsTransferredToFinance = true,
                TransferredToFinanceAt = DateTime.UtcNow
            });

            assignment.IsTransferredToFinance = true;
            assignment.TransferredToFinanceAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task SyncAssignmentFinancialRecordsAsync(ProjectAssignment assignment)
        {
            var records = await _context.FinancialRecords
                .Where(r => r.ProjectAssignmentId == assignment.Id && !r.IsCleared)
                .ToListAsync();

            foreach (var record in records)
            {
                var engineer = assignment.Engineers.FirstOrDefault(e => e.UserId == record.EngineerId);
                record.Area = assignment.Area;
                record.PricePerMeter = assignment.PricePerMeter;
                record.Value = assignment.AssignmentValue;
                record.ContributionPercentage = engineer?.ContributionPercentage;
            }

            var claim = await _context.FinancialClaims
                .FirstOrDefaultAsync(c => c.ProjectAssignmentId == assignment.Id && !c.IsClientSettled);

            if (claim != null)
            {
                var stage = assignment.StageId.HasValue
                    ? await _context.ProjectStages.Include(s => s.RedesignRequests)
                        .FirstOrDefaultAsync(s => s.Id == assignment.StageId.Value)
                    : null;

                claim.Area = assignment.Area;
                claim.SalePricePerMeter = assignment.SalePricePerMeter;
                claim.Value = assignment.AssignmentSaleValue;
                claim.SaleMarkupPercent1 = stage?.SaleMarkupPercent1;
                claim.SaleMarkupPercent2 = stage?.SaleMarkupPercent2;
                claim.RedesignIncreasePercentage = stage?.RedesignRequests.Sum(r => r.IncreasePercentage) ?? 0;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalculateProjectNetProfitAsync(int projectId)
        {
            var totalSales = await _context.FinancialClaims
                .Where(c => c.ProjectId == projectId)
                .SumAsync(c => c.Value
                    * (1 + (c.SaleMarkupPercent1 ?? 0) / 100)
                    * (1 + (c.SaleMarkupPercent2 ?? 0) / 100)
                    * (1 + (c.RedesignIncreasePercentage ?? 0) / 100));

            var totalTaskCosts = await _context.FinancialRecords
                .Where(r => r.ProjectId == projectId)
                .SumAsync(r => r.Value * (1 + (r.ContributionPercentage ?? 0) / 100));

            var totalGeneralExpenses = await _context.ProjectExpenses
                .Where(e => e.ProjectId == projectId && e.SiteId == null)
                .SumAsync(e => e.Amount);

            var siteCosts = await _siteCalc.CalculateProjectSiteCostsAsync(projectId);

            return totalSales - (totalTaskCosts + totalGeneralExpenses + siteCosts);
        }
    }
}