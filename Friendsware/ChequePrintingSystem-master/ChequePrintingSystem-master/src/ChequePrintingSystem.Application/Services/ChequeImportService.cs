using System.Globalization;
using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Application.DTOs;
using ClosedXML.Excel;
using FluentValidation;

namespace ChequePrintingSystem.Application.Services;

public interface IChequeImportService
{
    Task<ChequeImportResult> ImportFromExcelAsync(Stream stream, CancellationToken cancellationToken = default);

    byte[] GetImportTemplateBytes();
}

public class ChequeImportService(IChequeService chequeService, IUnitOfWork unitOfWork) : IChequeImportService
{
    public async Task<ChequeImportResult> ImportFromExcelAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RowsUsed().Skip(1).ToList();
        var payees = await unitOfWork.Payees.ListAsync(cancellationToken: cancellationToken);
        var accounts = await unitOfWork.BankAccounts.ListAsync(cancellationToken: cancellationToken);
        var importedCount = 0;
        var skippedCount = 0;
        var issues = new List<string>();

        foreach (var row in rows)
        {
            var sheetRow = row.RowNumber();
            var chequeNumber = row.Cell(1).GetString().Trim();
            var payeeName = row.Cell(3).GetString().Trim();
            var accountNumber = row.Cell(5).GetString().Trim();

            if (string.IsNullOrWhiteSpace(chequeNumber) && string.IsNullOrWhiteSpace(payeeName)
                                                       && string.IsNullOrWhiteSpace(accountNumber))
            {
                continue;
            }

            if (!TryReadDateOnly(row.Cell(2), out var date))
            {
                skippedCount++;
                issues.Add($"Row {sheetRow}: Invalid or missing Date.");
                continue;
            }

            var amount = row.Cell(4).TryGetValue<decimal>(out var parsedAmount)
                ? parsedAmount
                : 0m;
            var statusRaw = row.Cell(6).GetString().Trim();
            var remarks = row.Cell(7).GetString().Trim();

            if (string.IsNullOrWhiteSpace(chequeNumber) || string.IsNullOrWhiteSpace(payeeName)
                                                         || string.IsNullOrWhiteSpace(accountNumber))
            {
                skippedCount++;
                issues.Add($"Row {sheetRow}: ChequeNumber, PayeeName, and AccountNumber are required.");
                continue;
            }

            var payee = payees.FirstOrDefault(p => p.Name.Equals(payeeName, StringComparison.OrdinalIgnoreCase));
            if (payee is null)
            {
                skippedCount++;
                issues.Add($"Row {sheetRow}: Payee \"{payeeName}\" not found.");
                continue;
            }

            var account = accounts.FirstOrDefault(a =>
                a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
            if (account is null)
            {
                skippedCount++;
                issues.Add($"Row {sheetRow}: Bank account \"{accountNumber}\" not found.");
                continue;
            }

            var status = Enum.TryParse<Domain.Enums.ChequeStatus>(statusRaw, true, out var parsed)
                ? parsed
                : Domain.Enums.ChequeStatus.Issued;

            var request = new CreateChequeRequest(
                chequeNumber,
                date,
                payee.Id,
                amount,
                account.Id,
                status,
                string.IsNullOrWhiteSpace(remarks) ? null : remarks);

            try
            {
                await chequeService.CreateAsync(request, cancellationToken);
                importedCount++;
            }
            catch (ValidationException vex)
            {
                skippedCount++;
                issues.Add($"Row {sheetRow}: {string.Join(" ", vex.Errors.Select(e => e.ErrorMessage))}");
            }
        }

        return new ChequeImportResult(importedCount, skippedCount, issues);
    }

    public byte[] GetImportTemplateBytes()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Cheques");

        sheet.Cell(1, 1).Value = "ChequeNumber";
        sheet.Cell(1, 2).Value = "Date";
        sheet.Cell(1, 3).Value = "PayeeName";
        sheet.Cell(1, 4).Value = "Amount";
        sheet.Cell(1, 5).Value = "AccountNumber";
        sheet.Cell(1, 6).Value = "Status";
        sheet.Cell(1, 7).Value = "Remarks";

        sheet.Cell(2, 1).Value = "CHQ-00001";
        sheet.Cell(2, 2).Value = new DateTime(2026, 1, 15);
        sheet.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd";
        sheet.Cell(2, 3).Value = "Exact payee name from Payees";
        sheet.Cell(2, 4).Value = 1250.50;
        sheet.Cell(2, 5).Value = "Bank account number";
        sheet.Cell(2, 6).Value = "Issued";
        sheet.Cell(2, 7).Value = "Optional";

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static bool TryReadDateOnly(IXLCell cell, out DateOnly date)
    {
        date = default;

        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var serial))
        {
            try
            {
                date = DateOnly.FromDateTime(DateTime.FromOADate(serial));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        var text = cell.GetString().Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
    }
}
