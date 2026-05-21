using ChequePrintingSystem.Domain.Enums;

namespace ChequePrintingSystem.Domain.Entities;

public class Cheque : BaseAuditableEntity
{
    public string ChequeNumber { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public Guid PayeeId { get; set; }
    public Payee? Payee { get; set; }
    public decimal Amount { get; set; }
    public string AmountInWords { get; set; } = string.Empty;
    public Guid BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ChequeStatus Status { get; set; } = ChequeStatus.Issued;
    public DateOnly? ClearedOn { get; set; }
    public string? Remarks { get; set; }
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
