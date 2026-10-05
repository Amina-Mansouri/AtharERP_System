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
                                    c.RelativeColumn(0.8f);
                                    c.RelativeColumn(0.8f);
                                    c.RelativeColumn(1.1f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.1f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.1f);
                                    c.RelativeColumn(1.3f);
                                });
                                HeaderCell(table, "بيان التكليف");
                                HeaderCell(table, "المساحة");
                                HeaderCell(table, "سعر المتر");
                                HeaderCell(table, "قيمة البيع");
                                HeaderCell(table, "نسبة 1");
                                HeaderCell(table, "نسبة 2");
                                HeaderCell(table, "القيمة بعد النسبة");
                                HeaderCell(table, "نسبة إعادة التصميم");
                                HeaderCell(table, "الإجمالي بعد الزيادة");
                                HeaderCell(table, "المدفوع");
                                HeaderCell(table, "المبلغ المستحق");
                                HeaderCell(table, "الحالة");

                                foreach (var c in section.Claims)
                                {
                                    DataCell(table, c.ProjectAssignment?.AssignmentType ?? "-");
                                    DataCell(table, c.Area.HasValue ? c.Area.Value.ToString("N1") : "-");
                                    DataCell(table, c.SalePricePerMeter.HasValue ? c.SalePricePerMeter.Value.ToString("N0") : "-");
                                    DataCell(table, c.Value.ToString("N0"));
                                    DataCell(table, c.SaleMarkupPercent1.HasValue ? c.SaleMarkupPercent1.Value.ToString("N1") + "%" : "-");
                                    DataCell(table, c.SaleMarkupPercent2.HasValue ? c.SaleMarkupPercent2.Value.ToString("N1") + "%" : "-");
                                    DataCell(table, c.ValueAfterPercentage.ToString("N0"));
                                    DataCell(table, c.RedesignIncreasePercentage.HasValue ? c.RedesignIncreasePercentage.Value.ToString("N1") + "%" : "-");
                                    DataCell(table, c.RealValue.ToString("N0"));
                                    DataCell(table, c.PaidAmount.ToString("N0"));
                                    DataCell(table, c.RemainingAmount.ToString("N0"));
                                    DataCell(table, c.IsClientSettled ? "تم التحصيل" : "بانتظار التحصيل");
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
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.1f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.1f);
                                    c.RelativeColumn(1.3f);
                                });
                                HeaderCell(table, "بيان التكليف");
                                HeaderCell(table, "المساحة");
                                HeaderCell(table, "سعر المتر");
                                HeaderCell(table, "قيمة البيع");
                                HeaderCell(table, "نسبة إعادة التصميم");
                                HeaderCell(table, "الإجمالي بعد الزيادة");
                                HeaderCell(table, "المدفوع");
                                HeaderCell(table, "المبلغ المستحق");
                                HeaderCell(table, "الحالة");

                                foreach (var c in section.Claims)
                                {
                                    DataCell(table, c.ProjectAssignment?.AssignmentType ?? "-");
                                    DataCell(table, c.Area.HasValue ? c.Area.Value.ToString("N1") : "-");
                                    DataCell(table, c.SalePricePerMeter.HasValue ? c.SalePricePerMeter.Value.ToString("N0") : "-");
                                    DataCell(table, c.Value.ToString("N0"));
                                    DataCell(table, c.RedesignIncreasePercentage.HasValue ? c.RedesignIncreasePercentage.Value.ToString("N1") + "%" : "-");
                                    DataCell(table, c.RealValue.ToString("N0"));
                                    DataCell(table, c.PaidAmount.ToString("N0"));
                                    DataCell(table, c.RemainingAmount.ToString("N0"));
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
        private static string NumberToArabicWords(decimal amount)
        {
            long number = (long)Math.Floor(amount);
            if (number == 0) return "صفر";

            string[] ones = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
            string[] teens = { "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
            string[] tens = { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
            string[] hundreds = { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };
            string[] scaleSingular = { "", "ألف", "مليون", "مليار" };
            string[] scaleDual = { "", "ألفان", "مليونان", "ملياران" };
            string[] scalePluralFew = { "", "آلاف", "ملايين", "مليارات" };

            string ConvertGroup(int n)
            {
                var parts = new List<string>();
                int h = n / 100, rem = n % 100;
                if (h > 0) parts.Add(hundreds[h]);
                if (rem > 0)
                {
                    if (rem < 10) parts.Add(ones[rem]);
                    else if (rem < 20) parts.Add(teens[rem - 10]);
                    else
                    {
                        int t = rem / 10, o = rem % 10;
                        parts.Add(o > 0 ? ones[o] + " و" + tens[t] : tens[t]);
                    }
                }
                return string.Join(" و", parts);
            }

            var groups = new List<int>();
            long n = number;
            while (n > 0) { groups.Add((int)(n % 1000)); n /= 1000; }
            if (groups.Count == 0) groups.Add(0);

            var resultParts = new List<string>();
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                int g = groups[i];
                if (g == 0) continue;
                string groupWords = ConvertGroup(g);
                if (i == 0)
                    resultParts.Add(groupWords);
                else if (g == 1)
                    resultParts.Add(scaleSingular[i]);
                else if (g == 2)
                    resultParts.Add(scaleDual[i]);
                else if (g >= 3 && g <= 10)
                    resultParts.Add(groupWords + " " + scalePluralFew[i]);
                else
                    resultParts.Add(groupWords + " " + scaleSingular[i]);
            }

            return string.Join(" و", resultParts);
        }
        public byte[] GeneratePaymentReceipt(ClaimPayment payment)
        {
            var claim = payment.FinancialClaim;
            var project = claim.Project;
            var client = project.Client;
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);
            var paidBefore = claim.PaidAmount - payment.Amount;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(10));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(50).Image(logoPath).FitArea();

                        header.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("إيصال استلام نقدية").FontSize(16).Bold().FontColor("#221837");
                                c.Item().Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(110).Border(1).BorderColor("#c9a15a").Padding(6).Column(c =>
                            {
                                c.Item().Text("رقم الإيصال").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{payment.Id:D6}").FontSize(13).Bold().FontColor("#221837");
                                c.Item().PaddingTop(4).Text($"{payment.PaidAt:yyyy-MM-dd}").FontSize(9);
                            });
                        });
                        header.Item().PaddingTop(8).BorderBottom(2).BorderColor("#c9a15a");
                    });

                    page.Content().PaddingTop(18).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text(t =>
                        {
                            t.Span("استلمنا من السيد/ة: ").FontSize(10.5f);
                            t.Span(client?.Name ?? "-").FontSize(11).Bold();
                        });
                        if (!string.IsNullOrWhiteSpace(client?.CompanyName))
                        {
                            col.Item().Text($"الشركة: {client.CompanyName}").FontSize(10);
                        }

                        col.Item().PaddingTop(6).Background("#f6f4fb").Padding(10).Column(c =>
                        {
                            c.Item().Text("مبلغاً وقدره").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{payment.Amount:N0} د.ل").FontSize(18).Bold().FontColor("#221837");
                            c.Item().PaddingTop(4).Text($"فقط {NumberToArabicWords(payment.Amount)} دينار ليبي لا غير").FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                        });

                        col.Item().Text(t =>
                        {
                            t.Span("وذلك عن: ").FontSize(10);
                            t.Span($"{(claim.ProjectAssignment?.AssignmentType ?? "-")} — مشروع {project.Code} - {project.Name}").FontSize(10).Bold();
                        });

                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            HeaderCell(table, "الإجمالي");
                            HeaderCell(table, "مدفوع سابقاً");
                            HeaderCell(table, "هذه الدفعة");
                            HeaderCell(table, "المتبقي");

                            DataCell(table, claim.RealValue.ToString("N0") + " د.ل");
                            DataCell(table, paidBefore.ToString("N0") + " د.ل");
                            DataCell(table, payment.Amount.ToString("N0") + " د.ل");
                            DataCell(table, claim.RemainingAmount.ToString("N0") + " د.ل");
                        });

                        col.Item().PaddingTop(30).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("توقيع المستلم").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(18).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                            row.ConstantItem(20);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("الختم").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(18).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                        });
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }
    }
}