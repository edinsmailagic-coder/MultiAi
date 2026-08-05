using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace MultiAI.Services
{
    public interface IAttachmentTextExtractor
    {
        string? Extract(string fileName, string contentType, byte[] data);
    }

    public class AttachmentTextExtractor : IAttachmentTextExtractor
    {
        private const int MaxExtractChars = 40_000;

        public string? Extract(string fileName, string contentType, byte[] data)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            try
            {
                var text = ext switch
                {
                    ".txt" or ".csv" or ".md" => Encoding.UTF8.GetString(data),
                    ".pdf" => ExtractPdf(data),
                    ".docx" => ExtractDocx(data),
                    ".xlsx" => ExtractXlsx(data),
                    ".doc" or ".xls" => throw new Exception("Legacy .doc/.xls are not supported. Use .docx/.xlsx."),
                    _ when contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => null,
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(text))
                    return text;

                text = text.Trim();
                return text.Length > MaxExtractChars ? text[..MaxExtractChars] : text;
            }
            catch (Exception ex) when (ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to extract text from '{fileName}': {ex.Message}");
            }
        }

        private static string ExtractPdf(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var document = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private static string ExtractDocx(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var document = WordprocessingDocument.Open(stream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            return body?.InnerText ?? string.Empty;
        }

        private static string ExtractXlsx(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var document = SpreadsheetDocument.Open(stream, false);
            var workbookPart = document.WorkbookPart
                ?? throw new Exception("Invalid spreadsheet.");
            var sb = new StringBuilder();
            var shared = workbookPart.SharedStringTablePart?.SharedStringTable;

            foreach (var sheet in workbookPart.Workbook.Sheets?.OfType<Sheet>() ?? [])
            {
                if (sheet.Id?.Value is null)
                    continue;

                var part = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
                var rows = part.Worksheet.Descendants<Row>();
                foreach (var row in rows)
                {
                    var values = row.Elements<Cell>().Select(c => GetCellText(c, shared));
                    sb.AppendLine(string.Join('\t', values));
                }
            }

            return sb.ToString();
        }

        private static string GetCellText(Cell cell, SharedStringTable? shared)
        {
            var value = cell.CellValue?.InnerText ?? string.Empty;
            if (cell.DataType?.Value == CellValues.SharedString &&
                int.TryParse(value, out var index) &&
                shared != null &&
                index >= 0 &&
                index < shared.ChildElements.Count)
            {
                return shared.ElementAt(index).InnerText;
            }

            return value;
        }
    }
}
