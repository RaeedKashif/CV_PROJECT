using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Domain.Entities;
using ChequePrintingSystem.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChequePrintingSystem.Presentation.Controllers;

[Authorize]
public class BankAccountsController(IUnitOfWork unitOfWork) : Controller
{
    public async Task<IActionResult> Index([FromQuery] string? q, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = q?.Trim();
        var qLower = !string.IsNullOrWhiteSpace(query) ? query.ToLowerInvariant() : null;

        var banks = await unitOfWork.Banks.ListAsync(cancellationToken: cancellationToken);
        ViewBag.BankLookup = banks.ToDictionary(x => x.Id, x => x.Name);

        System.Linq.Expressions.Expression<Func<BankAccount, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(qLower))
        {
            predicate = a => a.AccountNumber.ToLower().Contains(qLower);
        }

        var totalCount = await unitOfWork.BankAccounts.CountAsync(predicate, cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var rows = await unitOfWork.BankAccounts.ListPagedAsync(
            predicate,
            qx => qx.OrderBy(x => x.AccountNumber),
            page,
            pageSize,
            cancellationToken);

        return View(new PagedListViewModel<BankAccount>
        {
            Query = query,
            Rows = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateBanks(cancellationToken);
        return View(new BankAccountFormViewModel());
    }

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPost]
    public async Task<IActionResult> Create(BankAccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateBanks(cancellationToken);
            return View(model);
        }

        await unitOfWork.BankAccounts.AddAsync(new BankAccount
        {
            BankId = model.BankId,
            AccountNumber = model.AccountNumber.Trim(),
            OpeningBalance = model.OpeningBalance,
            DateX = model.DateX,
            DateY = model.DateY,
            PayeeX = model.PayeeX,
            PayeeY = model.PayeeY,
            AmountNumericX = model.AmountNumericX,
            AmountNumericY = model.AmountNumericY,
            AmountWordsX = model.AmountWordsX,
            AmountWordsY = model.AmountWordsY,
            TemplateCss = NormalizeTemplateCss(model.TemplateCss)
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.BankAccounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return NotFound();
        await PopulateBanks(cancellationToken);

        return View(new BankAccountFormViewModel
        {
            Id = account.Id,
            BankId = account.BankId,
            AccountNumber = account.AccountNumber,
            OpeningBalance = account.OpeningBalance,
            DateX = account.DateX,
            DateY = account.DateY,
            PayeeX = account.PayeeX,
            PayeeY = account.PayeeY,
            AmountNumericX = account.AmountNumericX,
            AmountNumericY = account.AmountNumericY,
            AmountWordsX = account.AmountWordsX,
            AmountWordsY = account.AmountWordsY,
            TemplateCss = account.TemplateCss
        });
    }

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPost]
    public async Task<IActionResult> Edit(BankAccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            await PopulateBanks(cancellationToken);
            return View(model);
        }

        var account = await unitOfWork.BankAccounts.GetByIdAsync(model.Id.Value, cancellationToken);
        if (account is null) return NotFound();

        account.BankId = model.BankId;
        account.AccountNumber = model.AccountNumber.Trim();
        account.OpeningBalance = model.OpeningBalance;
        account.DateX = model.DateX;
        account.DateY = model.DateY;
        account.PayeeX = model.PayeeX;
        account.PayeeY = model.PayeeY;
        account.AmountNumericX = model.AmountNumericX;
        account.AmountNumericY = model.AmountNumericY;
        account.AmountWordsX = model.AmountWordsX;
        account.AmountWordsY = model.AmountWordsY;
        account.TemplateCss = NormalizeTemplateCss(model.TemplateCss);
        unitOfWork.BankAccounts.Update(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.BankAccounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return NotFound();
        unitOfWork.BankAccounts.Delete(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateBanks(CancellationToken cancellationToken)
    {
        ViewBag.Banks = (await unitOfWork.Banks.ListAsync(cancellationToken: cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()));
    }

    private static string? NormalizeTemplateCss(string? templateCss) =>
        string.IsNullOrWhiteSpace(templateCss) ? null : templateCss.Trim();
}
