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
                        page.DefaultTextStyle(x => x.FontFamily("AtharArabic").FontSize(9));
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
                    page.Size(PageSizes.A5);
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
                    page.Size(PageSizes.A5);
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
                        if (hasLogo)
                            header.Item().AlignCenter().Height(45).Image(logoPath).FitArea();

                        header.Item().PaddingTop(6).BorderBottom(2).BorderColor("#c9a15a")
                            .PaddingBottom(5).AlignCenter().Text("تقرير الربح والخسارة").FontSize(15).Bold().FontColor("#221837");

                        header.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text($"رقم المشروع: {project.Code} - {project.Name}");
                            row.RelativeItem().AlignLeft().Text($"تاريخ التصدير: {DateTime.UtcNow:yyyy-MM-dd}");
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
                            HeaderCell(table, "بيان التكليف");
                            HeaderCell(table, "القيمة");
                            HeaderCell(table, "المدفوع");
                            HeaderCell(table, "المتبقي");
                            HeaderCell(table, "الحالة");

                            foreach (var c in result.Claims)
                            {
                                DataCell(table, c.ProjectAssignment?.AssignmentType ?? "-");
                                DataCell(table, c.RealValue.ToString("N0"));
                                DataCell(table, c.PaidAmount.ToString("N0"));
                                DataCell(table, c.RemainingAmount.ToString("N0"));
                                DataCell(table, c.IsClientSettled ? "تم التحصيل" : "بانتظار التحصيل");
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
                            HeaderCell(table, "بيان التكليف");
                            HeaderCell(table, "المهندس");
                            HeaderCell(table, "القيمة");
                            HeaderCell(table, "الحالة");

                            foreach (var r in result.Records)
                            {
                                DataCell(table, r.ProjectAssignment?.AssignmentType ?? "-");
                                DataCell(table, r.Engineer?.FullName ?? "-");
                                DataCell(table, r.ValueAfterPercentage.ToString("N0"));
                                DataCell(table, r.IsCleared ? "مصروف" : "غير مصروف");
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
                            HeaderCell(table, "التصنيف");
                            HeaderCell(table, "الوصف");
                            HeaderCell(table, "التاريخ");
                            HeaderCell(table, "المبلغ");

                            foreach (var e in result.GeneralExpenseRows)
                            {
                                DataCell(table, e.ExpenseCategory?.NameAr ?? "-");
                                DataCell(table, e.Description ?? "-");
                                DataCell(table, e.Date.ToString("yyyy-MM-dd"));
                                DataCell(table, e.Amount.ToString("N0"));
                            }
                        });

                        col.Item().PaddingTop(6).Background("#f6f4fb").Padding(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            HeaderCell(table, "صافي الربح المستحق");
                            HeaderCell(table, "صافي الربح المحصَّل");
                            DataCell(table, result.AccruedNetProfit.ToString("N0") + " د.ل");
                            DataCell(table, result.RealizedNetProfit.ToString("N0") + " د.ل");
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