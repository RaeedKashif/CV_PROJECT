using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Domain.Entities;

namespace ChequePrintingSystem.Infrastructure.Persistence;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private IRepository<Bank>? _banks;
    private IRepository<BankAccount>? _accounts;
    private IRepository<Payee>? _payees;
    private IRepository<Cheque>? _cheques;
    private IRepository<Attachment>? _attachments;
    private IRepository<AuditLog>? _auditLogs;

    public IRepository<Bank> Banks => _banks ??= new Repository<Bank>(dbContext);
    public IRepository<BankAccount> BankAccounts => _accounts ??= new Repository<BankAccount>(dbContext);
    public IRepository<Payee> Payees => _payees ??= new Repository<Payee>(dbContext);
    public IRepository<Cheque> Cheques => _cheques ??= new Repository<Cheque>(dbContext);
    public IRepository<Attachment> Attachments => _attachments ??= new Repository<Attachment>(dbContext);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
