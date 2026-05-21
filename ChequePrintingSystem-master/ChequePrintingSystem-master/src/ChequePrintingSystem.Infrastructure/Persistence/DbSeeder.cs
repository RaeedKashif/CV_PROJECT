using ChequePrintingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChequePrintingSystem.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        await dbContext.Database.MigrateAsync();

        foreach (var role in new[] { "Admin", "Accountant", "Viewer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (await userManager.FindByEmailAsync("admin@cheque.local") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@cheque.local",
                Email = "admin@cheque.local",
                FullName = "System Admin",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (await userManager.FindByEmailAsync("accountant@cheque.local") is null)
        {
            var accountant = new ApplicationUser
            {
                UserName = "accountant@cheque.local",
                Email = "accountant@cheque.local",
                FullName = "Default Accountant",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(accountant, "Accountant@123");
            await userManager.AddToRoleAsync(accountant, "Accountant");
        }

        if (await userManager.FindByEmailAsync("viewer@cheque.local") is null)
        {
            var viewer = new ApplicationUser
            {
                UserName = "viewer@cheque.local",
                Email = "viewer@cheque.local",
                FullName = "Default Viewer",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(viewer, "Viewer@123");
            await userManager.AddToRoleAsync(viewer, "Viewer");
        }

        if (!await dbContext.Banks.AnyAsync())
        {
            var bank = new Bank { Name = "Demo Bank", SwiftCode = "DEMO001" };
            var account = new BankAccount
            {
                Bank = bank,
                AccountNumber = "001-000001-00",
                OpeningBalance = 100000,
                DateX = 148,
                DateY = 13,
                PayeeX = 23,
                PayeeY = 31,
                AmountNumericX = 151,
                AmountNumericY = 31,
                AmountWordsX = 23,
                AmountWordsY = 39,
                TemplateCss = ".cheque-canvas{width:190mm;height:85mm;position:relative;}"
            };
            var payee = new Payee { Name = "Demo Supplier", Email = "supplier@demo.local" };
            dbContext.Banks.Add(bank);
            dbContext.BankAccounts.Add(account);
            dbContext.Payees.Add(payee);
            await dbContext.SaveChangesAsync();
        }

        var firstAccount = await dbContext.BankAccounts.OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync();
        if (firstAccount is not null && firstAccount.DateX <= 25 && firstAccount.PayeeX <= 25)
        {
            firstAccount.DateX = 148;
            firstAccount.DateY = 13;
            firstAccount.PayeeX = 23;
            firstAccount.PayeeY = 31;
            firstAccount.AmountNumericX = 151;
            firstAccount.AmountNumericY = 31;
            firstAccount.AmountWordsX = 23;
            firstAccount.AmountWordsY = 39;
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Cheques.AnyAsync())
        {
            var payee = await dbContext.Payees.OrderBy(x => x.Name).FirstAsync();
            var account = await dbContext.BankAccounts.OrderBy(x => x.AccountNumber).FirstAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            dbContext.Cheques.AddRange(
                new Cheque
                {
                    ChequeNumber = "CHQ-1001",
                    Date = today,
                    PayeeId = payee.Id,
                    Amount = 2500m,
                    AmountInWords = "Two Thousand Five Hundred and 00/100 Only",
                    BankAccountId = account.Id,
                    Status = Domain.Enums.ChequeStatus.Issued
                },
                new Cheque
                {
                    ChequeNumber = "CHQ-1002",
                    Date = today.AddDays(-2),
                    PayeeId = payee.Id,
                    Amount = 7800m,
                    AmountInWords = "Seven Thousand Eight Hundred and 00/100 Only",
                    BankAccountId = account.Id,
                    Status = Domain.Enums.ChequeStatus.Cleared,
                    ClearedOn = today.AddDays(-1)
                },
                new Cheque
                {
                    ChequeNumber = "CHQ-1003",
                    Date = today.AddDays(10),
                    PayeeId = payee.Id,
                    Amount = 9900m,
                    AmountInWords = "Nine Thousand Nine Hundred and 00/100 Only",
                    BankAccountId = account.Id,
                    Status = Domain.Enums.ChequeStatus.PostDated
                }
            );
            await dbContext.SaveChangesAsync();
        }
    }
}
