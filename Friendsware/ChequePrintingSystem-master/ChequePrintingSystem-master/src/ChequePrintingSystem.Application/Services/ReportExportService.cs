using ChequePrintingSystem.Application.DTOs;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ChequePrintingSystem.Application.Services;

public interface IReportExportService
{
    byte[] ExportExcel(IReadOnlyList<ChequeDto> rows);
    byte[] ExportPdf(IReadOnlyList<ChequeDto> rows, string title);
}

public class ReportExportService : IReportExportService
{
    /// <summary>
    /// Column order matches bulk import: ChequeNumber, Date, PayeeName, Amount, AccountNumber, Status, Remarks.
    /// </summary>
    public byte[] ExportExcel(IReadOnlyList<ChequeDto> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Cheque Report");
        ws.Cell(1, 1).Value = "ChequeNumber";
        ws.Cell(1, 2).Value = "Date";
        ws.Cell(1, 3).Value = "PayeeName";
        ws.Cell(1, 4).Value = "Amount";
        ws.Cell(1, 5).Value = "AccountNumber";
        ws.Cell(1, 6).Value = "Status";
        ws.Cell(1, 7).Value = "Remarks";
        ws.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var r = i + 2;
            ws.Cell(r, 1).Value = row.ChequeNumber;
            ws.Cell(r, 2).Value = row.Date.ToDateTime(TimeOnly.MinValue);
            ws.Cell(r, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(r, 3).Value = row.PayeeName;
            ws.Cell(r, 4).Value = row.Amount;
            ws.Cell(r, 5).Value = row.BankAccountNumber;
            ws.Cell(r, 6).Value = row.Status.ToString();
            ws.Cell(r, 7).Value = row.Remarks ?? string.Empty;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportPdf(IReadOnlyList<ChequeDto> rows, string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text(title).Bold().FontSize(16);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1.6f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Cheque #");
                            header.Cell().Element(CellStyle).Text("Date");
                            header.Cell().Element(CellStyle).Text("Payee");
                            header.Cell().Element(CellStyle).Text("Amount");
                            header.Cell().Element(CellStyle).Text("Account");
                            header.Cell().Element(CellStyle).Text("Status");
                            header.Cell().Element(CellStyle).Text("Remarks");
                        });

                        foreach (var row in rows)
                        {
                            table.Cell().Element(CellStyle).Text(row.ChequeNumber);
                            table.Cell().Element(CellStyle).Text(row.Date.ToString("yyyy-MM-dd"));
                            table.Cell().Element(CellStyle).Text(row.PayeeName);
                            table.Cell().Element(CellStyle).Text(row.Amount.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(row.BankAccountNumber);
                            table.Cell().Element(CellStyle).Text(row.Status.ToString());
                            table.Cell().Element(CellStyle).Text(row.Remarks ?? "");
                        }
                    });
                });
            })
            .GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
}
