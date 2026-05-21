namespace ChequePrintingSystem.Application.DTOs;

public record ChequeStatusCountsDto(
    int CancelledCount,
    int PendingCount,
    int ClearedCount);

