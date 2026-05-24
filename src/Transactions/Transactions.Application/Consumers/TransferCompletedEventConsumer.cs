using MassTransit;
using Microsoft.Extensions.Logging;
using Transactions.Application.Events;

namespace Transactions.Application.Consumers;

public class TransferCompletedEventConsumer : IConsumer<TransferCompletedEvent>
{
    private readonly ILogger<TransferCompletedEventConsumer> _logger;

    public TransferCompletedEventConsumer(ILogger<TransferCompletedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransferCompletedEvent> context)
    {
        var evento = context.Message;

        // Aqui simularíamos a chamada para um serviço de envio de E-mail, SMS ou Push Notification
        _logger.LogInformation("=====================================================");
        _logger.LogInformation($"[EVENTO RECEBIDO] Processando notificação de transferência!");
        _logger.LogInformation($"Transação: {evento.TransactionId}");
        _logger.LogInformation($"De: {evento.SenderId} | Para: {evento.ReceiverId}");
        _logger.LogInformation($"Valor: R$ {evento.Amount}");
        _logger.LogInformation("=====================================================");

        return Task.CompletedTask;
    }
}