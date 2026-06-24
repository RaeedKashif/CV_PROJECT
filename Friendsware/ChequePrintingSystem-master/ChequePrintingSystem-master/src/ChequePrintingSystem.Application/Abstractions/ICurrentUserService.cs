namespace ChequePrintingSystem.Application.Abstractions;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
}
