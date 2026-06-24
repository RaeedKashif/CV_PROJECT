using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Domain.Entities;
using ChequePrintingSystem.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChequePrintingSystem.Presentation.Controllers;

[Authorize]
public class BanksController(IUnitOfWork unitOfWork) : Controller
{
    public async Task<IActionResult> Index([FromQuery] string? q, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = q?.Trim();
        var qLower = !string.IsNullOrWhiteSpace(query) ? query.ToLowerInvariant() : null;

        System.Linq.Expressions.Expression<Func<Bank, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(qLower))
        {
            predicate = b =>
                (b.Name != null && b.Name.ToLower().Contains(qLower))
                || (b.SwiftCode != null && b.SwiftCode.ToLower().Contains(qLower));
        }

        var totalCount = await unitOfWork.Banks.CountAsync(predicate, cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var rows = await unitOfWork.Banks.ListPagedAsync(
            predicate,
            bs => bs.OrderBy(x => x.Name),
            page,
            pageSize,
            cancellationToken);

        return View(new PagedListViewModel<Bank>
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
    public IActionResult Create() => View(new BankFormViewModel());

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPost]
    public async Task<IActionResult> Create(BankFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await unitOfWork.Banks.AddAsync(new Bank { Name = model.Name.Trim(), SwiftCode = model.SwiftCode?.Trim() }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var bank = await unitOfWork.Banks.GetByIdAsync(id, cancellationToken);
        if (bank is null) return NotFound();

        return View(new BankFormViewModel
        {
            Id = bank.Id,
            Name = bank.Name,
            SwiftCode = bank.SwiftCode
        });
    }

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPost]
    public async Task<IActionResult> Edit(BankFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var bank = await unitOfWork.Banks.GetByIdAsync(model.Id.Value, cancellationToken);
        if (bank is null) return NotFound();

        bank.Name = model.Name.Trim();
        bank.SwiftCode = model.SwiftCode?.Trim();
        unitOfWork.Banks.Update(bank);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var bank = await unitOfWork.Banks.GetByIdAsync(id, cancellationToken);
        if (bank is null) return NotFound();

        unitOfWork.Banks.Delete(bank);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
