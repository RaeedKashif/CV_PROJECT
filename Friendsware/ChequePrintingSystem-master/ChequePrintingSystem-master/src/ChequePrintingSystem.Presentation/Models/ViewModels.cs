using ChequePrintingSystem.Application.DTOs;
using System.ComponentModel.DataAnnotations;
using ChequePrintingSystem.Domain.Enums;

namespace ChequePrintingSystem.Presentation.Models;

public class DashboardViewModel
{
    public DashboardDto Data { get; set; } = new(0, 0, 0, 0, new Dictionary<string, decimal>());
}

public class ChequeCreateViewModel
{
    [Required(ErrorMessage = "Cheque number is required.")]
    [StringLength(32)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Payee is required.")]
    public Guid PayeeId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Bank account is required.")]
    public Guid BankAccountId { get; set; }

    public ChequeStatus Status { get; set; } = ChequeStatus.Issued;

    [StringLength(512)]
    public string? Remarks { get; set; }
}

public class BankFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(32)]
    public string? SwiftCode { get; set; }
}

public class PayeeFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(128)]
    public string? Email { get; set; }

    [StringLength(32)]
    public string? Phone { get; set; }
}

public class BankAccountFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required]
    public Guid BankId { get; set; }

    [Required]
    [StringLength(64)]
    public string AccountNumber { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal OpeningBalance { get; set; }

    public decimal DateX { get; set; }
    public decimal DateY { get; set; }
    public decimal PayeeX { get; set; }
    public decimal PayeeY { get; set; }
    public decimal AmountNumericX { get; set; }
    public decimal AmountNumericY { get; set; }
    public decimal AmountWordsX { get; set; }
    public decimal AmountWordsY { get; set; }
    public string? TemplateCss { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(TemplateCss))
        {
            yield break;
        }

        if (TemplateCss.Contains('<') || TemplateCss.Contains('>'))
        {
            yield return new ValidationResult(
                "Template CSS cannot contain angle brackets or HTML tags.",
                [nameof(TemplateCss)]);
        }
    }
}

public class ReportFilterViewModel
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? PayeeName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? ChequeNumber { get; set; }
    public ChequeStatus? Status { get; set; }
    public IReadOnlyList<ChequePrintingSystem.Application.DTOs.ChequeDto> Rows { get; set; } =
        Array.Empty<ChequePrintingSystem.Application.DTOs.ChequeDto>();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ChequeIndexViewModel
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? PayeeName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? ChequeNumber { get; set; }
    public ChequeStatus? Status { get; set; }

    public IReadOnlyList<ChequePrintingSystem.Application.DTOs.ChequeDto> Rows { get; set; } =
        Array.Empty<ChequePrintingSystem.Application.DTOs.ChequeDto>();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class PagedListViewModel<T>
{
    public string? Query { get; set; }
    public IReadOnlyList<T> Rows { get; set; } = Array.Empty<T>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ChequePrintViewModel
{
    public Guid ChequeId { get; set; }
    public string ChequeNumber { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountInWords { get; set; } = string.Empty;
    public decimal DateX { get; set; }
    public decimal DateY { get; set; }
    public decimal PayeeX { get; set; }
    public decimal PayeeY { get; set; }
    public decimal AmountNumericX { get; set; }
    public decimal AmountNumericY { get; set; }
    public decimal AmountWordsX { get; set; }
    public decimal AmountWordsY { get; set; }
    public string TemplateCss { get; set; } = string.Empty;
}
