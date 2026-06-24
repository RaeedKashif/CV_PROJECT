namespace ChequePrintingSystem.Domain.Entities;

public class Attachment : BaseAuditableEntity
{
    public Guid ChequeId { get; set; }
    public Cheque? Cheque { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}
