namespace ChequePrintingSystem.Domain.Entities;

public class BankAccount : BaseAuditableEntity
{
    public Guid BankId { get; set; }
    public Bank? Bank { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal DateX { get; set; }
    public decimal DateY { get; set; }
    public decimal PayeeX { get; set; }
    public decimal PayeeY { get; set; }
    public decimal AmountNumericX { get; set; }
    public decimal AmountNumericY { get; set; }
    public decimal AmountWordsX { get; set; }
    public decimal AmountWordsY { get; set; }
    public string? TemplateCss { get; set; }
    public ICollection<Cheque> Cheques { get; set; } = new List<Cheque>();
}
