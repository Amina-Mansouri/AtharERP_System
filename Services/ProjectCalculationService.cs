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

        // نسبة إنجاز المرحلة = مجموع أوزان الخطوات المكتملة ÷ مجموع أوزان كل الخطوات
        public async Task RecalculateStageAsync(int stageId)
        {
            var stage = await _context.ProjectStages
                .Include(s => s.Steps)
                .FirstOrDefaultAsync(s => s.Id == stageId);

            if (stage == null) return;

            var totalWeight = stage.Steps.Sum(s => s.Weight);
            var completedWeight = stage.Steps
                .Where(s => s.Status == ProjectStatus.Completed)
                .Sum(s => s.Weight);

            stage.CompletionPercentage = totalWeight > 0
                ? Math.Round((completedWeight / totalWeight) * 100, 2)
                : 0;

            await _context.SaveChangesAsync();
            await RecalculateProjectAsync(stage.ProjectId);
        }

        // نسبة إنجاز المشروع = المتوسط المرجّح لنسب إنجاز المراحل حسب وزن كل مرحلة
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
    }
}