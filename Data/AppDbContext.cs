using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

        // Module 2 (سابق - لم يُمس): إدارة المشاريع
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectEngineer> ProjectEngineers { get; set; } = null!;
        public DbSet<ProjectStage> ProjectStages { get; set; } = null!;
        public DbSet<ProjectStep> ProjectSteps { get; set; } = null!;
        public DbSet<ProjectTask> ProjectTasks { get; set; } = null!;
        public DbSet<TaskDependency> TaskDependencies { get; set; } = null!;

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

            // ========== الفهارس الفريدة ==========
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

            // ========== قسم المستخدم الأساسي ==========
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== المناصب المتعددة (EmployeePosition) ==========
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

            // ========== الصلاحيات الإضافية الممنوحة يدوياً (UserPermission) ==========
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

            // ========== علاقات المشاريع (Module 2 - سابق، بدون تغيير) ==========
            builder.Entity<Project>()
                .HasIndex(p => p.Code)
                .IsUnique();

            builder.Entity<Project>()
                .HasOne(p => p.ParentProject)
                .WithMany(p => p.ChildProjects)
                .HasForeignKey(p => p.ParentProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.ProjectManager)
                .WithMany()
                .HasForeignKey(p => p.ProjectManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectEngineer>()
                .HasOne(pe => pe.Project)
                .WithMany(p => p.ProjectEngineers)
                .HasForeignKey(pe => pe.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectEngineer>()
                .HasOne(pe => pe.User)
                .WithMany()
                .HasForeignKey(pe => pe.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectEngineer>()
                .HasIndex(pe => new { pe.ProjectId, pe.UserId })
                .IsUnique();

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
                .HasIndex(s => new { s.ProjectId, s.Order })
                .IsUnique();

            builder.Entity<ProjectStep>()
                .HasOne(st => st.ProjectStage)
                .WithMany(s => s.Steps)
                .HasForeignKey(st => st.ProjectStageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.ProjectStage)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.ProjectStageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}