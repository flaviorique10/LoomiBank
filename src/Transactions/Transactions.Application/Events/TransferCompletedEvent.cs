namespace Transactions.Application.Events;

public record TransferCompletedEvent(
    Guid TransactionId,
    Guid SenderId,
    Guid ReceiverId,
    decimal Amount,
    DateTime CompletedAt
);