using ChequePrintingSystem.Application.DTOs;
using ChequePrintingSystem.Domain.Enums;
using FluentValidation;

namespace ChequePrintingSystem.Application.Validation;

public class CreateChequeRequestValidator : AbstractValidator<CreateChequeRequest>
{
    public CreateChequeRequestValidator()
    {
        RuleFor(x => x.ChequeNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PayeeId).NotEmpty();
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(512).When(x => x.Remarks is not null);
        RuleFor(x => x.Status)
            .Must(s => s is ChequeStatus.Issued or ChequeStatus.PostDated)
            .WithMessage("New cheques must be Issued or PostDated.");
    }
}
