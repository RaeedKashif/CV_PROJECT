using System.Linq.Expressions;
using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChequePrintingSystem.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Payee> Payees => Set<Payee>();
    public DbSet<Cheque> Cheques => Set<Cheque>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ApplySoftDeleteQueryFilters(builder);

        builder.Entity<Bank>().HasIndex(x => x.Name).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Entity<BankAccount>().HasIndex(x => x.AccountNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Entity<BankAccount>().Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Entity<BankAccount>().Property(x => x.DateX).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.DateY).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.PayeeX).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.PayeeY).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.AmountNumericX).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.AmountNumericY).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.AmountWordsX).HasPrecision(9, 2);
        builder.Entity<BankAccount>().Property(x => x.AmountWordsY).HasPrecision(9, 2);
        builder.Entity<Cheque>().HasIndex(x => x.ChequeNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Entity<Cheque>().HasIndex(x => new { x.Date, x.Status });
        builder.Entity<Cheque>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<Payee>().HasIndex(x => x.Name);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeletedProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(nameof(BaseAuditableEntity.IsDeleted)));
            var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(compareExpression, parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var actor = currentUserService.UserName;
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                entry.Entity.CreatedBy = actor;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                entry.Entity.UpdatedBy = actor;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = DateTime.UtcNow;
                entry.Entity.DeletedBy = actor;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
