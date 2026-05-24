namespace Transactions.Application.DTOs;

public record UserTransactionHistoryDto(
    Guid Id,
    Guid SenderId,
    Guid ReceiverId,
    decimal Amount,
    string Status,
    DateTime CreatedAt
);