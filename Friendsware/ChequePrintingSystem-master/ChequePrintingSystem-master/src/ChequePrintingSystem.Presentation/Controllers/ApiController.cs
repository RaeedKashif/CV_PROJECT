using ChequePrintingSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChequePrintingSystem.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiController(IChequeService chequeService) : ControllerBase
{
    [HttpGet("cheques")]
    public async Task<IActionResult> GetCheques(CancellationToken cancellationToken) =>
        Ok(await chequeService.GetAllAsync(cancellationToken));

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken) =>
        Ok(await chequeService.BuildDashboardAsync(cancellationToken));
}
