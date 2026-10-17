using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    // توليد PDF لنموذج مراسلة الموقع (طلب أو رد) بين الشركة والمقاول
    public class SiteCorrespondencePdfService
    {
        private readonly IWebHostEnvironment _environment;

        public SiteCorrespondencePdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] Generate(SiteCorrespondence item, Site site)
        {
            var project = site.Project;
            var buildingCode = project?.Scope == ProjectScope.Sub ? project.Code : null;
            var priorityLabel = GetDisplayName(item.Priority);
            var statusLabel = item.Status == CorrespondenceStatus.Pending ? "قيد الانتظار" : "تم الرد";
            var requestCode = $"{site.Code}-{item.RequestNumber:D4}";
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(10.5f));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(55).Image(logoPath).FitArea();
                        header.Item().PaddingTop(6).AlignCenter().Text("أثر للتصاميم والاستشارات الهندسية").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingTop(12).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("الطلب").Bold().FontSize(13).FontColor(Colors.White);

                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"اسم المشروع: {project?.Name ?? "-"}");
                                row.RelativeItem().Text($"رقم الطلب: {requestCode}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"مقدم الطلب: {item.SubmittedByName}");
                                row.RelativeItem().Text($"رمز المشروع: {project?.Code ?? "-"}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"الأولوية: {priorityLabel}");
                                row.RelativeItem().Text($"رمز المبنى: {buildingCode ?? "-"}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"تاريخ الرد المطلوب: {(item.RequiredResponseDate.HasValue ? item.RequiredResponseDate.Value.ToString("yyyy-MM-dd") : "-")}");
                                row.RelativeItem().Text($"الموقع: {site.Name}");
                            });
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("تفاصيل الاستفسار").Bold().FontSize(13).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(90).Text(item.DetailedQuery);

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("المرفقات").Bold().FontSize(13).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(30).Column(c =>
                        {
                            if (!item.Attachments.Any())
                            {
                                c.Item().Text("لا يوجد مرفق");
                            }
                            else
                            {
                                foreach (var att in item.Attachments)
                                {
                                    c.Item().Text($"• {att.FileName}");
                                }
                            }
                        });

                        if (!item.IsFromContractor)
                        {
                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Background("#c9a15a").Padding(5).AlignCenter().Text("اعتماد المدير المباشر").Bold().FontSize(11).FontColor(Colors.White);
                                    c.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                                    {
                                        cc.Item().Text($"الصفة: {item.ApproverPosition ?? "-"}");
                                        cc.Item().Text($"التاريخ: {item.CreatedAt:yyyy-MM-dd}");
                                        cc.Item().PaddingTop(4).Text("التوقيع:");
                                        if (!string.IsNullOrEmpty(item.ApproverSignaturePath))
                                        {
                                            var path = MapPath(item.ApproverSignaturePath);
                                            if (File.Exists(path))
                                                cc.Item().Height(40).Image(path).FitArea();
                                        }
                                    });
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Background("#c9a15a").Padding(5).AlignCenter().Text("مقدم الطلب").Bold().FontSize(11).FontColor(Colors.White);
                                    c.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                                    {
                                        cc.Item().Text($"الصفة: {item.SubmittedByPosition ?? "-"}");
                                        cc.Item().Text($"التاريخ: {item.CreatedAt:yyyy-MM-dd}");
                                        cc.Item().PaddingTop(4).Text("التوقيع:");
                                        if (!string.IsNullOrEmpty(item.SubmittedBySignaturePath))
                                        {
                                            var path = MapPath(item.SubmittedBySignaturePath);
                                            if (File.Exists(path))
                                                cc.Item().Height(40).Image(path).FitArea();
                                        }
                                    });
                                });
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(10).Column(c =>
                            {
                                c.Item().Background("#c9a15a").Padding(5).AlignCenter().Text("مقدم الطلب").Bold().FontSize(11).FontColor(Colors.White);
                                c.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                                {
                                    cc.Item().Text($"الصفة: {item.SubmittedByPosition ?? "-"}");
                                    cc.Item().Text($"التاريخ: {item.CreatedAt:yyyy-MM-dd}");
                                    cc.Item().PaddingTop(4).Text("التوقيع:");
                                    if (!string.IsNullOrEmpty(item.SubmittedBySignaturePath))
                                    {
                                        var path = MapPath(item.SubmittedBySignaturePath);
                                        if (File.Exists(path))
                                            cc.Item().Height(40).Image(path).FitArea();
                                    }
                                });
                            });
                        }

                        col.Item().PaddingTop(10).Text($"الحالة: {statusLabel}").Bold().FontColor("#221837");
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
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