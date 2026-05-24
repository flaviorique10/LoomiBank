using Transactions.Domain.Enums;

namespace Transactions.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid SenderId { get; private set; }
    public Guid ReceiverId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor principal
    public Transaction(Guid senderId, Guid receiverId, decimal amount)
    {
        Id = Guid.NewGuid();
        SenderId = senderId;
        ReceiverId = receiverId;
        Amount = amount;
        Status = TransactionStatus.Pending; // Toda transação nasce como pendente
        CreatedAt = DateTime.UtcNow; // Você no controle: horário unificado padrão UTC
    }

    // Construtor vazio exigido pelo Entity Framework
    protected Transaction() { }

    // Método de negócio para alterar o status no futuro (quando formos processar)
    public void UpdateStatus(TransactionStatus newStatus)
    {
        Status = newStatus;
    }
}