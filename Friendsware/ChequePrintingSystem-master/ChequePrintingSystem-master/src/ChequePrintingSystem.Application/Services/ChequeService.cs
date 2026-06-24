using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Application.DTOs;
using ChequePrintingSystem.Domain.Entities;
using ChequePrintingSystem.Domain.Enums;
using FluentValidation;

namespace ChequePrintingSystem.Application.Services;

public interface IChequeService
{
    Task<IReadOnlyList<ChequeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChequeDto>> GetFilteredAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<PagedResult<ChequeDto>> GetFilteredPagedAsync(ReportFilterDto filter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ChequeStatusCountsDto> GetFilteredCountsAsync(ReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<Cheque?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreateChequeRequest request, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<DashboardDto> BuildDashboardAsync(CancellationToken cancellationToken = default);
}

public class ChequeService(
    IUnitOfWork unitOfWork,
    IAmountInWordsConverter converter,
    IValidator<CreateChequeRequest> createChequeValidator) : IChequeService
{
    public async Task<IReadOnlyList<ChequeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cheques = await unitOfWork.Cheques.ListAsync(cancellationToken: cancellationToken);
        var payees = await unitOfWork.Payees.ListAsync(cancellationToken: cancellationToken);
        var accounts = await unitOfWork.BankAccounts.ListAsync(cancellationToken: cancellationToken);

        return cheques
            .OrderByDescending(x => x.Date)
            .Select(c =>
                new ChequeDto(
                    c.Id,
                    c.ChequeNumber,
                    c.Date,
                    payees.FirstOrDefault(p => p.Id == c.PayeeId)?.Name ?? "N/A",
                    c.Amount,
                    c.AmountInWords,
                    accounts.FirstOrDefault(a => a.Id == c.BankAccountId)?.AccountNumber ?? "N/A",
                    c.Status,
                    c.Remarks))
            .ToList();
    }

    public async Task<Cheque?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default) =>
        await unitOfWork.Cheques.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ChequeDto>> GetFilteredAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var payeeName = filter.PayeeName?.Trim();
        var hasPayeeFilter = !string.IsNullOrWhiteSpace(payeeName);
        IReadOnlyList<Guid> payeeIds = Array.Empty<Guid>();
        if (hasPayeeFilter)
        {
            var payeeLower = payeeName!.ToLowerInvariant();
            payeeIds = (await unitOfWork.Payees.ListAsync(
                    p => p.Name.ToLower().Contains(payeeLower),
                    cancellationToken: cancellationToken))
                .Select(p => p.Id)
                .ToList();

            if (payeeIds.Count == 0) return Array.Empty<ChequeDto>();
        }

        var bankAccountNumber = filter.BankAccountNumber?.Trim();
        var hasAccountFilter = !string.IsNullOrWhiteSpace(bankAccountNumber);
        IReadOnlyList<Guid> accountIds = Array.Empty<Guid>();
        if (hasAccountFilter)
        {
            var accountLower = bankAccountNumber!.ToLowerInvariant();
            accountIds = (await unitOfWork.BankAccounts.ListAsync(
                    a => a.AccountNumber.ToLower().Contains(accountLower),
                    cancellationToken: cancellationToken))
                .Select(a => a.Id)
                .ToList();

            if (accountIds.Count == 0) return Array.Empty<ChequeDto>();
        }

        var chequeNumber = filter.ChequeNumber?.Trim();
        var chequeNumberLower = !string.IsNullOrWhiteSpace(chequeNumber) ? chequeNumber.ToLowerInvariant() : null;

        var fromDate = filter.FromDate;
        var toDate = filter.ToDate;
        var status = filter.Status;

        var cheques = await unitOfWork.Cheques.ListAsync(
            c => (!fromDate.HasValue || c.Date >= fromDate.Value)
                 && (!toDate.HasValue || c.Date <= toDate.Value)
                 && (chequeNumberLower == null || c.ChequeNumber.ToLower().Contains(chequeNumberLower))
                 && (!hasPayeeFilter || payeeIds.Contains(c.PayeeId))
                 && (!hasAccountFilter || accountIds.Contains(c.BankAccountId))
                 && (!status.HasValue || c.Status == status.Value),
            cancellationToken: cancellationToken);

