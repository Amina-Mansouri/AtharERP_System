using AtharERP_System.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtharERP_System.Services
{
    public class FinancePdfService
    {
        private readonly IWebHostEnvironment _environment;

        public FinancePdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] GenerateClaimsReport(string title, List<(Project Project, List<FinancialClaim> Claims)> sections, bool includePercentageColumns)
        {
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                foreach (var section in sections)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(25);
                        page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(9));
                        page.ContentFromRightToLeft();

                        page.Header().Column(header =>
                        {
                            if (hasLogo)
                                header.Item().AlignCenter().Height(45).Image(logoPath).FitArea();

                            header.Item().PaddingTop(6).BorderBottom(2).BorderColor("#c9a15a")
                                .PaddingBottom(5).AlignCenter().Text(title).FontSize(15).Bold().FontColor("#221837");

                            header.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Text($"المشروع: {section.Project.Code} - {section.Project.Name}");
                                row.RelativeItem().AlignLeft().Text($"تاريخ التصدير: {DateTime.UtcNow:yyyy-MM-dd}");
                            });
                        });

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            if (includePercentageColumns)
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.3f);
                                });
                                HeaderCell(table, "بيان التكليف");
                                HeaderCell(table, "المساحة");
                                HeaderCell(table, "سعر المتر");
                                HeaderCell(table, "قيمة البيع");
                                HeaderCell(table, "نسبة 1");
                                HeaderCell(table, "نسبة 2");
                                HeaderCell(table, "القيمة بعد النسبة");

                                foreach (var c in section.Claims)
                                {
                                    DataCell(table, c.ProjectAssignment?.AssignmentType ?? "-");
                                    DataCell(table, c.Area.HasValue ? c.Area.Value.ToString("N1") : "-");
                                    DataCell(table, c.SalePricePerMeter.HasValue ? c.SalePricePerMeter.Value.ToString("N0") : "-");
                                    DataCell(table, c.Value.ToString("N0"));
                                    DataCell(table, c.SaleMarkupPercent1.HasValue ? c.SaleMarkupPercent1.Value.ToString("N1") + "%" : "-");
                                    DataCell(table, c.SaleMarkupPercent2.HasValue ? c.SaleMarkupPercent2.Value.ToString("N1") + "%" : "-");
                                    DataCell(table, c.ValueAfterPercentage.ToString("N0"));
                                }
                            }
                            else
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.3f);
                                    c.RelativeColumn(1.3f);
                                });
                                HeaderCell(table, "بيان التكليف");
                                HeaderCell(table, "المساحة");
                                HeaderCell(table, "سعر المتر");
                                HeaderCell(table, "قيمة البيع");
                                HeaderCell(table, "المبلغ المستحق");
                                HeaderCell(table, "الحالة");

                                foreach (var c in section.Claims)
                                {
                                    DataCell(table, c.ProjectAssignment?.AssignmentType ?? "-");
                                    DataCell(table, c.Area.HasValue ? c.Area.Value.ToString("N1") : "-");
                                    DataCell(table, c.SalePricePerMeter.HasValue ? c.SalePricePerMeter.Value.ToString("N0") : "-");
                                    DataCell(table, c.Value.ToString("N0"));
                                    DataCell(table, c.RealValue.ToString("N0"));
                                    DataCell(table, c.IsClientSettled ? "تم التحصيل" : "بانتظار التحصيل");
                                }
                            }
                        });

                        page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                }
            }).GeneratePdf();
        }

        private static void HeaderCell(TableDescriptor table, string text)
        {
            table.Cell().Background("#221837").Padding(5).Text(text).FontColor(Colors.White).Bold();
        }

        private static void DataCell(TableDescriptor table, string text)
        {
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(text);
        }
    }
}