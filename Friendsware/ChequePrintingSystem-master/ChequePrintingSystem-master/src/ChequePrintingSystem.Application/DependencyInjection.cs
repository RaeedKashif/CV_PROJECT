using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChequePrintingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IChequeService, ChequeService>();
        services.AddScoped<IChequeImportService, ChequeImportService>();
        services.AddSingleton<IReportExportService, ReportExportService>();
        services.AddSingleton<IAmountInWordsConverter, AmountInWordsConverter>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
