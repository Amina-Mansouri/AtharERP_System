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

            // ========== الأدوار القوالب الجاهزة ==========
            var templateRoles = new[]
            {
                new { Name = "مدير النظام", Desc = "صلاحيات كاملة", IsTemplate = true },
                new { Name = "مهندس تصميم", Desc = "المشاريع والتصاميم", IsTemplate = true },
                new { Name = "مهندس جودة", Desc = "الجودة والتقارير", IsTemplate = true }
            };

            foreach (var r in templateRoles)
            {
                if (!await roleManager.RoleExistsAsync(r.Name))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = r.Name,
                        Description = r.Desc,
                        IsTemplate = r.IsTemplate,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // ========== صلاحيات النظام ==========
            var permissions = new[]
            {
                new Permission { Name = "Users.View", Description = "عرض المستخدمين", Module = "المستخدمون", Action = "View" },
                new Permission { Name = "Users.Create", Description = "إنشاء مستخدم", Module = "المستخدمون", Action = "Create" },
                new Permission { Name = "Users.Edit", Description = "تعديل مستخدم", Module = "المستخدمون", Action = "Edit" },
                new Permission { Name = "Users.Delete", Description = "حذف مستخدم", Module = "المستخدمون", Action = "Delete" },

                new Permission { Name = "Roles.View", Description = "عرض الأدوار", Module = "الأدوار", Action = "View" },
                new Permission { Name = "Roles.Create", Description = "إنشاء دور", Module = "الأدوار", Action = "Create" },
                new Permission { Name = "Roles.Edit", Description = "تعديل دور", Module = "الأدوار", Action = "Edit" },
                new Permission { Name = "Roles.Delete", Description = "حذف دور", Module = "الأدوار", Action = "Delete" },

                new Permission { Name = "Projects.View", Description = "عرض المشاريع", Module = "المشاريع", Action = "View" },
                new Permission { Name = "Projects.Create", Description = "إنشاء مشروع", Module = "المشاريع", Action = "Create" },
                new Permission { Name = "Projects.Edit", Description = "تعديل مشروع", Module = "المشاريع", Action = "Edit" },
                new Permission { Name = "Projects.Delete", Description = "حذف مشروع", Module = "المشاريع", Action = "Delete" },

                new Permission { Name = "Sites.View", Description = "عرض المواقع", Module = "المواقع", Action = "View" },
                new Permission { Name = "Sites.Manage", Description = "إدارة المواقع", Module = "المواقع", Action = "Manage" },

                new Permission { Name = "Supplies.View", Description = "عرض التوريدات", Module = "التوريدات", Action = "View" },
                new Permission { Name = "Supplies.Create", Description = "إنشاء طلب توريد", Module = "التوريدات", Action = "Create" },
                new Permission { Name = "Supplies.Approve", Description = "اعتماد توريد", Module = "التوريدات", Action = "Approve" },

                new Permission { Name = "Finance.View", Description = "عرض المالية", Module = "المالية", Action = "View" },
                new Permission { Name = "Finance.Create", Description = "تسجيل مالية", Module = "المالية", Action = "Create" },

                new Permission { Name = "HR.View", Description = "عرض الموارد البشرية", Module = "الموارد البشرية", Action = "View" },
                new Permission { Name = "HR.Attendance", Description = "إدارة الحضور", Module = "الموارد البشرية", Action = "Attendance" },

                new Permission { Name = "CRM.View", Description = "عرض العملاء", Module = "العلاقات العامة", Action = "View" },
                new Permission { Name = "CRM.Approvals", Description = "موافقات العملاء", Module = "العلاقات العامة", Action = "Approvals" },

                new Permission { Name = "Reports.View", Description = "عرض التقارير", Module = "التقارير", Action = "View" },
                new Permission { Name = "Reports.Export", Description = "تصدير التقارير", Module = "التقارير", Action = "Export" },
            };

            foreach (var p in permissions)
            {
                if (!await context.Permissions.AnyAsync(x => x.Name == p.Name))
                    context.Permissions.Add(p);
            }
            await context.SaveChangesAsync();

            // ربط الصلاحيات بالأدوار
            await LinkPermissionsToRole(context, roleManager, "مدير النظام", permissions.Select(p => p.Name).ToArray());
            await LinkPermissionsToRole(context, roleManager, "مهندس تصميم", new[] {
                "Projects.View", "Projects.Create", "Projects.Edit",
                "Sites.View", "Sites.Manage", "Supplies.View", "Supplies.Create",
                "CRM.View", "CRM.Approvals", "Reports.View"
            });
            await LinkPermissionsToRole(context, roleManager, "مهندس جودة", new[] {
                "Projects.View", "Projects.Edit", "Sites.View",
                "Supplies.View", "Supplies.Approve", "Finance.View",
                "Reports.View", "Reports.Export", "CRM.View", "CRM.Approvals"
            });

            // ========== المستخدم الافتراضي ==========
            var adminEmail = "admin@athar.ly";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "مدير النظام",
                    JobTitle = "مدير تنفيذي",
                    Department = "الإدارة العليا",
                    EngineerRank = EngineerRank.None,
                    EmailConfirmed = true,
                    IsActive = true,
                    JoinDate = DateTime.UtcNow
                };
                await userManager.CreateAsync(admin, "Athar@Admin2026");
                await userManager.AddToRoleAsync(admin, "مدير النظام");
            }
        }

        private static async Task LinkPermissionsToRole(AppDbContext context, RoleManager<ApplicationRole> roleManager, string roleName, string[] permNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;

            var existing = await context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var perms = await context.Permissions
                .Where(p => permNames.Contains(p.Name))
                .ToListAsync();

            foreach (var p in perms)
            {
                if (!existing.Contains(p.Id))
                    context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
            }
            await context.SaveChangesAsync();
        }
    }
}