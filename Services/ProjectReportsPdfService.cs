using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    public class ProjectReportsPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public ProjectReportsPdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] GenerateClosurePdf(ProjectClosureReport r, Site? site, List<DocumentRevision> revisions)
        {
            var project = r.Project;
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(11));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(55).Image(logoPath).FitArea();
                        header.Item().PaddingTop(4).AlignCenter().Text("تقارير إغلاق وتسليم المشاريع").Bold().FontSize(14);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("بيانات المشروع").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"اسم المشروع: {project.Name}");
                                row.RelativeItem().Text($"كود المشروع: {project.Code}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"موقع المشروع: {site?.Name ?? "-"}");
                                row.RelativeItem().Text($"الاصدار: {r.Version ?? "-"}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"تاريخ بدء المشروع: {(project.PlannedStartDate.HasValue ? project.PlannedStartDate.Value.ToString("yyyy-MM-dd") : "-")}");
                                row.RelativeItem().Text($"تاريخ الإغلاق: {(r.ClosureDate.HasValue ? r.ClosureDate.Value.ToString("yyyy-MM-dd") : "-")}");
                            });
                            c.Item().Text($"التخصص: {(r.Stages.Any() ? string.Join("، ", r.Stages.Select(s => s.Stage.Name)) : "-")}");
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("حالة نطاق العمل").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().Text((r.ScopeFullyCompleted ? "☑" : "☐") + " تم استكمال كامل نطاق العمل");
                            c.Item().Text((r.ScopePartiallyWithNotes ? "☑" : "☐") + " تم استكمال جزئي مع ملاحظات");
                            c.Item().Text((r.ScopeExcludedWorkExists ? "☑" : "☐") + " توجد أعمال مستثناة من نطاق العقد");
                            c.Item().PaddingTop(4).Text($"ملاحظات: {r.ScopeNotes ?? "-"}");
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("حالة مخرجات التصميم").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Border(1).Background("#f2f2f2").Padding(4).Text("التخصص").Bold();
                                h.Cell().Border(1).Background("#f2f2f2").Padding(4).Text("الحالة").Bold();
                            });
                            if (!r.Stages.Any())
                            {
                                table.Cell().ColumnSpan(2).Border(1).Padding(4).Text("لا توجد مراحل مُختارة");
                            }
                            foreach (var s in r.Stages)
                            {
                                table.Cell().Border(1).Padding(4).Text(s.Stage.Name);
                                table.Cell().Border(1).Padding(4).Text(GetDisplayName(s.Stage.Status));
                            }
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Border(1).Background("#f2f2f2").Padding(4).Text("الملف").Bold();
                                h.Cell().Border(1).Background("#f2f2f2").Padding(4).Text("الحالة").Bold();
                                h.Cell().Border(1).Background("#f2f2f2").Padding(4).Text("رقم الإصدار").Bold();
                                h.Cell().Border(1).Background("#f2f2f2").Padding(4).Text("الملاحظات").Bold();
                            });
                            if (!revisions.Any())
                            {
                                table.Cell().ColumnSpan(4).Border(1).Padding(4).Text("لا توجد ملفات مصدرة");
                            }
                            foreach (var rev in revisions)
                            {
                                table.Cell().Border(1).Padding(4).Text(rev.Document?.FileName ?? "-");
                                table.Cell().Border(1).Padding(4).Text(GetDisplayName(rev.Status));
                                table.Cell().Border(1).Padding(4).Text(rev.Revision.ToString());
                                table.Cell().Border(1).Padding(4).Text(rev.Category ?? "-");
                            }
                        });

                        col.Item().PaddingTop(4).Background("#c9a15a").Padding(6).AlignCenter().Text("حالة المشروع النهائية").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().Text($"الحالة: {(r.FinalStatus.HasValue ? GetDisplayName(r.FinalStatus.Value) : "بانتظار اعتماد المدير")}");
                            c.Item().Text($"ملاحظات: {r.FinalStatusNotes ?? "-"}");
                        });

                        col.Item().PaddingTop(10).Background("#c9a15a").Padding(6).AlignCenter().Text("الاعتماد").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            if (r.DecidedBy == null)
                            {
                                c.Item().Text("بانتظار الاعتماد");
                            }
                            else
                            {
                                c.Item().Text($"الاسم: {r.DecidedBy.FullName}");
                                c.Item().Text($"التاريخ: {r.DecidedAt:yyyy-MM-dd}");
                                c.Item().PaddingTop(4).Text("التوقيع:");
                                if (!string.IsNullOrEmpty(r.DecidedBy.SignatureImagePath))
                                {
                                    var path = MapPath(r.DecidedBy.SignatureImagePath);
                                    if (File.Exists(path))
                                        c.Item().Height(40).Image(path).FitArea();
                                }
                            }
                        });
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        public byte[] GenerateDeliveryPdf(ProjectDeliveryReport r, Site? site, List<ProjectAssignment> linkedAssignments, List<ProjectAssignment> incompleteAssignments)
        {
            var project = r.Project;
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(11));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(55).Image(logoPath).FitArea();
                        header.Item().PaddingTop(4).AlignCenter().Text("نموذج تسليم مشروع").Bold().FontSize(14);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("بيانات المشروع").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"اسم المشروع: {project.Name}");
                                row.RelativeItem().Text($"كود المشروع: {project.Code}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"موقع المشروع: {site?.Name ?? "-"}");
                                row.RelativeItem().Text($"التخصص: {r.Stage.Name}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"تاريخ التكليف: {(r.AssignmentDate.HasValue ? r.AssignmentDate.Value.ToString("yyyy-MM-dd") : "-")}");
                                row.RelativeItem().Text($"تاريخ التسليم الفعلي: {(r.ActualDeliveryDate.HasValue ? r.ActualDeliveryDate.Value.ToString("yyyy-MM-dd") : "-")}");
                            });
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("التكليفات المرتبطة").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(25).Column(c =>
                        {
                            if (!linkedAssignments.Any())
                                c.Item().Text("لا توجد تكليفات مرتبطة");
                            else
                                foreach (var a in linkedAssignments)
                                    c.Item().Text($"• {a.AssignmentType} ({GetDisplayName(a.Status)})");
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("قائمة الملفات والمستندات المسلّمة").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(25).Column(c =>
                        {
                            if (!r.DeliveredFiles.Any())
                                c.Item().Text("لا توجد ملفات مسلّمة في هذا التسليم");
                            else
                                foreach (var df in r.DeliveredFiles)
                                    c.Item().Text($"• {df.DocumentRevision?.Document?.FileName ?? "-"} (إصدار {df.DocumentRevision?.Revision})");
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("قائمة الملفات المراد تسليمها").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(25).Column(c =>
                        {
                            if (!incompleteAssignments.Any())
                                c.Item().Text("لا توجد تكليفات غير مكتملة");
                            else
                                foreach (var a in incompleteAssignments)
                                    c.Item().Text($"• {a.AssignmentType}");
                        });

                        col.Item().Background("#c9a15a").Padding(6).AlignCenter().Text("ملاحظات").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).MinHeight(40).Text(r.Notes ?? "-");

                        col.Item().PaddingTop(10).Background("#c9a15a").Padding(6).AlignCenter().Text("الاعتمادات النهائية").Bold().FontSize(12).FontColor(Colors.White);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Background("#eee").Padding(5).AlignCenter().Text("اعتماد المدير المباشر").Bold().FontSize(11);
                                c.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                                {
                                    if (r.DecidedBy == null)
                                    {
                                        cc.Item().Text("بانتظار الاعتماد");
                                    }
                                    else
                                    {
                                        cc.Item().Text($"الاسم: {r.DecidedBy.FullName}");
                                        cc.Item().Text($"الرقم الوظيفي: {r.DecidedBy.JobNumber ?? "-"}");
                                        cc.Item().Text($"التاريخ: {r.DecidedAt:yyyy-MM-dd}");
                                        cc.Item().PaddingTop(4).Text("التوقيع:");
                                        if (!string.IsNullOrEmpty(r.DecidedBy.SignatureImagePath))
                                        {
                                            var path = MapPath(r.DecidedBy.SignatureImagePath);
                                            if (File.Exists(path))
                                                cc.Item().Height(40).Image(path).FitArea();
                                        }
                                    }
                                });
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Background("#eee").Padding(5).AlignCenter().Text("المهندسة المسؤولة").Bold().FontSize(11);
                                c.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(cc =>
                                {
                                    cc.Item().Text($"الاسم: {r.CreatedBy.FullName}");
                                    cc.Item().Text($"الرقم الوظيفي: {r.CreatedBy.JobNumber ?? "-"}");
                                    cc.Item().Text($"التاريخ: {r.CreatedAt:yyyy-MM-dd}");
                                    cc.Item().PaddingTop(4).Text("التوقيع:");
                                    if (!string.IsNullOrEmpty(r.CreatedBy.SignatureImagePath))
                                    {
                                        var path = MapPath(r.CreatedBy.SignatureImagePath);
                                        if (File.Exists(path))
                                            cc.Item().Height(40).Image(path).FitArea();
                                    }
                                });
                            });
                        });
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