using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Domain.Entities;
using ChequePrintingSystem.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChequePrintingSystem.Presentation.Controllers;

[Authorize]
public class PayeesController(IUnitOfWork unitOfWork) : Controller
{
    public async Task<IActionResult> Index([FromQuery] string? q, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = q?.Trim();
        var qLower = !string.IsNullOrWhiteSpace(query) ? query.ToLowerInvariant() : null;

        System.Linq.Expressions.Expression<Func<Payee, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(qLower))
        {
            predicate = p => p.Name.ToLower().Contains(qLower)
                             || (p.Email != null && p.Email.ToLower().Contains(qLower))
                             || (p.Phone != null && p.Phone.ToLower().Contains(qLower));
        }

        var totalCount = await unitOfWork.Payees.CountAsync(predicate, cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var rows = await unitOfWork.Payees.ListPagedAsync(
            predicate,
            ps => ps.OrderBy(x => x.Name),
            page,
            pageSize,
            cancellationToken);

        return View(new PagedListViewModel<Payee>
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
    public IActionResult Create() => View(new PayeeFormViewModel());

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPost]
    public async Task<IActionResult> Create(PayeeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await unitOfWork.Payees.AddAsync(new Payee
        {
            Name = model.Name.Trim(),
            Email = model.Email?.Trim(),
            Phone = model.Phone?.Trim()
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var payee = await unitOfWork.Payees.GetByIdAsync(id, cancellationToken);
        if (payee is null) return NotFound();

        return View(new PayeeFormViewModel
        {
            Id = payee.Id,
            Name = payee.Name,
            Email = payee.Email,
            Phone = payee.Phone
        });
    }

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPost]
    public async Task<IActionResult> Edit(PayeeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var payee = await unitOfWork.Payees.GetByIdAsync(model.Id.Value, cancellationToken);
        if (payee is null) return NotFound();

        payee.Name = model.Name.Trim();
        payee.Email = model.Email?.Trim();
        payee.Phone = model.Phone?.Trim();
        unitOfWork.Payees.Update(payee);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var payee = await unitOfWork.Payees.GetByIdAsync(id, cancellationToken);
        if (payee is null) return NotFound();

        unitOfWork.Payees.Delete(payee);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
