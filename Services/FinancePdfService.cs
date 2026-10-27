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

            void ClaimsHeaderCell(TableDescriptor t, string text) =>
                t.Cell().Background("#221837").Padding(3).Text(text).FontColor(Colors.White).Bold().FontSize(9);

            void ClaimsDataCell(TableDescriptor t, string text) =>
                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text).FontSize(8.5f);

            return Document.Create(container =>
            {
                foreach (var section in sections)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(25);
                        page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(9));
                        page.ContentFromRightToLeft();

                        page.Header().Column(header =>
                        {
                            header.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    if (hasLogo)
                                        c.Item().Height(35).Image(logoPath).FitArea();
                                    c.Item().PaddingTop(4).Text(title).FontSize(13).Bold().FontColor("#221837");
                                    c.Item().Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                                });
                                row.ConstantItem(140).Border(1).BorderColor("#c9a15a").Padding(5).Column(c =>
                                {
                                    c.Item().Text("رقم المشروع").FontSize(8).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(section.Project.Code).FontSize(12).Bold().FontColor("#221837");
                                    c.Item().PaddingTop(2).Text(t =>
                                    {
                                        t.Span("التاريخ : ").FontSize(8).FontColor(Colors.Grey.Medium);
                                        t.Span($"{DateTime.UtcNow:yyyy-MM-dd}").FontSize(8);
                                    });
                                    c.Item().Text(t =>
                                    {
                                        t.Span("الموافق : ").FontSize(8).FontColor(Colors.Grey.Medium);
                                        t.Span(ArabicDayName(DateTime.UtcNow)).FontSize(8);
                                    });
                                });
                            });
                            header.Item().PaddingTop(6).BorderBottom(2).BorderColor("#c9a15a");
                            header.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("المشروع: ").FontSize(9);
                                t.Span($"{section.Project.Code} - {section.Project.Name}").FontSize(9).Bold();
                            });
                        });

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Spacing(10);

                            col.Item().Table(table =>
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
                                    ClaimsHeaderCell(table, "بيان التكليف");
                                    ClaimsHeaderCell(table, "المساحة");
                                    ClaimsHeaderCell(table, "سعر المتر");
                                    ClaimsHeaderCell(table, "قيمة البيع");
                                    ClaimsHeaderCell(table, "نسبة 1");
                                    ClaimsHeaderCell(table, "نسبة 2");
                                    ClaimsHeaderCell(table, "القيمة بعد النسبة");
                                    ClaimsHeaderCell(table, "نسبة إعادة التصميم");
                                    ClaimsHeaderCell(table, "الإجمالي بعد الزيادة");
                                    ClaimsHeaderCell(table, "المدفوع");
                                    ClaimsHeaderCell(table, "المبلغ المستحق");
                                    ClaimsHeaderCell(table, "الحالة");

                                    foreach (var c in section.Claims)
                                    {
                                        ClaimsDataCell(table, c.ProjectAssignment?.Stage?.Name ?? "-");
                                        ClaimsDataCell(table, c.Area.HasValue ? c.Area.Value.ToString("N1") : "-");
                                        ClaimsDataCell(table, c.SalePricePerMeter.HasValue ? c.SalePricePerMeter.Value.ToString("N0") : "-");
                                        ClaimsDataCell(table, c.Value.ToString("N0"));
                                        ClaimsDataCell(table, c.SaleMarkupPercent1.HasValue ? c.SaleMarkupPercent1.Value.ToString("N1") + "%" : "-");
                                        ClaimsDataCell(table, c.SaleMarkupPercent2.HasValue ? c.SaleMarkupPercent2.Value.ToString("N1") + "%" : "-");
                                        ClaimsDataCell(table, c.ValueAfterPercentage.ToString("N0"));
                                        ClaimsDataCell(table, c.RedesignIncreasePercentage.HasValue ? c.RedesignIncreasePercentage.Value.ToString("N1") + "%" : "-");
                                        ClaimsDataCell(table, c.RealValue.ToString("N0"));
                                        ClaimsDataCell(table, c.PaidAmount.ToString("N0"));
                                        ClaimsDataCell(table, c.RemainingAmount.ToString("N0"));
                                        ClaimsDataCell(table, c.IsClientSettled ? "تم التحصيل" : "بانتظار التحصيل");
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
                                    ClaimsHeaderCell(table, "بيان التكليف");
                                    ClaimsHeaderCell(table, "المساحة");
                                    ClaimsHeaderCell(table, "سعر المتر");
                                    ClaimsHeaderCell(table, "قيمة البيع");
                                    ClaimsHeaderCell(table, "نسبة إعادة التصميم");
                                    ClaimsHeaderCell(table, "الإجمالي بعد الزيادة");
                                    ClaimsHeaderCell(table, "المدفوع");
                                    ClaimsHeaderCell(table, "المبلغ المستحق");
                                    ClaimsHeaderCell(table, "الحالة");

                                    foreach (var c in section.Claims)
                                    {
                                        ClaimsDataCell(table, c.ProjectAssignment?.Stage?.Name ?? "-");
                                        ClaimsDataCell(table, c.Area.HasValue ? c.Area.Value.ToString("N1") : "-");
                                        ClaimsDataCell(table, c.SalePricePerMeter.HasValue ? c.SalePricePerMeter.Value.ToString("N0") : "-");
                                        ClaimsDataCell(table, c.Value.ToString("N0"));
                                        ClaimsDataCell(table, c.RedesignIncreasePercentage.HasValue ? c.RedesignIncreasePercentage.Value.ToString("N1") + "%" : "-");
                                        ClaimsDataCell(table, c.RealValue.ToString("N0"));
                                        ClaimsDataCell(table, c.PaidAmount.ToString("N0"));
                                        ClaimsDataCell(table, c.RemainingAmount.ToString("N0"));
                                        ClaimsDataCell(table, c.IsClientSettled ? "تم التحصيل" : "بانتظار التحصيل");
                                    }
                                }
                            });

                            col.Item().PaddingTop(16).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("اعتماد المدير العام").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                    c.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                                });
                                row.ConstantItem(20);
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("اعتماد المالي").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                    c.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                                });
                            });
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
        private void ImageCell(TableDescriptor table, string? relativePath)
        {
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Column(c =>
            {
                var fullPath = string.IsNullOrEmpty(relativePath)
                    ? null
                    : Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (fullPath != null && File.Exists(fullPath))
                    c.Item().Height(35).Image(fullPath).FitArea();
                else
                    c.Item().Text("-");
            });
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

        private static string ArabicDayName(DateTime date) => date.DayOfWeek switch
        {
            DayOfWeek.Sunday => "الأحد",
            DayOfWeek.Monday => "الاثنين",
            DayOfWeek.Tuesday => "الثلاثاء",
            DayOfWeek.Wednesday => "الأربعاء",
            DayOfWeek.Thursday => "الخميس",
            DayOfWeek.Friday => "الجمعة",
            DayOfWeek.Saturday => "السبت",
            _ => ""
        };
        public byte[] GeneratePaymentReceipt(ClaimPayment payment, List<ClaimPayment> previousPayments)
        {
            var claim = payment.FinancialClaim;
            var project = claim.Project;
            var client = project.Client;
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);
            var paidBefore = previousPayments.Sum(p => p.Amount);
            var remainingAtThisPayment = claim.RealValue - paidBefore - payment.Amount;
            var stageName = claim.ProjectAssignment?.Stage?.Name ?? "-";

            void ReceiptHeaderCell(TableDescriptor t, string text) =>
                t.Cell().Background("#221837").Padding(3).Text(text).FontColor(Colors.White).Bold().FontSize(9.5f);

            void ReceiptDataCell(TableDescriptor t, string text) =>
                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text).FontSize(8.5f);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(8.5f));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(30).Image(logoPath).FitArea();

                        header.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("إيصال استلام نقدية").FontSize(12).Bold().FontColor("#221837");
                                c.Item().Text("أثر للتصاميم والاستشارات الهندسية").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(110).Border(1).BorderColor("#c9a15a").Padding(4).Column(c =>
                            {
                                var receiptCode = $"{project.Code}-{payment.ReceiptNumber:D4}";
                                c.Item().Text("رقم الإيصال").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                c.Item().Text(receiptCode).FontSize(11).Bold().FontColor("#221837");
                                c.Item().PaddingTop(2).Text($"{payment.PaidAt:yyyy-MM-dd}").FontSize(7.5f);
                            });
                        });
                        header.Item().PaddingTop(4).BorderBottom(2).BorderColor("#c9a15a");
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Spacing(5);

                        col.Item().Text(t =>
                        {
                            t.Span("استلمنا من السيد/ة: ").FontSize(9);
                            t.Span(client?.Name ?? "-").FontSize(9).Bold();
                        });
                        if (!string.IsNullOrWhiteSpace(client?.CompanyName))
                        {
                            col.Item().Text($"الشركة: {client.CompanyName}").FontSize(9);
                        }

                        col.Item().PaddingTop(3).Background("#f6f4fb").Padding(6).Column(c =>
                        {
                            c.Item().Text("مبلغاً وقدره").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{payment.Amount:N2} د.ل").FontSize(14).Bold().FontColor("#221837");
                            c.Item().PaddingTop(2).Text($"فقط {NumberToArabicWords(payment.Amount)} دينار ليبي لا غير").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });

                        col.Item().Text(t =>
                        {
                            t.Span("المرحلة: ").FontSize(9);
                            t.Span(stageName).FontSize(9).Bold();
                        });
                        col.Item().Text(t =>
                        {
                            t.Span("وذلك عن: ").FontSize(9);
                            t.Span($"{stageName} — مشروع {project.Code} - {project.Name}").FontSize(9).Bold();
                        });

                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        if (previousPayments.Any())
                        {
                            col.Item().Text("الدفعات السابقة").FontSize(9.5f).Bold().FontColor("#221837");
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });
                                ReceiptHeaderCell(table, "تاريخ الدفعة");
                                ReceiptHeaderCell(table, "المبلغ");

                                foreach (var p in previousPayments)
                                {
                                    ReceiptDataCell(table, p.PaidAt.ToString("yyyy-MM-dd"));
                                    ReceiptDataCell(table, p.Amount.ToString("N2") + " د.ل");
                                }
                            });
                        }

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            ReceiptHeaderCell(table, "الإجمالي");
                            ReceiptHeaderCell(table, "مدفوع سابقاً");
                            ReceiptHeaderCell(table, "هذه الدفعة");
                            ReceiptHeaderCell(table, "المتبقي");

                            ReceiptDataCell(table, claim.RealValue.ToString("N2") + " د.ل");
                            ReceiptDataCell(table, paidBefore.ToString("N2") + " د.ل");
                            ReceiptDataCell(table, payment.Amount.ToString("N2") + " د.ل");
                            ReceiptDataCell(table, remainingAtThisPayment.ToString("N2") + " د.ل");
                        });

                        col.Item().PaddingTop(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("توقيع المستلم").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                            row.ConstantItem(18);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("الختم").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                        });
                    });

                    page.Footer().AlignCenter().PaddingTop(4).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(7).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        public byte[] GenerateProjectInvoice(Project project, List<FinancialClaim> claims)
        {
            var client = project.Client;
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);
            var totalValue = claims.Sum(c => c.RealValue);
            var totalPaid = claims.Sum(c => c.PaidAmount);
            var totalRemaining = claims.Sum(c => c.RemainingAmount);

            void InvoiceHeaderCell(TableDescriptor t, string text) =>
           t.Cell().Background("#221837").Padding(3).Text(text).FontColor(Colors.White).Bold().FontSize(9.5f);

            void InvoiceDataCell(TableDescriptor t, string text) =>
                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text).FontSize(8.5f);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(8.5f));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        if (hasLogo)
                            header.Item().AlignCenter().Height(30).Image(logoPath).FitArea();

                        header.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("فاتورة").FontSize(12).Bold().FontColor("#221837");
                                c.Item().Text("أثر للتصاميم والاستشارات الهندسية").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(100).Border(1).BorderColor("#c9a15a").Padding(4).Column(c =>
                            {
                                c.Item().Text("رقم المشروع").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                c.Item().Text(project.Code).FontSize(11).Bold().FontColor("#221837");
                                c.Item().PaddingTop(2).Text(t =>
                                {
                                    t.Span("التاريخ : ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                    t.Span($"{DateTime.UtcNow:yyyy-MM-dd}").FontSize(7.5f);
                                });
                                c.Item().Text(t =>
                                {
                                    t.Span("الموافق : ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                    t.Span(ArabicDayName(DateTime.UtcNow)).FontSize(7.5f);
                                });
                            });
                        });
                        header.Item().PaddingTop(4).BorderBottom(2).BorderColor("#c9a15a");
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        col.Spacing(5);

                        col.Item().Text(t =>
                        {
                            t.Span("السادة: ").FontSize(9);
                            t.Span(client?.Name ?? "-").FontSize(9).Bold();
                        });
                        if (!string.IsNullOrWhiteSpace(client?.CompanyName))
                        {
                            col.Item().Text($"الشركة: {client.CompanyName}").FontSize(9);
                        }
                        col.Item().Text(t =>
                        {
                            t.Span("المشروع: ").FontSize(9);
                            t.Span(project.Name).FontSize(9).Bold();
                        });

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(34);
                                c.RelativeColumn(3);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            InvoiceHeaderCell(table, "الرقم");
                            InvoiceHeaderCell(table, "بيانات المشروع");
                            InvoiceHeaderCell(table, "المساحة");
                            InvoiceHeaderCell(table, "سعر المتر");
                            InvoiceHeaderCell(table, "القيمة");

                            int i = 1;
                            foreach (var claim in claims)
                            {
                                InvoiceDataCell(table, (i++).ToString("D3"));
                                InvoiceDataCell(table, claim.ProjectAssignment?.Stage?.Name ?? "-");
                                InvoiceDataCell(table, (claim.Area ?? 0).ToString("N2"));
                                InvoiceDataCell(table, (claim.SalePricePerMeter ?? 0).ToString("N2"));
                                InvoiceDataCell(table, claim.RealValue.ToString("N2") + " د.ل");
                            }
                        });

                        col.Item().PaddingTop(4).AlignLeft().Width(150).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });
                            InvoiceDataCell(table, "الإجمالي");
                            InvoiceDataCell(table, totalValue.ToString("N2") + " د.ل");
                            InvoiceDataCell(table, "المدفوع");
                            InvoiceDataCell(table, totalPaid > 0 ? totalPaid.ToString("N2") + " د.ل" : "-");
                            InvoiceDataCell(table, "الباقي");
                            InvoiceDataCell(table, totalRemaining > 0 ? totalRemaining.ToString("N2") + " د.ل" : "-");
                        });

                        col.Item().PaddingTop(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("اعتماد المدير العام").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                            row.ConstantItem(18);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("اعتماد المالي").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                        });
                    });

                    page.Footer().AlignCenter().PaddingTop(4).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(7).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        public byte[] GenerateProjectProfitReport(Project project, ProjectCalculationService.ProjectNetProfitResult result)
        {
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo-full.png");
            var hasLogo = File.Exists(logoPath);

            void ProfitHeaderCell(TableDescriptor t, string text) =>
                t.Cell().Background("#221837").Padding(3).Text(text).FontColor(Colors.White).Bold().FontSize(9);

            void ProfitDataCell(TableDescriptor t, string text) =>
                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text).FontSize(8.5f);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(9));
                    page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                if (hasLogo)
                                    c.Item().Height(35).Image(logoPath).FitArea();
                                c.Item().PaddingTop(4).Text("تقرير الربح والخسارة").FontSize(13).Bold().FontColor("#221837");
                                c.Item().Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(140).Border(1).BorderColor("#c9a15a").Padding(5).Column(c =>
                            {
                                c.Item().Text("رقم المشروع").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text(project.Code).FontSize(12).Bold().FontColor("#221837");
                                c.Item().PaddingTop(2).Text(t =>
                                {
                                    t.Span("التاريخ : ").FontSize(8).FontColor(Colors.Grey.Medium);
                                    t.Span($"{DateTime.UtcNow:yyyy-MM-dd}").FontSize(8);
                                });
                                c.Item().Text(t =>
                                {
                                    t.Span("الموافق : ").FontSize(8).FontColor(Colors.Grey.Medium);
                                    t.Span(ArabicDayName(DateTime.UtcNow)).FontSize(8);
                                });
                            });
                        });
                        header.Item().PaddingTop(6).BorderBottom(2).BorderColor("#c9a15a");
                        header.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("المشروع: ").FontSize(9);
                            t.Span($"{project.Code} - {project.Name}").FontSize(9).Bold();
                        });
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(14);

                        col.Item().Text("جدول البيع النهائي").FontSize(11).Bold().FontColor("#221837");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            ProfitHeaderCell(table, "بيان التكليف");
                            ProfitHeaderCell(table, "القيمة");
                            ProfitHeaderCell(table, "المدفوع");
                            ProfitHeaderCell(table, "المتبقي");
                            ProfitHeaderCell(table, "الحالة");

                            foreach (var c in result.Claims)
                            {
                                ProfitDataCell(table, c.ProjectAssignment?.Stage?.Name ?? "-");
                                ProfitDataCell(table, c.RealValue.ToString("N0"));
                                ProfitDataCell(table, c.PaidAmount.ToString("N0"));
                                ProfitDataCell(table, c.RemainingAmount.ToString("N0"));
                                ProfitDataCell(table, c.IsClientSettled ? "تم التحصيل" : "بانتظار التحصيل");
                            }
                        });

                        col.Item().Text("جدول التكاليف").FontSize(11).Bold().FontColor("#221837");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            ProfitHeaderCell(table, "بيان التكليف");
                            ProfitHeaderCell(table, "المهندس");
                            ProfitHeaderCell(table, "القيمة");
                            ProfitHeaderCell(table, "الحالة");

                            foreach (var r in result.Records)
                            {
                                ProfitDataCell(table, r.ProjectAssignment?.Stage?.Name ?? "-");
                                ProfitDataCell(table, r.Engineer?.FullName ?? "-");
                                ProfitDataCell(table, r.ValueAfterPercentage.ToString("N0"));
                                ProfitDataCell(table, r.IsCleared ? "مصروف" : "غير مصروف");
                            }
                        });

                        col.Item().Text("جدول المصروفات").FontSize(11).Bold().FontColor("#221837");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            ProfitHeaderCell(table, "التصنيف");
                            ProfitHeaderCell(table, "الوصف");
                            ProfitHeaderCell(table, "التاريخ");
                            ProfitHeaderCell(table, "المبلغ");

                            foreach (var e in result.GeneralExpenseRows)
                            {
                                ProfitDataCell(table, e.ExpenseCategory?.NameAr ?? "-");
                                ProfitDataCell(table, e.Description ?? "-");
                                ProfitDataCell(table, e.Date.ToString("yyyy-MM-dd"));
                                ProfitDataCell(table, e.Amount.ToString("N0"));
                            }
                        });

                        col.Item().PaddingTop(6).Background("#f6f4fb").Padding(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            ProfitHeaderCell(table, "صافي الربح المستحق");
                            ProfitHeaderCell(table, "صافي الربح المحصَّل");
                            ProfitDataCell(table, result.AccruedNetProfit.ToString("N0") + " د.ل");
                            ProfitDataCell(table, result.RealizedNetProfit.ToString("N0") + " د.ل");
                        });

                        col.Item().PaddingTop(16).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("اعتماد المدير العام").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                            row.ConstantItem(20);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("اعتماد المالي").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            });
                        });
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        public byte[] GenerateSupplyRequestsPdf(List<SiteSupplyRequestItem> items)
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
                        header.Item().PaddingTop(8).BorderBottom(2).BorderColor("#c9a15a")
                            .PaddingBottom(6).AlignCenter().Text("طلبات التوريد").FontSize(16).Bold().FontColor("#221837");
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(50);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn();
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        HeaderCell(table, "الصورة");
                        HeaderCell(table, "المشروع");
                        HeaderCell(table, "الموقع");
                        HeaderCell(table, "المادة");
                        HeaderCell(table, "الكمية");
                        HeaderCell(table, "الوحدة");
                        HeaderCell(table, "المورّد");
                        HeaderCell(table, "التصنيف");

                        foreach (var item in items)
                        {
                            ImageCell(table, item.PhotoPath);
                            DataCell(table, item.SiteSupplyRequest.Site?.Project?.Name ?? "-");
                            DataCell(table, item.SiteSupplyRequest.Site?.Name ?? "-");
                            DataCell(table, item.MaterialName);
                            DataCell(table, item.Quantity.ToString("N2"));
                            DataCell(table, item.Unit);
                            DataCell(table, item.Vendor?.Name ?? "-");
                            DataCell(table, item.MaterialItem?.MaterialCategory?.NameAr ?? "-");
                        }
                    });

                    page.Footer().AlignCenter().PaddingTop(10).Text("أثر للتصاميم والاستشارات الهندسية").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }
    }
}