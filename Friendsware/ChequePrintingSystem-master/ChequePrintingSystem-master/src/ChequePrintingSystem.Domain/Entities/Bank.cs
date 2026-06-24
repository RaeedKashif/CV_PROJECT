namespace ChequePrintingSystem.Domain.Entities;

public class Bank : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? SwiftCode { get; set; }
    public ICollection<BankAccount> Accounts { get; set; } = new List<BankAccount>();
}
