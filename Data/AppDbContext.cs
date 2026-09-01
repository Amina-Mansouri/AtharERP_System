using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AtharERP_System.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // الجداول الإضافية خارج Identity
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;

        // Module 01: الهيكل التنظيمي والصلاحيات الموسّعة
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<EmployeePosition> EmployeePositions { get; set; } = null!;
        public DbSet<UserPermission> UserPermissions { get; set; } = null!;

        // Module 02: إدارة المشاريع
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectStage> ProjectStages { get; set; } = null!;
        public DbSet<ProjectStep> ProjectSteps { get; set; } = null!;
        public DbSet<ProjectTask> ProjectTasks { get; set; } = null!;
        public DbSet<TaskAssignee> TaskAssignees { get; set; } = null!;
        public DbSet<TaskTodo> TaskTodos { get; set; } = null!;
        public DbSet<TaskDependency> TaskDependencies { get; set; } = null!;
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; } = null!;
        public DbSet<ProjectAssignmentSubtask> ProjectAssignmentSubtasks { get; set; } = null!;
        public DbSet<ProjectTeamMember> ProjectTeamMembers { get; set; } = null!;
        public DbSet<ProjectDocument> ProjectDocuments { get; set; } = null!;
        public DbSet<ProjectTimeline> ProjectTimelines { get; set; } = null!;
        public DbSet<FinancialRecord> FinancialRecords { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationSetting> NotificationSettings { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; } = null!;
        // Module 03: إدارة المواقع
        public DbSet<Site> Sites { get; set; } = null!;
        public DbSet<SiteOperation> SiteOperations { get; set; } = null!;
        public DbSet<SiteDailyReport> SiteDailyReports { get; set; } = null!;
        public DbSet<SiteDailyReportPhoto> SiteDailyReportPhotos { get; set; } = null!;
        public DbSet<SiteQualityCheck> SiteQualityChecks { get; set; } = null!;
        public DbSet<SiteSafetyCheck> SiteSafetyChecks { get; set; } = null!;
        public DbSet<SiteContractor> SiteContractors { get; set; } = null!;
        public DbSet<SiteMaintenance> SiteMaintenances { get; set; } = null!;
        public DbSet<SiteDocument> SiteDocuments { get; set; } = null!;
        public DbSet<SiteSupplyRequest> SiteSupplyRequests { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========== تخصيص أسماء جداول Identity ==========
            builder.Entity<ApplicationUser>().ToTable("Users", "identity");
            builder.Entity<ApplicationRole>().ToTable("Roles", "identity");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "identity");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "identity");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "identity");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "identity");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "identity");

            // ========== علاقات RolePermission ==========
            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            builder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            // ========== القسم (Department) - علاقة ذاتية ==========
            builder.Entity<Department>()
                .HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ApplicationUser>()
    .HasIndex(u => u.JobNumber)
    .IsUnique();

           

            builder.Entity<EmployeePosition>()
                .HasOne(ep => ep.User)
                .WithMany(u => u.EmployeePositions)
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EmployeePosition>()
                .HasOne(ep => ep.Department)
                .WithMany(d => d.EmployeePositions)
                .HasForeignKey(ep => ep.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany()
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPermission>()
                .HasIndex(up => new { up.UserId, up.PermissionId })
                .IsUnique();

            // ========== المشروع (Project) ==========
            builder.Entity<Project>()
                .HasIndex(p => p.Code)
                .IsUnique();

            builder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.ParentProject)
                .WithMany(p => p.ChildProjects)
                .HasForeignKey(p => p.ParentProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== مراحل المشروع (ProjectStage) ==========
            builder.Entity<ProjectStage>()
                .HasOne(s => s.Project)
                .WithMany(p => p.Stages)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectStage>()
                .HasOne(s => s.AssignedEngineer)
                .WithMany()
                .HasForeignKey(s => s.AssignedEngineerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectStage>()
                .HasOne(s => s.Department)
                .WithMany()
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectStage>()
                .HasIndex(s => new { s.ProjectId, s.Sequence })
                .IsUnique();

            // ========== خطوات المرحلة (ProjectStep) ==========
            builder.Entity<ProjectStep>()
                .HasOne(st => st.Stage)
                .WithMany(s => s.Steps)
                .HasForeignKey(st => st.StageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectStep>()
                .HasOne(st => st.CompletedBy)
                .WithMany()
                .HasForeignKey(st => st.CompletedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== مهام المشروع (ProjectTask) ==========
            builder.Entity<ProjectTask>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.Stage)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.StageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== المكلَّفون بالمهمة (TaskAssignee) ==========
            builder.Entity<TaskAssignee>()
                .HasOne(ta => ta.Task)
                .WithMany(t => t.Assignees)
                .HasForeignKey(ta => ta.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskAssignee>()
                .HasOne(ta => ta.User)
                .WithMany()
                .HasForeignKey(ta => ta.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskAssignee>()
                .HasIndex(ta => new { ta.TaskId, ta.UserId })
                .IsUnique();

            builder.Entity<TaskAssignee>()
                .Property(ta => ta.IsLead)
                .HasDefaultValue(false);
            // ========== قائمة مهام To-Do (TaskTodo) ==========
            builder.Entity<TaskTodo>()
                .HasOne(tt => tt.Task)
                .WithMany(t => t.Todos)
                .HasForeignKey(tt => tt.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== تبعيات المهام (TaskDependency) ==========
            builder.Entity<TaskDependency>()
                .HasOne(td => td.Task)
                .WithMany(t => t.Dependencies)
                .HasForeignKey(td => td.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskDependency>()
                .HasOne(td => td.DependsOnTask)
                .WithMany(t => t.DependentTasks)
                .HasForeignKey(td => td.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskDependency>()
                .HasIndex(td => new { td.TaskId, td.DependsOnTaskId })
                .IsUnique();

            
            // ========== تكليفات المشروع (ProjectAssignment) ==========
            builder.Entity<ProjectAssignment>()
                .HasOne(a => a.Project)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectAssignment>()
                .HasOne(a => a.Stage)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.StageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ProjectAssignment>()
                .HasOne(a => a.LeadEngineer)
                .WithMany()
                .HasForeignKey(a => a.LeadEngineerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectAssignment>()
                .HasOne(a => a.AssistantEngineer)
                .WithMany()
                .HasForeignKey(a => a.AssistantEngineerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectAssignmentSubtask>()
                .HasOne(cs => cs.ProjectAssignment)
                .WithMany(a => a.Subtasks)
                .HasForeignKey(cs => cs.ProjectAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== فريق المشروع (ProjectTeamMember) ==========
            builder.Entity<ProjectTeamMember>()
                .HasOne(tm => tm.Project)
                .WithMany(p => p.TeamMembers)
                .HasForeignKey(tm => tm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTeamMember>()
                .HasOne(tm => tm.User)
                .WithMany()
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectTeamMember>()
                .HasIndex(tm => new { tm.ProjectId, tm.UserId })
                .IsUnique();

            // ========== مستندات المشروع (ProjectDocument) ==========
            builder.Entity<ProjectDocument>()
                .HasOne(d => d.Project)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectDocument>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== الجدول الزمني (ProjectTimeline) ==========
            builder.Entity<ProjectTimeline>()
                .HasOne(tl => tl.Project)
                .WithMany(p => p.Timelines)
                .HasForeignKey(tl => tl.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== السجلات المالية (FinancialRecord) ==========
            builder.Entity<FinancialRecord>()
                .HasOne(f => f.Project)
                .WithMany()
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinancialRecord>()
      .HasOne(f => f.ProjectAssignment)
      .WithMany()
      .HasForeignKey(f => f.ProjectAssignmentId)
      .OnDelete(DeleteBehavior.SetNull);

            // ========== الإشعارات (Notification) ==========
            // ========== الإشعارات (Notification) ==========
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .Property(n => n.EventType)
                .HasDefaultValue(NotificationEventType.TaskAssigned);

            builder.Entity<Notification>()
                .Property(n => n.SourceModule)
                .HasDefaultValue("02");

            builder.Entity<Notification>()
                .Property(n => n.RequiresAction)
                .HasDefaultValue(false);

            // ========== تفضيلات الإشعارات (NotificationSetting) ==========
            builder.Entity<NotificationSetting>()
                .HasOne(ns => ns.User)
                .WithMany()
                .HasForeignKey(ns => ns.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationSetting>()
                .HasIndex(ns => new { ns.UserId, ns.EventType })
                .IsUnique();

            // ========== سجل التدقيق (AuditLog) ==========
            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // ========== المواقع (Site) ==========
            builder.Entity<Site>()
                .HasOne(s => s.Project)
                .WithMany()
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== مراحل الموقع (SiteOperation) ==========
            builder.Entity<SiteOperation>()
                .HasOne(o => o.Site)
                .WithMany(s => s.Operations)
                .HasForeignKey(o => o.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SiteOperation>()
                .HasOne(o => o.Responsible)
                .WithMany()
                .HasForeignKey(o => o.ResponsibleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== التقارير اليومية (SiteDailyReport) ==========
            builder.Entity<SiteDailyReport>()
                .HasOne(r => r.Site)
                .WithMany(s => s.DailyReports)
                .HasForeignKey(r => r.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SiteDailyReport>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== صور التقرير اليومي (SiteDailyReportPhoto) ==========
            builder.Entity<SiteDailyReportPhoto>()
                .HasOne(p => p.DailyReport)
                .WithMany(r => r.Photos)
                .HasForeignKey(p => p.DailyReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== فحوصات الجودة (SiteQualityCheck) ==========
            builder.Entity<SiteQualityCheck>()
                .HasOne(q => q.Site)
                .WithMany(s => s.QualityChecks)
                .HasForeignKey(q => q.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SiteQualityCheck>()
                .HasOne(q => q.CheckedBy)
                .WithMany()
                .HasForeignKey(q => q.CheckedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SiteQualityCheck>()
                .HasOne(q => q.ApprovedBy)
                .WithMany()
                .HasForeignKey(q => q.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== فحوصات السلامة (SiteSafetyCheck) ==========
            builder.Entity<SiteSafetyCheck>()
                .HasOne(sc => sc.Site)
                .WithMany(s => s.SafetyChecks)
                .HasForeignKey(sc => sc.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SiteSafetyCheck>()
                .HasOne(sc => sc.CheckedBy)
                .WithMany()
                .HasForeignKey(sc => sc.CheckedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== المقاولون (SiteContractor) ==========
            builder.Entity<SiteContractor>()
                .HasOne(c => c.Site)
                .WithMany(s => s.Contractors)
                .HasForeignKey(c => c.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== الصيانة (SiteMaintenance) ==========
            builder.Entity<SiteMaintenance>()
                .HasOne(m => m.Site)
                .WithMany(s => s.MaintenanceRequests)
                .HasForeignKey(m => m.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SiteMaintenance>()
                .HasOne(m => m.Responsible)
                .WithMany()
                .HasForeignKey(m => m.ResponsibleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== مستندات الموقع (SiteDocument) ==========
            builder.Entity<SiteDocument>()
                .HasOne(d => d.Site)
                .WithMany(s => s.Documents)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== طلبات توريد الموقع (SiteSupplyRequest) ==========
            builder.Entity<SiteSupplyRequest>()
                .HasOne(sr => sr.Site)
                .WithMany(s => s.SupplyRequests)
                .HasForeignKey(sr => sr.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SiteSupplyRequest>()
                .HasOne(sr => sr.Project)
                .WithMany()
                .HasForeignKey(sr => sr.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SiteSupplyRequest>()
                .HasOne(sr => sr.RequestedBy)
                .WithMany()
                .HasForeignKey(sr => sr.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);
            // ========== مستندات الموظف (EmployeeDocument) ==========
            builder.Entity<EmployeeDocument>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EmployeeDocument>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);


            // ========== إصلاح عام: فرض UTC على كل خصائص DateTime قبل حفظها في Postgres ==========
            // أعمدة timestamptz ترفض DateTime.Kind=Unspecified (وهو ما يصل من <input type="date">
            // في المتصفح)، فيفشل الحفظ بخطأ ArgumentException. هذا المحوّل يطبَّق تلقائياً على كل
            // خاصية DateTime/DateTime? في كل الكيانات دون تعديل كل كيان على حدة.
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue
                    ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime())
                    : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(utcConverter);
                    else if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }
}