using System;
using System.IO;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LegalMateAI.BLL.Services.Service
{
    public class PdfGenerationService
    {
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
                        .FontColor(Colors.Yellow.Darken1) // بدل Gold
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
                                .Text(content)
                                .FontSize(12)
                                .FontFamily("Arial")
                                .AlignRight() // بدل DirectionRightToLeft
                                .LineHeight(1.6f);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Column(footer =>
                        {
                            footer.Item()
                                .Text($"تم إنشاء هذا العقد بواسطة LegalMate AI - منصة المساعدة القانونية الذكية");
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
    }
}
