using System;
using ChequePrintingSystem.Application;
using ChequePrintingSystem.Infrastructure;
using ChequePrintingSystem.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Auth/Login";
    opt.AccessDeniedPath = "/Auth/AccessDenied";
});

var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var hasHttpsEndpoint = configuredUrls?
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) == true;

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
if (hasHttpsEndpoint)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var um = scope.ServiceProvider.GetRequiredService<UserManager<ChequePrintingSystem.Domain.Entities.ApplicationUser>>();
    var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbSeeder.SeedAsync(db, um, rm);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
