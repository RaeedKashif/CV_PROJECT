using ChequePrintingSystem.Application.Abstractions;
using ChequePrintingSystem.Application.DTOs;
using ChequePrintingSystem.Application.Services;
using ChequePrintingSystem.Domain.Entities;
using ChequePrintingSystem.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace ChequePrintingSystem.Application.Tests;

public class ChequeServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateChequeAndAuditLog()
    {
        var cheques = new Mock<IRepository<Cheque>>();
        var logs = new Mock<IRepository<AuditLog>>();
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(x => x.Cheques).Returns(cheques.Object);
        uow.SetupGet(x => x.AuditLogs).Returns(logs.Object);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var validator = new Mock<IValidator<CreateChequeRequest>>();
        validator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateChequeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var service = new ChequeService(uow.Object, new AmountInWordsConverter(), validator.Object);
        var request = new CreateChequeRequest("CHQ-1", DateOnly.FromDateTime(DateTime.Today), Guid.NewGuid(), 250m, Guid.NewGuid(), ChequeStatus.Issued, null);

        var id = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        cheques.Verify(x => x.AddAsync(It.IsAny<Cheque>(), It.IsAny<CancellationToken>()), Times.Once);
        logs.Verify(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
