using AtharERP_System.Data;
using AtharERP_System.Identity;
using AtharERP_System.Models.Entities;
using AtharERP_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// قاعدة البيانات PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity مع الكيانات المخصصة + الأدوار + رسائل الأخطاء بالعربية
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddErrorDescriber<AtharIdentityErrorDescriber>()
.AddDefaultTokenProviders();
// نظام مصادقة منفصل تماماً للمقاولين (Cookie خاص، لا علاقة له بهوية موظفي الشركة)
builder.Services.AddAuthentication()
    .AddCookie("ContractorScheme", options =>
    {
        options.LoginPath = "/ContractorPortal/Login";
        options.AccessDeniedPath = "/ContractorPortal/Login";
        options.Cookie.Name = "AtharContractorAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });

// خدمة التحقق من الصلاحيات
builder.Services.AddScoped<PermissionService>();

// خدمة حساب نسب الإنجاز للمراحل والمشاريع
builder.Services.AddScoped<ProjectCalculationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddHostedService<ContractLifecycleHostedService>();

// خدمة إرسال البريد الإلكتروني (تفعيل حساب منسي / إعادة تعيين كلمة المرور)
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// تقليل الفاصل الزمني للتحقق من صلاحية الجلسة حتى يتم إبطالها بسرعة عند تعطيل المستخدم
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

// التحقق الإضافي من حالة تفعيل الحساب عند كل تحقق من الجلسة
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);

        if (context.Principal?.Identity?.IsAuthenticated != true)
            return;

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);

        if (user == null || !user.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

var app = builder.Build();

// تهيئة البيانات الافتراضية
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();