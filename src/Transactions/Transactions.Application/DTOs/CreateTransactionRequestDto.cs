namespace Transactions.Application.DTOs;

public record CreateTransactionRequestDto(Guid SenderId, Guid ReceiverId, decimal Amount);