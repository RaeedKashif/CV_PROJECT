using ChequePrintingSystem.Application.Services;
using ChequePrintingSystem.Presentation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChequePrintingSystem.Presentation.Controllers;

[Authorize]
public class DashboardController(IChequeService chequeService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = new DashboardViewModel
        {
            Data = await chequeService.BuildDashboardAsync(cancellationToken)
        };
        return View(vm);
    }
}
