# Bug Fixes

This document records the fixes applied after the issues listed in `BUGS.md`.

## Summary

The following bug categories were addressed:

- soft-delete visibility and uniqueness issues
- stored XSS risk in cheque print template CSS
- attachment uploads using the wrong physical path base
- amount-in-words formatting inconsistency
- noisy HTTPS redirection behavior during HTTP-only local runs

## Fixes Applied

### 1. Soft-deleted records are now excluded consistently

What changed:

- added a global query filter for all `BaseAuditableEntity` types so `IsDeleted = true` rows are hidden automatically
- changed repository `GetByIdAsync` to use a query-filter-aware lookup instead of `FindAsync`
- changed unique indexes for soft-deletable entities to filtered unique indexes so deleted rows no longer block reuse of values

Files:

- [ApplicationDbContext.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/ApplicationDbContext.cs:22)
- [ApplicationDbContext.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/ApplicationDbContext.cs:24)
- [ApplicationDbContext.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/ApplicationDbContext.cs:41)
- [Repository.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/Repository.cs:11)

Database migration added:

- [20260520102701_AddFilteredUniqueIndexesForSoftDelete.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/Migrations/20260520102701_AddFilteredUniqueIndexesForSoftDelete.cs:1)

### 2. Cheque template CSS is no longer rendered as raw HTML

What changed:

- removed `Html.Raw` from the print view
- added validation to reject angle brackets in `TemplateCss`
- normalized saved CSS values before persisting them
- added field-level validation output in the bank account form

Files:

- [Print.cshtml](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Views/Cheques/Print.cshtml:24)
- [ViewModels.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Models/ViewModels.cs:65)
- [BankAccountsController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/BankAccountsController.cs:72)
- [BankAccounts/_Form.cshtml](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Views/BankAccounts/_Form.cshtml:29)

### 3. Attachment uploads now use the actual web root

What changed:

- injected `IWebHostEnvironment` into `ChequesController`
- changed upload path resolution from a relative `wwwroot/uploads` path to `WebRootPath/uploads`

Files:

- [ChequesController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/ChequesController.cs:16)
- [ChequesController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/ChequesController.cs:92)

### 4. Amount-in-words format is now standardized

What changed:

- standardized fractional formatting to banking-style `NN/100`
- normalized decimal rounding before conversion
- changed zero-value output to use the same format family

Example:

- before: `One Hundred Twenty Five Rupees and Seventy Five Paisa Only`
- after: `One Hundred Twenty Five Rupees and 75/100 Only`

Files:

- [AmountInWordsConverter.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Application/Services/AmountInWordsConverter.cs:19)

### 5. HTTP-only runs no longer emit the HTTPS-port warning

What changed:

- added startup logic to enable `UseHttpsRedirection()` only when an HTTPS URL is actually configured

Files:

- [Program.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Program.cs:25)
- [Program.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Program.cs:39)

## Verification Performed

### Build

Command:

```powershell
dotnet build ChequePrintingSystem.sln
```

Result:

- succeeded
- `0` warnings
- `0` errors

### Tests

Command:

```powershell
dotnet test ChequePrintingSystem.sln --no-build
```

Result:

- `2` passed
- `0` failed
- `0` skipped

### Database Update

Command:

```powershell
dotnet ef database update --project src\ChequePrintingSystem.Infrastructure --startup-project src\ChequePrintingSystem.Presentation
```

Result:

- succeeded

### Runtime Smoke Test

Command:

```powershell
dotnet run --project src\ChequePrintingSystem.Presentation --no-build --urls http://localhost:5051
```

Result:

- app started successfully
- `GET /` returned HTTP `200`
- the previous HTTP-only HTTPS-port warning did not reappear during the smoke run

## Remaining Notes

- The fixes above address the targeted issues from `BUGS.md`.
- This does not guarantee the system is free of every possible defect; it confirms the reviewed issues were fixed and re-verified.
