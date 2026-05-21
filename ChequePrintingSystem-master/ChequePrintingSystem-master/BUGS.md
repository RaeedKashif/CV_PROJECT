# Bug Report

This document captures the issues I found from a best-effort verification pass across the current codebase.

## Scope of Verification

The review included:

- `dotnet test ChequePrintingSystem.sln`
- a runtime smoke check by starting the MVC app on a separate localhost port
- targeted code review of the main cheque, reporting, authentication, persistence, and print flows

## Automated Results

### Test Suite

Command run:

```powershell
dotnet test ChequePrintingSystem.sln
```

Result:

- `1` failed
- `1` passed

Failing test:

- `ChequePrintingSystem.Application.Tests.AmountInWordsConverterTests.Convert_ShouldReturnFormattedWords`

### Runtime Smoke Check

The app started successfully on `http://localhost:5051` and returned HTTP `200` for `/`.

Observed warning when running HTTP-only:

- `Failed to determine the https port for redirect.`

## Findings

### 1. Soft-deleted records are still returned by repositories

Severity: `High`

Why this is a bug:

The DbContext converts deletes into soft deletes by setting `IsDeleted = true`, but the generic repository never filters deleted rows out of `GetByIdAsync`, `ListAsync`, `CountAsync`, or `ListPagedAsync`. That means deleted banks, payees, bank accounts, attachments, cheques, and audit-backed entities can still appear in listings and be retrieved as if they were active.

Evidence:

- [ApplicationDbContext.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/ApplicationDbContext.cs:53)
- [ApplicationDbContext.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/ApplicationDbContext.cs:56)
- [Repository.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/Repository.cs:12)
- [Repository.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/Repository.cs:16)
- [Repository.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/Repository.cs:27)
- [Repository.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Infrastructure/Persistence/Repository.cs:46)

Impact:

- deleted records can still show up in UI lists
- deleted records can still be counted in pagination and summaries
- deleted records can still be fetched by ID
- unique indexes remain occupied by deleted rows, so users may be unable to recreate a deleted bank name, account number, or cheque number

Recommended fix:

- add a global query filter for `BaseAuditableEntity.IsDeleted == false`
- or make repositories exclude soft-deleted rows consistently
- review unique-index behavior if soft delete is intended to support recreation

### 2. Stored XSS/script injection via printable cheque template CSS

Severity: `High`

Why this is a bug:

`TemplateCss` is user-editable in the bank account form and is rendered with `Html.Raw` inside the print view. A malicious user with `Admin` or `Accountant` access can inject `</style><script>...</script>` or other markup-breaking payloads that execute in the browser when a cheque is printed or previewed.

Evidence:

- [BankAccounts/_Form.cshtml](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Views/BankAccounts/_Form.cshtml:29)
- [BankAccounts/_Form.cshtml](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Views/BankAccounts/_Form.cshtml:30)
- [BankAccountsController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/BankAccountsController.cs:80)
- [BankAccountsController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/BankAccountsController.cs:135)
- [Print.cshtml](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Views/Cheques/Print.cshtml:24)
- [Print.cshtml](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Views/Cheques/Print.cshtml:26)

Impact:

- authenticated stored XSS
- malicious scripts can run in the context of the application
- theft of session data or forced actions becomes possible
- printing pages are especially exposed because they render this content directly

Recommended fix:

- do not render raw user-provided CSS/HTML
- sanitize and validate template input against a strict CSS-only allowlist
- consider storing structured layout values instead of arbitrary CSS text

### 3. Attachment uploads depend on the process working directory instead of the app web root

Severity: `Medium`

Why this is a bug:

Attachment files are written to `Path.Combine("wwwroot", "uploads")`, which is relative to the current process working directory. If the app is started from a different directory, run as a service, or published under a different content-root arrangement, uploads may be saved outside the actual web root and become inaccessible through `/uploads/...`.

Evidence:

- [ChequesController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/ChequesController.cs:87)
- [ChequesController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/ChequesController.cs:89)
- [ChequesController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/ChequesController.cs:92)
- [ChequesController.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Controllers/ChequesController.cs:99)

Impact:

- attachments may not be served by the application after upload
- deployments that do not use the repo root as the working directory can break file access
- behavior can differ between local runs and published runs

Recommended fix:

- inject `IWebHostEnvironment`
- use `environment.WebRootPath` to build the physical uploads directory
- consider validating file types and file size at the same time

### 4. Amount-in-words output is inconsistent with the tested and seeded cheque format

Severity: `Medium`

Why this is a bug:

The amount converter renders fractional values as words like `Seventy Five Paisa Only`, but the test suite expects `75/100 Only`, and seeded cheque data already uses the slash-based banking format such as `Two Thousand Five Hundred and 00/100 Only`. This mismatch means generated cheques are inconsistent with both automated expectations and existing sample data.

Evidence:

- [AmountInWordsConverter.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Application/Services/AmountInWordsConverter.cs:29)
- [AmountInWordsConverter.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Application/Services/AmountInWordsConverter.cs:34)
- [AmountInWordsConverterTests.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/tests/ChequePrintingSystem.Application.Tests/AmountInWordsConverterTests.cs:12)
- [AmountInWordsConverterTests.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/tests/ChequePrintingSystem.Application.Tests/AmountInWordsConverterTests.cs:13)

Impact:

- unit test failure
- inconsistent printed cheque wording
- potential rejection by users expecting a standard banking notation

Recommended fix:

- pick one format and standardize the system
- if banking-style formatting is desired, return values like `One Hundred Twenty Five Rupees and 75/100 Only`
- update seeded data and tests only after the format decision is explicit

### 5. HTTP-only launches trigger HTTPS redirection warnings

Severity: `Low`

Why this is a bug:

The application always enables `UseHttpsRedirection()`. When the app is launched on an HTTP-only URL, it logs `Failed to determine the https port for redirect.` This does not block startup, but it creates noisy diagnostics and can cause confusing behavior in environments where HTTPS is not configured.

Evidence:

- [Program.cs](C:/Users/Shekhani%20Laptops/ChequePrintingSystem-master/ChequePrintingSystem-master/src/ChequePrintingSystem.Presentation/Program.cs:33)
- runtime smoke check produced the warning while the app was bound to `http://localhost:5051`

Impact:

- noisy logs
- confusing local behavior during HTTP-only development runs
- potential redirect problems in environments without an HTTPS endpoint

Recommended fix:

- conditionally enable HTTPS redirection only when HTTPS is configured
- or ensure launch profiles and deployment config always provide an HTTPS endpoint

## Notes on Non-Product Failures

These came up during verification but should not be treated as application bugs:

- running `dotnet build` and `dotnet test` in parallel caused compiler/output locking
- building while the app process was still running caused `MSB3021/MSB3027` file-lock errors because the running web app held the output DLLs open

## Suggested Next Steps

1. Fix the soft-delete/query-filter issue first because it affects data correctness across the whole system.
2. Fix the stored-XSS risk in printable template CSS next because it is a security issue.
3. Standardize amount-in-words formatting and make the tests pass.
4. Move attachment storage to the real web root path.
5. Clean up HTTP/HTTPS startup behavior for more predictable local and deployment runs.
