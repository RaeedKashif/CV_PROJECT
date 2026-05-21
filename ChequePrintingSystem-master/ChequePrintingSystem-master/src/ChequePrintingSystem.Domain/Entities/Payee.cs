namespace ChequePrintingSystem.Domain.Entities;

public class Payee : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public ICollection<Cheque> Cheques { get; set; } = new List<Cheque>();
}
