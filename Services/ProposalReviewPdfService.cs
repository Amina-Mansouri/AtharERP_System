using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    // توليد ملف PDF لنموذج مراجعة موحّد — يخدم مراجعة المستندات المرتبطة بالمهام
    public class ProposalReviewPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public ProposalReviewPdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] Generate(ProposalReview review, Project project, Project? subProject, string? stageName, string itemName, int? revisionNumber)
        {
            var statusLabel = GetDisplayName(review.Status);
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "athar-logo-header.webp");
            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(11));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(60).Image(logoPath).FitArea();

                        header.Item().PaddingTop(8).BorderBottom(2).BorderColor("#c9a15a")
                            .PaddingBottom(6).AlignCenter().Text("نموذج مراجعة").FontSize(18).Bold().FontColor("#221837");
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"كود المشروع: {project.Code}");
                            row.RelativeItem().Text($"اسم المشروع: {project.Name}");
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"رقم المبنى: {subProject?.Code ?? "-"}");
                            row.RelativeItem().Text($"اسم المبنى: {subProject?.Name ?? "-"}");
                        });

                        col.Item().Text($"نوع المشروع: {project.ProjectCategory?.DisplayName ?? "-"}");

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"التاريخ: {review.ReviewDate:yyyy-MM-dd}");
                            row.RelativeItem().Text($"رقم المراجعة: {review.ReviewNumber}");
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"المرحلة: {stageName ?? "-"}");
                            row.RelativeItem().Text($"التخصص: {review.Discipline ?? "-"}");
                        });

                        col.Item().Text($"المستند: {itemName} — رمز: {revisionNumber?.ToString() ?? "-"}");

                        col.Item().PaddingTop(10).Text("الحالة").Bold().FontColor("#221837");
                        col.Item().Text($"☑ {statusLabel}");

                        col.Item().PaddingTop(10).Text("الملاحظات:").Bold().FontColor("#221837");
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(60)
                            .Text(string.IsNullOrWhiteSpace(review.Notes) ? "-" : review.Notes);

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"اسم المراجع: {review.ReviewerName}");
                                c.Item().Text($"الوظيفة: {review.ReviewerPosition ?? "-"}");
                                c.Item().PaddingTop(5).Text("التوقيع:");
                                if (!string.IsNullOrEmpty(review.ReviewerSignaturePath))
                                {
                                    var path = MapPath(review.ReviewerSignaturePath);
                                    if (File.Exists(path))
                                        c.Item().Height(50).Image(path).FitArea();
                                }
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"اسم المشرف المباشر: {review.SupervisorName ?? "-"}");
                                c.Item().Text($"الوظيفة: {review.SupervisorPosition ?? "-"}");
                                c.Item().PaddingTop(5).Text("التوقيع:");
                                if (!string.IsNullOrEmpty(review.SupervisorSignaturePath))
                                {
                                    var path = MapPath(review.SupervisorSignaturePath);
                                    if (File.Exists(path))
                                        c.Item().Height(50).Image(path).FitArea();
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(9).FontColor(Colors.Grey.Medium);
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