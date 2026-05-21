namespace ChequePrintingSystem.Domain.Entities;

public class AuditLog : BaseAuditableEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
