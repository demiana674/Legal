using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using iText.Kernel.Pdf;
using iText.Forms;
using LegalMateAI.DTOs.ReadDTO;
 using ClosedXML.Excel;
namespace LegalMateAI.BLL.Services.Service
{
    public class PdfGenerationService
    {
        public PdfGenerationService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ========== توليد عقود من قوالب نصية ==========
        
        public byte[] GenerateContractPdf(string title, string contractNumber, string content, DateTime createdAt)
        {
            using var ms = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));
                    page.PageColor(Colors.White);

                    page.Header()
                        .Text(title)
                        .FontSize(24)
                        .FontColor(Colors.Yellow.Darken1)
                        .Bold()
                        .AlignCenter()
                        .Underline();

                    page.Content()
                        .Column(column =>
                        {
                            column.Item()
                                .Text($"رقم العقد: {contractNumber}")
                                .AlignCenter()
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .PaddingTop(20)
                                .Text(content)
                                .FontSize(12)
                                .FontFamily("Arial")
                                .LineHeight(1.6f);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Column(footer =>
                        {
                            footer.Item()
                                .Text("تم إنشاء هذا العقد بواسطة LegalMate AI - منصة المساعدة القانونية الذكية")
                                .FontSize(10);
                            footer.Item()
                                .Text($"تاريخ الإنشاء: {createdAt:dd/MM/yyyy HH:mm}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Lighten2);
                        });
                });
            })
            .GeneratePdf(ms);

            return ms.ToArray();
        }

        public byte[] GenerateContractWord(string title, string contractNumber, string content, DateTime createdAt)
        {
            var wordHtml = $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='UTF-8'>
<title>{title}</title>
<style>
    body {{
        font-family: 'Arial', sans-serif;
        margin: 40px;
        line-height: 1.6;
        direction: rtl;
    }}
    h1 {{
        color: #C8A84B;
        text-align: center;
        border-bottom: 2px solid #C8A84B;
        padding-bottom: 10px;
    }}
    .contract-number {{
        text-align: center;
        color: #666;
        margin-bottom: 30px;
    }}
    .content {{
        white-space: pre-wrap;
        direction: rtl;
        text-align: right;
    }}
    .footer {{
        margin-top: 50px;
        text-align: center;
        font-size: 12px;
        color: #999;
        border-top: 1px solid #ddd;
        padding-top: 20px;
    }}
</style>
</head>
<body>
    <h1>{title}</h1>
    <div class='contract-number'>رقم العقد: {contractNumber}</div>
    <div class='content'>{content.Replace("\n", "<br/>")}</div>
    <div class='footer'>
        تم إنشاء هذا العقد بواسطة LegalMate AI - منصة المساعدة القانونية الذكية<br/>
        تاريخ الإنشاء: {createdAt:dd/MM/yyyy HH:mm}
    </div>
</body>
</html>";

            return Encoding.UTF8.GetBytes(wordHtml);
        }

        // ========== تعبئة بيانات في PDF موجود (Predefined Templates) ==========
        
        /// <summary>
        /// يملأ البيانات في ملف PDF جاهز (يدعم AcroForms)
        /// </summary>
        public byte[]? FillPdfForm(string templatePath, Dictionary<string, string> fieldValues)
        {
            try
            {
                if (!File.Exists(templatePath))
                {
                    return null;
                }

                using var outputStream = new MemoryStream();
                
                // فتح ملف PDF القالب
                using var pdfReader = new PdfReader(templatePath);
                using var pdfWriter = new PdfWriter(outputStream);
                using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);
                
                // الحصول على النموذج
                var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
                
                // تعبئة الحقول
                foreach (var field in fieldValues)
                {
                    var formField = form.GetField(field.Key);
                    if (formField != null)
                    {
                        formField.SetValue(field.Value);
                    }
                }
                
                // تثبيت النموذج (جعله غير قابل للتعديل)
                form.FlattenFields();
                
                pdfDoc.Close();
                
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PdfGenerationService] Error filling PDF form: {ex.Message}");
                
                // Fallback: إنشاء PDF جديد مع البيانات
                return GenerateSimpleFilledPdf(fieldValues);
            }
        }

        /// <summary>
        /// إنشاء PDF بسيط كحل بديل عند فشل تعبئة النموذج
        /// </summary>
        private byte[] GenerateSimpleFilledPdf(Dictionary<string, string> fieldValues)
        {
            using var ms = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));
                    page.PageColor(Colors.White);

                    page.Header()
                        .Text("عقد رسمي")
                        .FontSize(22)
                        .FontColor(Colors.Yellow.Darken1)
                        .Bold()
                        .AlignCenter();

                    page.Content()
                        .Column(column =>
                        {
                            column.Item().PaddingTop(20);
                            
                            foreach (var field in fieldValues)
                            {
                                column.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem(3)
                                            .Text(field.Key)
                                            .FontSize(12)
                                            .FontColor(Colors.Grey.Darken2);
                                            
                                        row.RelativeItem(7)
                                            .Text(field.Value)
                                            .FontSize(12)
                                            .Bold();
                                    });
                                column.Item().PaddingTop(10);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"تم إنشاء هذا العقد بواسطة LegalMate AI - {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Lighten2);
                });
            })
            .GeneratePdf(ms);

            return ms.ToArray();
        }

        // ========== تصدير Admin Logs كـ PDF ==========
        
        /// <summary>
        /// تصدير سجلات الأدمن كتقرير PDF احترافي
        /// </summary>
        public byte[] ExportAdminLogsToPdf(List<AdminLogDto> logs, string adminName, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var ms = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                    page.PageColor(Colors.White);

                    // ===== Header =====
                    page.Header()
                        .Column(header =>
                        {
                            header.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem(3)
                                        .Text("النظام القضائي الإلكتروني - LegalMate AI")
                                        .FontSize(14)
                                        .FontColor(Colors.Yellow.Darken1)
                                        .Bold();
                                        
                                    row.RelativeItem(1)
                                        .AlignRight()
                                        .Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Medium);
                                });

                            header.Item()
                                .PaddingTop(5)
                                .Text("تقرير سجل النشاطات الإدارية")
                                .FontSize(12)
                                .Bold()
                                .AlignCenter();

                            header.Item()
                                .PaddingTop(5)
                                .Row(infoRow =>
                                {
                                    infoRow.RelativeItem(1)
                                        .Text($"المسؤول: {adminName}")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken2);
                                        
                                    if (fromDate.HasValue && toDate.HasValue)
                                    {
                                        infoRow.RelativeItem(1)
                                            .AlignRight()
                                            .Text($"الفترة: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken2);
                                    }
                                });

                            header.Item()
                                .PaddingTop(5)
                                .Text($"إجمالي السجلات: {logs.Count}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2);

                            header.Item()
                                .PaddingTop(10)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten2);
                        });

                    // ===== Content =====
                    page.Content()
                        .PaddingTop(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.5f);  // التاريخ والوقت
                                columns.RelativeColumn(2);      // الإجراء
                                columns.RelativeColumn(1.5f);   // النوع
                                columns.RelativeColumn(1.5f);   // المسؤول
                                columns.RelativeColumn(2);      // معرف الهدف
                            });

                            // Header Row
                            table.Header(header =>
                            {
                                // خلية التاريخ والوقت
                                header.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Yellow.Lighten5)
                                    .Padding(5)
                                    .AlignCenter()
                                    .Text("التاريخ والوقت")
                                    .FontSize(9)
                                    .Bold();
                                    
                                // خلية الإجراء
                                header.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Yellow.Lighten5)
                                    .Padding(5)
                                    .AlignCenter()
                                    .Text("الإجراء")
                                    .FontSize(9)
                                    .Bold();
                                    
                                // خلية النوع
                                header.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Yellow.Lighten5)
                                    .Padding(5)
                                    .AlignCenter()
                                    .Text("النوع")
                                    .FontSize(9)
                                    .Bold();
                                    
                                // خلية المسؤول
                                header.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Yellow.Lighten5)
                                    .Padding(5)
                                    .AlignCenter()
                                    .Text("المسؤول")
                                    .FontSize(9)
                                    .Bold();
                                    
                                // خلية معرف الهدف
                                header.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Background(Colors.Yellow.Lighten5)
                                    .Padding(5)
                                    .AlignCenter()
                                    .Text("معرف الهدف")
                                    .FontSize(9)
                                    .Bold();
                            });

                            // Data Rows
                            foreach (var log in logs)
                            {
                                // خلية التاريخ والوقت
                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .Text(log.TimestampFormatted)
                                    .FontSize(8);
                                    
                                // خلية الإجراء
                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .Text($"{log.ActionIcon} {log.ActionName}")
                                    .FontSize(8);
                                    
                                // خلية النوع
                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .Text(log.TargetTypeAr)
                                    .FontSize(8);
                                    
                                // خلية المسؤول
                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .Text(log.Name)
                                    .FontSize(8);
                                    
                                // خلية معرف الهدف
                                table.Cell()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .Text(log.TargetId.ToString())
                                    .FontSize(8);
                            }
                        });

                    // ===== Footer =====
                    page.Footer()
                        .AlignCenter()
                        .Column(footer =>
                        {
                            footer.Item()
                                .PaddingTop(10)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten2);
                                
                            footer.Item()
                                .PaddingTop(5)
                                .Text("تم إنشاء هذا التقرير بواسطة LegalMate AI")
                                .FontSize(7)
                                .FontColor(Colors.Grey.Medium);
                        });
                });
            })
            .GeneratePdf(ms);

            return ms.ToArray();
        }

      

public byte[] ExportAdminLogsToExcel(List<AdminLogDto> logs)
{
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Logs");

    ws.Cell(1, 1).Value = "التاريخ والوقت";
    ws.Cell(1, 2).Value = "الإجراء";
    ws.Cell(1, 3).Value = "النوع";
    ws.Cell(1, 4).Value = "المسؤول";
    ws.Cell(1, 5).Value = "معرف الهدف";

    int row = 2;

    foreach (var log in logs)
    {
        ws.Cell(row, 1).Value = log.TimestampFormatted;
        ws.Cell(row, 2).Value = log.ActionName;
        ws.Cell(row, 3).Value = log.TargetTypeAr;
        ws.Cell(row, 4).Value = log.Name;
        ws.Cell(row, 5).Value = log.TargetId.ToString();
        row++;
    }

    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);

    return stream.ToArray();
}
        
    } 
}