        var ordered = cheques
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.ChequeNumber)
            .ToList();

        if (ordered.Count == 0) return Array.Empty<ChequeDto>();

        var pagePayeeIds = ordered.Select(x => x.PayeeId).Distinct().ToList();
        var pageAccountIds = ordered.Select(x => x.BankAccountId).Distinct().ToList();

        var payees = await unitOfWork.Payees.ListAsync(
            p => pagePayeeIds.Contains(p.Id),
            cancellationToken: cancellationToken);

        var accounts = await unitOfWork.BankAccounts.ListAsync(
            a => pageAccountIds.Contains(a.Id),
            cancellationToken: cancellationToken);

        var payeeLookup = payees.ToDictionary(p => p.Id, p => p.Name);
        var accountLookup = accounts.ToDictionary(a => a.Id, a => a.AccountNumber);

        return ordered.Select(c =>
                new ChequeDto(
                    c.Id,
                    c.ChequeNumber,
                    c.Date,
                    payeeLookup.TryGetValue(c.PayeeId, out var payeeNameValue) ? payeeNameValue : "N/A",
                    c.Amount,
                    c.AmountInWords,
                    accountLookup.TryGetValue(c.BankAccountId, out var accountValue) ? accountValue : "N/A",
                    c.Status,
                    c.Remarks))
            .ToList();
    }

    public async Task<PagedResult<ChequeDto>> GetFilteredPagedAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var payeeName = filter.PayeeName?.Trim();
        var hasPayeeFilter = !string.IsNullOrWhiteSpace(payeeName);
        IReadOnlyList<Guid> payeeIds = Array.Empty<Guid>();
        if (hasPayeeFilter)
        {
            var payeeLower = payeeName!.ToLowerInvariant();
            payeeIds = (await unitOfWork.Payees.ListAsync(
                    p => p.Name.ToLower().Contains(payeeLower),
                    cancellationToken: cancellationToken))
                .Select(p => p.Id)
                .ToList();

            if (payeeIds.Count == 0)
            {
                return new PagedResult<ChequeDto>(Array.Empty<ChequeDto>(), 1, pageSize, 0, 0);
            }
        }

        var bankAccountNumber = filter.BankAccountNumber?.Trim();
        var hasAccountFilter = !string.IsNullOrWhiteSpace(bankAccountNumber);
        IReadOnlyList<Guid> accountIds = Array.Empty<Guid>();
        if (hasAccountFilter)
        {
            var accountLower = bankAccountNumber!.ToLowerInvariant();
            accountIds = (await unitOfWork.BankAccounts.ListAsync(
                    a => a.AccountNumber.ToLower().Contains(accountLower),
                    cancellationToken: cancellationToken))
                .Select(a => a.Id)
                .ToList();

            if (accountIds.Count == 0)
            {
                return new PagedResult<ChequeDto>(Array.Empty<ChequeDto>(), 1, pageSize, 0, 0);
            }
        }

        var chequeNumber = filter.ChequeNumber?.Trim();
        var chequeNumberLower = !string.IsNullOrWhiteSpace(chequeNumber) ? chequeNumber.ToLowerInvariant() : null;

        var fromDate = filter.FromDate;
        var toDate = filter.ToDate;
        var status = filter.Status;

        var predicate = (System.Linq.Expressions.Expression<Func<Cheque, bool>>)(c =>
            (!fromDate.HasValue || c.Date >= fromDate.Value)
            && (!toDate.HasValue || c.Date <= toDate.Value)
            && (chequeNumberLower == null || c.ChequeNumber.ToLower().Contains(chequeNumberLower))
            && (!hasPayeeFilter || payeeIds.Contains(c.PayeeId))
            && (!hasAccountFilter || accountIds.Contains(c.BankAccountId))
            && (!status.HasValue || c.Status == status.Value));

        var totalCount = await unitOfWork.Cheques.CountAsync(predicate, cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<ChequeDto>(Array.Empty<ChequeDto>(), 1, pageSize, 0, 0);
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page > totalPages) page = totalPages;

        var pageCheques = await unitOfWork.Cheques.ListPagedAsync(
            predicate,
            q => q.OrderByDescending(x => x.Date).ThenByDescending(x => x.ChequeNumber),
            page,
            pageSize,
            cancellationToken);

        var pagePayeeIds = pageCheques.Select(x => x.PayeeId).Distinct().ToList();
        var pageAccountIds = pageCheques.Select(x => x.BankAccountId).Distinct().ToList();

        var payees = await unitOfWork.Payees.ListAsync(
            p => pagePayeeIds.Contains(p.Id),
            cancellationToken: cancellationToken);

        var accounts = await unitOfWork.BankAccounts.ListAsync(
            a => pageAccountIds.Contains(a.Id),
            cancellationToken: cancellationToken);

        var payeeLookup = payees.ToDictionary(p => p.Id, p => p.Name);
        var accountLookup = accounts.ToDictionary(a => a.Id, a => a.AccountNumber);

        var items = pageCheques.Select(c =>
                new ChequeDto(
                    c.Id,
                    c.ChequeNumber,
                    c.Date,
                    payeeLookup.TryGetValue(c.PayeeId, out var payeeNameValue) ? payeeNameValue : "N/A",
                    c.Amount,
                    c.AmountInWords,
                    accountLookup.TryGetValue(c.BankAccountId, out var accountValue) ? accountValue : "N/A",
                    c.Status,
                    c.Remarks))
            .ToList();

        return new PagedResult<ChequeDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<ChequeStatusCountsDto> GetFilteredCountsAsync(
        ReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var payeeName = filter.PayeeName?.Trim();
        var hasPayeeFilter = !string.IsNullOrWhiteSpace(payeeName);
        IReadOnlyList<Guid> payeeIds = Array.Empty<Guid>();
        if (hasPayeeFilter)
        {
            var payeeLower = payeeName!.ToLowerInvariant();
            payeeIds = (await unitOfWork.Payees.ListAsync(
                    p => p.Name.ToLower().Contains(payeeLower),
                    cancellationToken: cancellationToken))
                .Select(p => p.Id)
                .ToList();

            if (payeeIds.Count == 0) return new ChequeStatusCountsDto(0, 0, 0);
        }

        var bankAccountNumber = filter.BankAccountNumber?.Trim();
        var hasAccountFilter = !string.IsNullOrWhiteSpace(bankAccountNumber);
        IReadOnlyList<Guid> accountIds = Array.Empty<Guid>();
        if (hasAccountFilter)
        {
            var accountLower = bankAccountNumber!.ToLowerInvariant();
            accountIds = (await unitOfWork.BankAccounts.ListAsync(
                    a => a.AccountNumber.ToLower().Contains(accountLower),
                    cancellationToken: cancellationToken))
                .Select(a => a.Id)
                .ToList();

            if (accountIds.Count == 0) return new ChequeStatusCountsDto(0, 0, 0);
        }

        var chequeNumber = filter.ChequeNumber?.Trim();
        var chequeNumberLower = !string.IsNullOrWhiteSpace(chequeNumber) ? chequeNumber.ToLowerInvariant() : null;

        var fromDate = filter.FromDate;
        var toDate = filter.ToDate;
        var status = filter.Status;

        // If status is explicitly filtered, only return a count for that status.
        if (status.HasValue)
        {
            var matchingCount = await unitOfWork.Cheques.CountAsync(
                c => (!fromDate.HasValue || c.Date >= fromDate.Value)
                     && (!toDate.HasValue || c.Date <= toDate.Value)
                     && (chequeNumberLower == null || c.ChequeNumber.ToLower().Contains(chequeNumberLower))
                     && (!hasPayeeFilter || payeeIds.Contains(c.PayeeId))
                     && (!hasAccountFilter || accountIds.Contains(c.BankAccountId))
                     && c.Status == status.Value,
                cancellationToken);

            return status.Value switch
            {
                ChequeStatus.Cancelled => new ChequeStatusCountsDto(matchingCount, 0, 0),
                ChequeStatus.Cleared => new ChequeStatusCountsDto(0, 0, matchingCount),
                ChequeStatus.Issued => new ChequeStatusCountsDto(0, matchingCount, 0),
                ChequeStatus.PostDated => new ChequeStatusCountsDto(0, matchingCount, 0),
                _ => new ChequeStatusCountsDto(0, 0, 0)
            };
        }

        var cancelledCount = await unitOfWork.Cheques.CountAsync(
            c => (!fromDate.HasValue || c.Date >= fromDate.Value)
                 && (!toDate.HasValue || c.Date <= toDate.Value)
                 && (chequeNumberLower == null || c.ChequeNumber.ToLower().Contains(chequeNumberLower))
                 && (!hasPayeeFilter || payeeIds.Contains(c.PayeeId))
                 && (!hasAccountFilter || accountIds.Contains(c.BankAccountId))
                 && c.Status == ChequeStatus.Cancelled,
            cancellationToken);

        var clearedCount = await unitOfWork.Cheques.CountAsync(
            c => (!fromDate.HasValue || c.Date >= fromDate.Value)
                 && (!toDate.HasValue || c.Date <= toDate.Value)
                 && (chequeNumberLower == null || c.ChequeNumber.ToLower().Contains(chequeNumberLower))
                 && (!hasPayeeFilter || payeeIds.Contains(c.PayeeId))
                 && (!hasAccountFilter || accountIds.Contains(c.BankAccountId))
                 && c.Status == ChequeStatus.Cleared,
            cancellationToken);

        var pendingCount = await unitOfWork.Cheques.CountAsync(
            c => (!fromDate.HasValue || c.Date >= fromDate.Value)
                 && (!toDate.HasValue || c.Date <= toDate.Value)
                 && (chequeNumberLower == null || c.ChequeNumber.ToLower().Contains(chequeNumberLower))
                 && (!hasPayeeFilter || payeeIds.Contains(c.PayeeId))
                 && (!hasAccountFilter || accountIds.Contains(c.BankAccountId))
                 && (c.Status == ChequeStatus.Issued || c.Status == ChequeStatus.PostDated),
            cancellationToken);

        return new ChequeStatusCountsDto(cancelledCount, pendingCount, clearedCount);
    }

    public async Task<Guid> CreateAsync(CreateChequeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await createChequeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var cheque = new Cheque
        {
            ChequeNumber = request.ChequeNumber,
            Date = request.Date,
            PayeeId = request.PayeeId,
            Amount = request.Amount,
            AmountInWords = converter.Convert(request.Amount),
            BankAccountId = request.BankAccountId,
            Status = request.Status,
            Remarks = request.Remarks
        };

        await unitOfWork.Cheques.AddAsync(cheque, cancellationToken);
        await unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            EntityName = nameof(Cheque),
            EntityId = cheque.Id.ToString(),
            Action = "CREATE",
            Payload = $"Cheque {cheque.ChequeNumber} created."
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return cheque.Id;
    }

    public async Task CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var cheque = await unitOfWork.Cheques.GetByIdAsync(id, cancellationToken)
                     ?? throw new KeyNotFoundException("Cheque not found.");

        if (cheque.Status == ChequeStatus.Cancelled)
        {
            throw new InvalidOperationException("This cheque is already cancelled.");
        }

        if (cheque.Status == ChequeStatus.Cleared)
        {
            throw new InvalidOperationException("Cleared cheques cannot be cancelled.");
        }

        cheque.Status = ChequeStatus.Cancelled;
        cheque.Remarks = reason;
        unitOfWork.Cheques.Update(cheque);
        await unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            EntityName = nameof(Cheque),
            EntityId = id.ToString(),
            Action = "CANCEL",
            Payload = reason
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardDto> BuildDashboardAsync(CancellationToken cancellationToken = default)
    {
        var cheques = await unitOfWork.Cheques.ListAsync(cancellationToken: cancellationToken);
        var accounts = await unitOfWork.BankAccounts.ListAsync(cancellationToken: cancellationToken);
        var banks = await unitOfWork.Banks.ListAsync(cancellationToken: cancellationToken);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcomingMax = now.AddDays(14);

        var balances = banks.ToDictionary(
            b => b.Name,
            b =>
            {
                var accountIds = accounts.Where(a => a.BankId == b.Id).Select(a => a.Id).ToHashSet();
                var opening = accounts.Where(a => a.BankId == b.Id).Sum(a => a.OpeningBalance);
                var cleared = cheques.Where(c => accountIds.Contains(c.BankAccountId) && c.Status == ChequeStatus.Cleared).Sum(c => c.Amount);
                return opening - cleared;
            });

        return new DashboardDto(
            cheques.Count,
            cheques.Where(c => c.Status == ChequeStatus.Cleared).Sum(c => c.Amount),
            cheques.Count(c => c.Status is ChequeStatus.Issued or ChequeStatus.PostDated),
            cheques.Count(c => c.Status == ChequeStatus.PostDated && c.Date >= now && c.Date <= upcomingMax),
            balances
        );
    }
}
