using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    // توليد PDF لنماذج الشؤون الإدارية (إجازة / استقالة / إذن / تسليم عهدة) — يُستدعى عند كل خطوة اعتماد لأرشفة النموذج بأحدث توقيع
    public class HRFormsPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public HRFormsPdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] GenerateLeavePdf(LeaveRequest r)
        {
            return BuildDocument("نموذج طلب إجازة", r.User, col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"نوع الإجازة: {GetDisplayName(r.Type)}");
                    row.RelativeItem().Text($"عدد الأيام: {r.DaysCount}");
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"من: {r.StartDate:yyyy-MM-dd}");
                    row.RelativeItem().Text($"إلى: {r.EndDate:yyyy-MM-dd}");
                });
                col.Item().Text($"السبب: {r.Reason ?? "-"}");
                col.Item().Text($"تاريخ التقديم: {r.RequestedAt:yyyy-MM-dd}");

                SignatureSection(col, "اعتماد الشؤون الإدارية", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GenerateResignationPdf(ResignationRequest r)
        {
            return BuildDocument("نموذج طلب استقالة", r.User, col =>
            {
                col.Item().Text($"آخر يوم عمل مقترح: {r.RequestedLastWorkingDay:yyyy-MM-dd}");
                col.Item().Text($"السبب: {r.Reason ?? "-"}");
                col.Item().Text($"تاريخ التقديم: {r.RequestedAt:yyyy-MM-dd}");

                SignatureSection(col, "اعتماد الشؤون الإدارية", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GeneratePermitPdf(PermitRequest r)
        {
            return BuildDocument("نموذج إذن", r.User, col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"نوع الإذن: {GetDisplayName(r.Type)}");
                    row.RelativeItem().Text($"التاريخ: {r.Date:yyyy-MM-dd}");
                });
                col.Item().Text($"الفترة: {r.TimeRangeDisplay} ({r.DurationHours:N1} ساعة)");
                col.Item().Text($"السبب: {r.Reason ?? "-"}");
                col.Item().Text($"تاريخ التقديم: {r.RequestedAt:yyyy-MM-dd}");

                SignatureSection(col, "اعتماد الشؤون الإدارية", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GenerateSalaryAdvancePdf(SalaryAdvanceRequest r)
        {
            return BuildDocument("نموذج طلب سلفة", r.User, col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"المبلغ: {r.Amount:N0} د.ل");
                    row.RelativeItem().Text($"مدة السداد: {r.InstallmentMonths} شهر");
                });
                col.Item().Text($"القسط الشهري: {r.MonthlyInstallment:N0} د.ل");
                col.Item().Text($"تاريخ التقديم: {r.RequestedAt:yyyy-MM-dd}");

                SignatureSection(col, "اعتماد المسؤول المالي", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GenerateCustodyHandoverPdf(CustodyHandover h)
        {
            return BuildDocument("نموذج تسليم عهدة", h.Holder, col =>
            {
                col.Item().Text($"تاريخ التسليم: {h.HandedDate:yyyy-MM-dd}");
                col.Item().PaddingTop(5).Text("المواد المسلَّمة:").Bold();
                foreach (var line in h.Lines)
                {
                    col.Item().Text($"• {line.Item?.Name ?? "-"} — الكمية: {line.QuantityHanded}");
                }

                SignatureSection(col, "الشؤون الإدارية (مُجهِّز العملية)", h.CreatedBy, h.HandedDate);
                SignatureSection(col, "توقيع المستلم", h.HolderConfirmed ? h.Holder : null, h.HolderConfirmedAt);
            });
        }

        private byte[] BuildDocument(string title, ApplicationUser? applicant, Action<ColumnDescriptor> body)
        {
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(12));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(60).Image(logoPath).FitArea();

                        header.Item().PaddingTop(8).BorderBottom(2).BorderColor("#c9a15a")
                            .PaddingBottom(6).AlignCenter().Text(title).FontSize(14).Bold().FontColor("#221837");
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"اسم الموظف: {applicant?.FullName ?? "-"}");
                            row.RelativeItem().Text($"الوظيفة: {applicant?.PositionRef?.NameAr ?? "-"}");
                        });
                        col.Item().Text($"القسم: {applicant?.Department?.Name ?? "-"}");

                        body(col);
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        private void SignatureSection(ColumnDescriptor col, string title, ApplicationUser? decider, DateTime? decidedAt)
        {
            col.Item().PaddingTop(10).Text(title).Bold().FontColor("#221837");
            if (decider == null)
            {
                col.Item().Text("بانتظار الاعتماد");
                return;
            }

            col.Item().Text($"الاسم: {decider.FullName}");
            col.Item().Text($"الوظيفة: {decider.PositionRef?.NameAr ?? "-"}");
            col.Item().Text($"التاريخ: {(decidedAt.HasValue ? decidedAt.Value.ToString("yyyy-MM-dd") : "-")}");
            col.Item().PaddingTop(3).Text("التوقيع:");
            if (!string.IsNullOrEmpty(decider.SignatureImagePath))
            {
                var path = MapPath(decider.SignatureImagePath);
                if (File.Exists(path))
                    col.Item().Height(45).Image(path).FitArea();
            }
        }

        private string MapPath(string relativePath)
        {
            return Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;
            return attr?.Name ?? value.ToString();
        }
    }
}