using ChequePrintingSystem.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ChequePrintingSystem.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string UserId => accessor.HttpContext?.User?.FindFirst("sub")?.Value ?? "system";
    public string UserName => accessor.HttpContext?.User?.Identity?.Name ?? "system";
}
