namespace Transactions.Application.DTOs;

public record TransactionResponseDto(Guid TransactionId, string Status, DateTime CreatedAt);