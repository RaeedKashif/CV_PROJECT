using ChequePrintingSystem.Domain.Enums;

namespace ChequePrintingSystem.Application.DTOs;

public record ChequeDto(
    Guid Id,
    string ChequeNumber,
    DateOnly Date,
    string PayeeName,
    decimal Amount,
    string AmountInWords,
    string BankAccountNumber,
    ChequeStatus Status,
    string? Remarks = null);

public sealed record ChequeImportResult(int ImportedCount, int SkippedCount, IReadOnlyList<string> Issues);

public record CreateChequeRequest(
    string ChequeNumber,
    DateOnly Date,
    Guid PayeeId,
    decimal Amount,
    Guid BankAccountId,
    ChequeStatus Status,
    string? Remarks
);

public record DashboardDto(
    int TotalCheques,
    decimal TotalClearedAmount,
    int PendingCheques,
    int UpcomingPdcCount,
    IReadOnlyDictionary<string, decimal> BankBalances
);

public class ReportFilterDto
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? ChequeNumber { get; set; }
    public string? PayeeName { get; set; }
    public string? BankAccountNumber { get; set; }
    public ChequeStatus? Status { get; set; }
}
