using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    // توليد ملف PDF لنموذج مراجعة موحّد — يخدم مراجعة المهام ومراجعة المقترحات التصميمية معاً
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

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(11));
                    page.ContentFromRightToLeft();

                    page.Header().AlignCenter().Text("نموذج مراجعة").FontSize(18).Bold();

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

                        col.Item().Text($"نوع المشروع: {(project.Type.HasValue ? GetDisplayName(project.Type.Value) : "-")}");

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"البند: {itemName}");
                            row.RelativeItem().Text($"رقم المراجعة/النسخة: {(revisionNumber.HasValue ? revisionNumber.Value.ToString() : "-")}");
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"التاريخ: {review.ReviewDate:yyyy-MM-dd}");
                            row.RelativeItem().Text($"المرحلة: {stageName ?? "-"}");
                        });

                        col.Item().Text($"التخصص: {review.Discipline ?? "-"}");

                        col.Item().PaddingTop(10).Text("الحالة").Bold();
                        col.Item().Text($"☑ {statusLabel}");

                        col.Item().PaddingTop(10).Text("الملاحظات:").Bold();
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