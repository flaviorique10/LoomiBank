namespace Transactions.Application.DTOs;

public record TransactionDetailsResponseDto(
    Guid Id,
    Guid SenderId,
    Guid ReceiverId,
    decimal Amount,
    string Status,
    DateTime CreatedAt
);