using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    // توليد PDF لنماذج الشؤون الإدارية (إجازة / استقالة / إذن / تسليم عهدة / سلفة / شكوى) — بشكل جدولي يطابق النماذج الورقية، يُستدعى عند كل خطوة اعتماد لأرشفة النموذج بأحدث توقيع
    public class HRFormsPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public HRFormsPdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] GenerateLeavePdf(LeaveRequest r, int remainingDays, List<(string Code, string Name)> openAssignments)
        {
            return BuildDocument("نموذج تقديم طلب اجازة", r.User, col =>
            {
                FieldRow(col, "تاريخ تقديم الطلب:", r.RequestedAt.ToString("yyyy-MM-dd"));
                FieldRow(col, "رقم طلب الإجازة:", r.Id.ToString());
                FieldRow(col, "رصيد الإجازات المتاح:", remainingDays.ToString());

                SectionHeader(col, "بيانات الاجازة");
                FieldRow(col, "نوع الإجازة:", GetDisplayName(r.Type));
                DualFieldRow(col, "من تاريخ:", r.StartDate.ToString("yyyy-MM-dd"), "إلى تاريخ:", r.EndDate.ToString("yyyy-MM-dd"));
                FieldRow(col, "عدد الأيام:", r.DaysCount.ToString());
                FieldRow(col, "سبب الإجازة:", r.Reason ?? "-");

                SectionHeader(col, "تسليم المهام أثناء فترة الإجازة");
                if (!openAssignments.Any())
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text("لا توجد تكليفات مفتوحة حالياً");
                }
                else
                {
                    var i = 1;
                    foreach (var a in openAssignments)
                    {
                        FieldRow(col, $".{i}", $"{a.Code} - {a.Name}");
                        i++;
                    }
                }

                SignatureSection(col, "اعتماد الشؤون الإدارية", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GenerateResignationPdf(ResignationRequest r)
        {
            return BuildDocument("طلب استقالة", r.User, col =>
            {
                FieldRow(col, "تاريخ تقديم الاستقالة:", r.RequestedAt.ToString("yyyy-MM-dd"));
                FieldRow(col, "سبب الاستقالة:", r.Reason ?? "-");

                SectionHeader(col, "إقرار الموظف");
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                    .Text("أتقدم بطلب استقالتي من العمل، راجياً قبولها واتخاذ الإجراءات اللازمة وفقاً للوائح والنظم المعمول بها، مع خالص الشكر والتقدير.");
                FieldRow(col, "آخر يوم عمل مقترح:", r.RequestedLastWorkingDay.ToString("yyyy-MM-dd"));
                SignatureSection(col, "توقيع الموظف", r.User, r.RequestedAt);

                SignatureSection(col, "اعتماد الشؤون الإدارية", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GeneratePermitPdf(PermitRequest r)
        {
            return BuildDocument("نموذج اذونات", r.User, col =>
            {
                SectionHeader(col, "نوع الأذن");
                FieldRow(col, "نوع الأذن:", GetDisplayName(r.Type));
                FieldRow(col, "الفترة:", $"{r.TimeRangeDisplay} ({r.DurationHours:N1} ساعة)");
                FieldRow(col, "التاريخ:", r.Date.ToString("yyyy-MM-dd"));

                SectionHeader(col, "السبب");
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).MinHeight(30).Text(r.Reason ?? "-");

                SectionHeader(col, "المرفقات");
                if (!r.Attachments.Any())
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text("لا توجد مرفقات");
                }
                else
                {
                    foreach (var att in r.Attachments)
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(att.FileName);
                    }
                }

                SignatureSection(col, "اعتماد الشؤون الإدارية", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GenerateComplaintPdf(ComplaintRequest r)
        {
            return BuildDocument("نموذج تقديم شكوى", r.User, col =>
            {
                FieldRow(col, "تاريخ تقديم الشكوى:", r.RequestedAt.ToString("yyyy-MM-dd"));

                SectionHeader(col, "ثانيًا: بيانات الشكوى أو الاعتراض");
                FieldRow(col, "نوع الطلب:", GetDisplayName(r.Type));
                FieldRow(col, "موضوع الشكوى:", r.Subject);
                FieldRow(col, "تفاصيل الشكوى:", r.Description, 45);

                SectionHeader(col, "ثالثًا: إقرار مقدم الطلب");
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                    .Text("أقر بأن المعلومات الواردة أعلاه صحيحة حسب علمي، وأنني قدمت هذا الطلب بحسن نية، وأتحمل مسؤولية أي معلومات أقدمها عمدًا بصورة مضلّلة.");
                SignatureSection(col, "توقيع مقدم الطلب", r.User, r.RequestedAt);

                SectionHeader(col, "رابعًا: رد الشؤون الإدارية");
                if (r.ResolvedBy == null)
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).MinHeight(30).Text("بانتظار الرد");
                }
                else
                {
                    FieldRow(col, "الرد:", r.ResolutionNotes ?? "-", 40);
                    SignatureSection(col, "تم الرد من", r.ResolvedBy, r.ResolvedAt);
                }
            });
        }

        public byte[] GenerateSalaryAdvancePdf(SalaryAdvanceRequest r)
        {
            return BuildDocument("نموذج طلب سلفة", r.User, col =>
            {
                FieldRow(col, "تاريخ تقديم الطلب:", r.RequestedAt.ToString("yyyy-MM-dd"));

                SectionHeader(col, "بيانات السلفة");
                DualFieldRow(col, "المبلغ:", $"{r.Amount:N0} د.ل", "مدة السداد:", $"{r.InstallmentMonths} شهر");
                FieldRow(col, "القسط الشهري:", $"{r.MonthlyInstallment:N0} د.ل");

                SignatureSection(col, "اعتماد المسؤول المالي", r.DecidedBy, r.DecidedAt);
                SignatureSection(col, "اعتماد المدير العام", r.GmDecidedBy, r.GmDecidedAt);
            });
        }

        public byte[] GenerateCustodyHandoverPdf(CustodyHandover h)
        {
            return BuildDocument("نموذج تسليم العهد", h.Holder, col =>
            {
                FieldRow(col, "تاريخ تسليم العهد:", h.HandedDate.ToString("yyyy-MM-dd"));

                SectionHeader(col, "العهد المستلمة");
                var i = 1;
                foreach (var line in h.Lines)
                {
                    FieldRow(col, $".{i}", $"{line.Item?.Name ?? "-"} — الكمية: {line.QuantityHanded}");
                    i++;
                }

                SignatureSection(col, "اعتماد الشؤون الإدارية", h.CreatedBy, h.HandedDate);
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
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(10));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(55).Image(logoPath).FitArea();

                        header.Item().PaddingTop(8).AlignCenter().Text(title).FontSize(15).Bold().FontColor("#221837");
                    });

                    page.Content().PaddingTop(12).Column(col =>
                    {
                        SectionHeader(col, "بيانات الموظفة");
                        DualFieldRow(col, "اسم الموظفة:", applicant?.FullName ?? "-", "الرقم الوظيفي:", applicant?.JobNumber ?? "-");
                        DualFieldRow(col, "القسم:", applicant?.Department?.Name ?? "-", "المسمى الوظيفي:", applicant?.PositionRef?.NameAr ?? "-");

                        body(col);
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        private void SectionHeader(ColumnDescriptor col, string text)
        {
            col.Item().Background("#d9d9d9").Padding(6).AlignCenter().Text(text).Bold().FontSize(11).FontColor("#221837");
        }

        private void FieldRow(ColumnDescriptor col, string label, string value, float minHeight = 20)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(1).Background("#f2f2f2").Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(label).Bold();
                row.RelativeItem(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).MinHeight(minHeight).AlignRight().Text(value ?? "-");
            });
        }

        private void DualFieldRow(ColumnDescriptor col, string label1, string value1, string label2, string value2)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(1).Background("#f2f2f2").Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(label1).Bold();
                row.RelativeItem(2).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(value1 ?? "-");
                row.RelativeItem(1).Background("#f2f2f2").Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(label2).Bold();
                row.RelativeItem(2).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(value2 ?? "-");
            });
        }

        private void SignatureSection(ColumnDescriptor col, string title, ApplicationUser? decider, DateTime? decidedAt)
        {
            SectionHeader(col, title);
            if (decider == null)
            {
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text("بانتظار الاعتماد");
                return;
            }

            FieldRow(col, "الاسم:", decider.FullName);
            FieldRow(col, "الوظيفة:", decider.PositionRef?.NameAr ?? "-");
            FieldRow(col, "التاريخ:", decidedAt.HasValue ? decidedAt.Value.ToString("yyyy-MM-dd") : "-");
            col.Item().Row(row =>
            {
                row.RelativeItem(1).Background("#f2f2f2").Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("التوقيع:").Bold();
                row.RelativeItem(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).MinHeight(30).Element(container =>
                {
                    if (!string.IsNullOrEmpty(decider.SignatureImagePath))
                    {
                        var path = MapPath(decider.SignatureImagePath);
                        if (File.Exists(path))
                            container.Image(path).FitArea();
                    }
                });
            });
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