using ChequePrintingSystem.Domain.Entities;

namespace ChequePrintingSystem.Application.Abstractions;

public interface IUnitOfWork
{
    IRepository<Bank> Banks { get; }
    IRepository<BankAccount> BankAccounts { get; }
    IRepository<Payee> Payees { get; }
    IRepository<Cheque> Cheques { get; }
    IRepository<Attachment> Attachments { get; }
    IRepository<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
