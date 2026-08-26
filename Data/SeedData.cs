using AtharERP_System.Models.Entities;
using AtharERP_System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AtharERP_System.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // ينشئ الجداول إذا لم تكن موجودة
            await context.Database.MigrateAsync();

            // ========== الأقسام الافتراضية (Section 10.3) ==========
            Department? topManagement = null;
            if (!await context.Departments.AnyAsync())
            {
                topManagement = new Department { Name = "الإدارة العليا", IsActive = true, CreatedAt = DateTime.UtcNow };
                context.Departments.Add(topManagement);
                await context.SaveChangesAsync();

                var childDepartmentNames = new[]
                {
                    "قسم التصميم المعماري والداخلي",
                    "قسم التصميم الإنشائي",
                    "قسم التصميم الميكانيكي",
                    "قسم التصميم الكهربائي",
                    "قسم التصميم الجرافيكي",
                    "قسم إدارة المشاريع",
                    "قسم إدارة العمليات الميدانية",
                    "قسم إدارة التوريدات",
                    "قسم شؤون الإدارة والموارد البشرية",
                    "قسم المالية",
                    "قسم الجودة والتدقيق",
                    "قسم Document Control"
                };

                foreach (var name in childDepartmentNames)
                {
                    context.Departments.Add(new Department
                    {
                        Name = name,
                        ParentDepartmentId = topManagement.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
            }
            else
            {
                topManagement = await context.Departments.FirstOrDefaultAsync(d => d.ParentDepartmentId == null);
            }

            // ========== الأدوار القوالب الجاهزة (محمية من الحذف) ==========
            var templateRoles = new[]
            {
                new { Name = "مدير النظام", Desc = "صلاحيات كاملة" },
                new { Name = "مهندس تصميم", Desc = "المشاريع والتصاميم" },
                new { Name = "مهندس جودة", Desc = "الجودة والتقارير" }
            };

            foreach (var r in templateRoles)
            {
                if (!await roleManager.RoleExistsAsync(r.Name))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = r.Name,
                        Description = r.Desc,
                        IsTemplate = true,
                        CanDelete = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // ========== كتالوج الصلاحيات (47 صلاحية - القسم 6 من الوحدة 1) ==========
            var permissions = new[]
            {
                // 6.1 عام / المستخدمون
                new Permission { Code = "Users.View", Name = "المستخدمين.عرض", Module = "Users" },
                new Permission { Code = "Users.Create", Name = "المستخدمين.إضافة", Module = "Users" },
                new Permission { Code = "Users.Edit", Name = "المستخدمين.تعديل", Module = "Users" },
                new Permission { Code = "Users.Delete", Name = "المستخدمين.حذف", Module = "Users" },
                new Permission { Code = "Users.ToggleActive", Name = "المستخدمين.تفعيل", Module = "Users" },

                // 6.2 الأدوار
                new Permission { Code = "Roles.View", Name = "الأدوار.عرض", Module = "Roles" },
                new Permission { Code = "Roles.Create", Name = "الأدوار.إضافة", Module = "Roles" },
                new Permission { Code = "Roles.Edit", Name = "الأدوار.تعديل", Module = "Roles" },
                new Permission { Code = "Roles.Delete", Name = "الأدوار.حذف", Module = "Roles" },
                new Permission { Code = "Roles.ManagePermissions", Name = "الأدوار.تخصيص_صلاحيات", Module = "Roles" },

                // 6.3 المشاريع
                new Permission { Code = "Projects.ViewAll", Name = "المشاريع.عرض_الكل", Module = "Projects" },
                new Permission { Code = "Projects.ViewOwn", Name = "المشاريع.عرض_الخاصة", Module = "Projects" },
                new Permission { Code = "Projects.Create", Name = "المشاريع.إضافة", Module = "Projects" },
                new Permission { Code = "Projects.Edit", Name = "المشاريع.تعديل", Module = "Projects" },
                new Permission { Code = "Projects.Delete", Name = "المشاريع.حذف", Module = "Projects" },
                new Permission { Code = "Projects.Stages.Manage", Name = "المشاريع.مراحل.إدارة", Module = "Projects" },
                new Permission { Code = "Projects.Tasks.Manage", Name = "المشاريع.مهام.إدارة", Module = "Projects" },
                new Permission { Code = "Projects.Costs.View", Name = "المشاريع.تكاليف.عرض", Module = "Projects" },
                new Permission { Code = "Projects.Costs.Edit", Name = "المشاريع.تكاليف.تعديل", Module = "Projects" },

                // 6.4 المالية
                new Permission { Code = "Finance.View", Name = "المالية.عرض", Module = "Finance" },
                new Permission { Code = "Finance.Costs.View", Name = "المالية.تكاليف.عرض", Module = "Finance" },
                new Permission { Code = "Finance.Costs.Edit", Name = "المالية.تكاليف.تعديل", Module = "Finance" },
                new Permission { Code = "Finance.Sales.View", Name = "المالية.مبيعات.عرض", Module = "Finance" },
                new Permission { Code = "Finance.Sales.Edit", Name = "المالية.مبيعات.تعديل", Module = "Finance" },
                new Permission { Code = "Finance.Reports", Name = "المالية.تقارير", Module = "Finance" },
                new Permission { Code = "Finance.Print", Name = "المالية.طباعة", Module = "Finance" },

                // 6.5 الموارد البشرية
                new Permission { Code = "HR.View", Name = "الموارد_البشرية.عرض", Module = "HR" },
                new Permission { Code = "HR.Attendance.View", Name = "الموارد_البشرية.حضور.عرض", Module = "HR" },
                new Permission { Code = "HR.Attendance.Edit", Name = "الموارد_البشرية.حضور.تعديل", Module = "HR" },
                new Permission { Code = "HR.Salaries.View", Name = "الموارد_البشرية.رواتب.عرض", Module = "HR" },
                new Permission { Code = "HR.Salaries.Edit", Name = "الموارد_البشرية.رواتب.تعديل", Module = "HR" },
                new Permission { Code = "HR.Leaves.Manage", Name = "الموارد_البشرية.إجازات.إدارة", Module = "HR" },
                new Permission { Code = "HR.Evaluation", Name = "الموارد_البشرية.تقييم", Module = "HR" },

                // 6.6 التوريدات
                new Permission { Code = "Supply.View", Name = "التوريدات.عرض", Module = "Supply" },
                new Permission { Code = "Supply.Create", Name = "التوريدات.إضافة", Module = "Supply" },
                new Permission { Code = "Supply.Approve", Name = "التوريدات.موافقة", Module = "Supply" },

                // 6.7 المواقع
                new Permission { Code = "Sites.View", Name = "المواقع.عرض", Module = "Site" },
                new Permission { Code = "Sites.Reports", Name = "المواقع.تقارير", Module = "Site" },
                new Permission { Code = "Sites.Manage", Name = "المواقع.إدارة", Module = "Site" },
               
                // 6.8 العلاقات العامة / CRM
                new Permission { Code = "PR.View", Name = "العلاقات_العامة.عرض", Module = "CRM" },
                new Permission { Code = "PR.Contracts", Name = "العلاقات_العامة.عقود", Module = "CRM" },
                new Permission { Code = "PR.Clients", Name = "العلاقات_العامة.عملاء", Module = "CRM" },

                // 6.9 الجودة
                new Permission { Code = "Quality.View", Name = "الجودة.عرض", Module = "Quality" },
                new Permission { Code = "Quality.Reports", Name = "الجودة.تقارير", Module = "Quality" },
                new Permission { Code = "Quality.Approve", Name = "الجودة.اعتماد", Module = "Quality" },

                // 6.10 التقارير والإشعارات
                new Permission { Code = "Reports.View", Name = "التقارير.عرض", Module = "Reports" },
                new Permission { Code = "Reports.Export", Name = "التقارير.تصدير", Module = "Reports" },
                new Permission { Code = "Notifications.Manage", Name = "الإشعارات.إدارة", Module = "Notifications" },
            };

            foreach (var p in permissions)
            {
                if (!await context.Permissions.AnyAsync(x => x.Code == p.Code))
                    context.Permissions.Add(p);
            }
            await context.SaveChangesAsync();

            // ========== ربط الصلاحيات بالأدوار ==========
            await LinkPermissionsToRole(context, roleManager, "مدير النظام", permissions.Select(p => p.Code).ToArray());

            await LinkPermissionsToRole(context, roleManager, "مهندس تصميم", new[]
            {
                "Projects.ViewOwn",
                "Supply.View", "Supply.Create"
            });

            await LinkPermissionsToRole(context, roleManager, "مهندس جودة", new[]
            {
                "Projects.ViewAll", "Projects.Create", "Projects.Stages.Manage", "Projects.Tasks.Manage",
                "Quality.View", "Quality.Approve", "Quality.Reports",
                "Reports.View"
            });

            // ========== المستخدم الافتراضي ==========
            var adminEmail = "admin@athar.ly";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "مدير النظام",
                    JobNumber = "EMP-0001",
                    DepartmentId = topManagement?.Id,
                    Responsibilities = "الإدارة الكاملة للنظام",
                    Rank = JobRank.M5_CEO,
                    CareerTrack = CareerTrack.Administrative,
                    ContractSalary = 0,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(admin, "Athar@Admin2026");
                await userManager.AddToRoleAsync(admin, "مدير النظام");

                if (topManagement != null)
                {
                    context.EmployeePositions.Add(new EmployeePosition
                    {
                        UserId = admin.Id,
                        DepartmentId = topManagement.Id,
                        Rank = JobRank.M5_CEO,
                        Track = CareerTrack.Administrative,
                        StartDate = DateTime.UtcNow,
                        IsPrimary = true
                    });
                    await context.SaveChangesAsync();
                }
            }

            // ========== عملاء ومشروع تجريبي ==========
            if (!await context.Clients.AnyAsync())
            {
                var sampleClients = new[]
                {
                    new Client
                    {
                        Name = "أحمد الككلي",
                        CompanyName = "مجموعة الككلي للاستثمار",
                        Phone = "0912345678",
                        Email = "info@kikli-group.ly",
                        Address = "طرابلس - شارع الجمهورية",
                        TaxNumber = "TX-10023",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Client
                    {
                        Name = "سالم المبروك",
                        CompanyName = "شركة المبروك للمقاولات",
                        Phone = "0913456789",
                        Email = "contact@mabrouk-const.ly",
                        Address = "بنغازي - شارع جمال عبدالناصر",
                        TaxNumber = "TX-10045",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Client
                    {
                        Name = "هدى العريبي",
                        CompanyName = null,
                        Phone = "0914567890",
                        Email = "houda.araibi@example.com",
                        Address = "مصراتة - المنطقة الصناعية",
                        TaxNumber = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Clients.AddRange(sampleClients);
                await context.SaveChangesAsync();

                if (!await context.Projects.AnyAsync())
                {
                    var sampleProject = new Project
                    {
                        Code = $"PRJ-{DateTime.UtcNow.Year}-001",
                        Name = "مشروع تجريبي - مجمع سكني",
                        Description = "مشروع تجريبي لتهيئة النظام",
                        ClientId = sampleClients[0].Id,
                        Type = ProjectType.Main,
                        Status = ProjectStatus.New,
                        PlannedStartDate = DateTime.UtcNow,
                        Budget = 500000,
                        Priority = Priority.Normal,
                        CreatedById = admin.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Projects.Add(sampleProject);
                    await context.SaveChangesAsync();

                    context.ProjectTeamMembers.Add(new ProjectTeamMember
                    {
                        ProjectId = sampleProject.Id,
                        UserId = admin.Id,
                        Role = TeamRole.ProjectManager,
                        JoinedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task LinkPermissionsToRole(AppDbContext context, RoleManager<ApplicationRole> roleManager, string roleName, string[] permCodes)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;

            var permCodesList = permCodes.ToList();

            var existing = await context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var perms = await context.Permissions
                .Where(p => permCodesList.Contains(p.Code))
                .ToListAsync();

            foreach (var p in perms)
            {
                if (!existing.Contains(p.Id))
                    context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id, IsGranted = true });
            }
            await context.SaveChangesAsync();
        }
    }
}