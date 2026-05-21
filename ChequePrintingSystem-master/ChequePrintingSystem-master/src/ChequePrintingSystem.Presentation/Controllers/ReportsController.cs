using ChequePrintingSystem.Application.Services;
using ChequePrintingSystem.Application.DTOs;
using ChequePrintingSystem.Domain.Enums;
using ChequePrintingSystem.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChequePrintingSystem.Presentation.Controllers;

[Authorize]
public class ReportsController(IChequeService chequeService, IReportExportService reportExportService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] ReportFilterViewModel filter, CancellationToken cancellationToken)
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

        var counts = await chequeService.GetFilteredCountsAsync(criteria, cancellationToken);
        ViewBag.CancelledCount = counts.CancelledCount;
        ViewBag.PendingCount = counts.PendingCount;
        ViewBag.ClearedCount = counts.ClearedCount;

        var page = await chequeService.GetFilteredPagedAsync(criteria, filter.Page, filter.PageSize, cancellationToken);
        filter.Rows = page.Items;
        filter.Page = page.Page;
        filter.PageSize = page.PageSize;
        filter.TotalCount = page.TotalCount;
        filter.TotalPages = page.TotalPages;

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] ReportFilterViewModel filter, CancellationToken cancellationToken)
    {
        var data = await chequeService.GetFilteredAsync(new ReportFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            PayeeName = filter.PayeeName,
            BankAccountNumber = filter.BankAccountNumber,
            ChequeNumber = filter.ChequeNumber,
            Status = filter.Status
        }, cancellationToken);

        var bytes = reportExportService.ExportExcel(data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cheque-report.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] ReportFilterViewModel filter, CancellationToken cancellationToken)
    {
        var data = await chequeService.GetFilteredAsync(new ReportFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            PayeeName = filter.PayeeName,
            BankAccountNumber = filter.BankAccountNumber,
            ChequeNumber = filter.ChequeNumber,
            Status = filter.Status
        }, cancellationToken);

        var bytes = reportExportService.ExportPdf(data, "Cheque Report");
        return File(bytes, "application/pdf", "cheque-report.pdf");
    }
}
