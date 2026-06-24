using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Application.DTOs;
using ChequePrintingSystem.Application.Services;
using ChequePrintingSystem.Domain.Entities;
using ChequePrintingSystem.Domain.Enums;
using ChequePrintingSystem.Presentation.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChequePrintingSystem.Presentation.Controllers;

[Authorize]
public class ChequesController(
    IChequeService chequeService,
    IChequeImportService chequeImportService,
    IUnitOfWork unitOfWork,
    IWebHostEnvironment webHostEnvironment) : Controller
{
    public async Task<IActionResult> Index([FromQuery] ChequeIndexViewModel filter, CancellationToken cancellationToken)
    {
        var criteria = new ReportFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            PayeeName = filter.PayeeName,
            BankAccountNumber = filter.BankAccountNumber,
            ChequeNumber = filter.ChequeNumber,
            Status = filter.Status
        };

        var page = await chequeService.GetFilteredPagedAsync(criteria, filter.Page, filter.PageSize, cancellationToken);
        filter.Rows = page.Items;
        filter.Page = page.Page;
        filter.PageSize = page.PageSize;
        filter.TotalCount = page.TotalCount;
        filter.TotalPages = page.TotalPages;

        return View(filter);
    }

    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateDropdowns(cancellationToken, restrictStatusesForNewCheque: true);
        return View(new ChequeCreateViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Create(
        ChequeCreateViewModel model,
        IFormFile? attachment,
        string submitAction,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(cancellationToken, restrictStatusesForNewCheque: true);
            return View(model);
        }

        var request = new CreateChequeRequest(
            model.ChequeNumber.Trim(),
            model.Date,
            model.PayeeId,
            model.Amount,
            model.BankAccountId,
            model.Status,
            string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim());

        Guid id;
        try
        {
            id = await chequeService.CreateAsync(request, cancellationToken);
        }
        catch (ValidationException vx)
        {
            foreach (var err in vx.Errors)
            {
                ModelState.AddModelError(
                    string.IsNullOrEmpty(err.PropertyName) ? string.Empty : err.PropertyName,
                    err.ErrorMessage);
            }

            await PopulateDropdowns(cancellationToken, restrictStatusesForNewCheque: true);
            return View(model);
        }

        if (attachment is { Length: > 0 })
        {
            var uploadsPath = Path.Combine(webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);
            var fileName = $"{id}_{Path.GetFileName(attachment.FileName)}";
            var fullPath = Path.Combine(uploadsPath, fileName);
            await using var stream = System.IO.File.Create(fullPath);
            await attachment.CopyToAsync(stream, cancellationToken);
            await unitOfWork.Attachments.AddAsync(new Attachment
            {
                ChequeId = id,
                FileName = attachment.FileName,
                Path = $"/uploads/{fileName}",
                ContentType = attachment.ContentType
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return submitAction == "print"
            ? RedirectToAction(nameof(Print), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Cancel(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await chequeService.CancelAsync(id, reason, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Message"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Print(Guid id, CancellationToken cancellationToken)
    {
        var cheque = await chequeService.GetEntityAsync(id, cancellationToken);
        if (cheque is null) return NotFound();

        var payee = await unitOfWork.Payees.GetByIdAsync(cheque.PayeeId, cancellationToken);
        var account = await unitOfWork.BankAccounts.GetByIdAsync(cheque.BankAccountId, cancellationToken);
        if (account is null) return NotFound();

        var vm = new ChequePrintViewModel
        {
            ChequeId = cheque.Id,
            ChequeNumber = cheque.ChequeNumber,
            Date = cheque.Date,
            FormattedDate = FormatDateForBoxes(cheque.Date),
            PayeeName = payee?.Name ?? "N/A",
            Amount = cheque.Amount,
            AmountInWords = cheque.AmountInWords,
            DateX = account.DateX,
            DateY = account.DateY,
            PayeeX = account.PayeeX,
            PayeeY = account.PayeeY,
            AmountNumericX = account.AmountNumericX,
            AmountNumericY = account.AmountNumericY,
            AmountWordsX = account.AmountWordsX,
            AmountWordsY = account.AmountWordsY,
            TemplateCss = account.TemplateCss ?? string.Empty
        };

        return View(vm);
    }

    private static string FormatDateForBoxes(DateOnly date)
    {
        return date.ToString("ddMMyyyy");
    }

    [Authorize(Roles = "Admin,Accountant")]
    public IActionResult DownloadImportTemplate()
    {
        var bytes = chequeImportService.GetImportTemplateBytes();
        const string fileName = "cheque-import-template.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Message"] = "Please select a valid Excel file.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await chequeImportService.ImportFromExcelAsync(stream, cancellationToken);
            var msg =
                $"Imported {result.ImportedCount} cheques from {file.FileName}; skipped {result.SkippedCount} row(s).";
            if (result.Issues.Count > 0)
            {
                var shown = result.Issues.Take(20).ToList();
                msg += " Details: " + string.Join("; ", shown);
                if (result.Issues.Count > shown.Count)
                {
                    msg += $" (+{result.Issues.Count - shown.Count} more)";
                }
            }

            TempData["Message"] = msg;
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"Import failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns(CancellationToken cancellationToken, bool restrictStatusesForNewCheque = false)
    {
        ViewBag.Payees = (await unitOfWork.Payees.ListAsync(cancellationToken: cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()));
        ViewBag.Accounts = (await unitOfWork.BankAccounts.ListAsync(cancellationToken: cancellationToken))
            .Select(x => new SelectListItem(x.AccountNumber, x.Id.ToString()));
        IEnumerable<ChequeStatus> statuses = Enum.GetValues<ChequeStatus>();
        if (restrictStatusesForNewCheque)
        {
            statuses = statuses.Where(s => s is ChequeStatus.Issued or ChequeStatus.PostDated);
        }

        ViewBag.Statuses = statuses.Select(x => new SelectListItem(x.ToString(), x.ToString()));
    }
}
