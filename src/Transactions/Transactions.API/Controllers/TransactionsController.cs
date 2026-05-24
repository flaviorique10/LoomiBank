using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transactions.Application.DTOs;
using Transactions.Application.Interfaces;
using Transactions.Domain.Entities;
using Transactions.Infrastructure.Persistence;

namespace Transactions.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionDbContext _context;

    public TransactionsController(TransactionDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequestDto request,
        [FromServices] IValidator<CreateTransactionRequestDto> validator,
        [FromServices] ICustomerIntegrationService customerIntegrationService,
        [FromServices] MassTransit.IPublishEndpoint publishEndpoint) // <- Serviço injetado aqui
    {
        // 1. Validação dos dados de entrada usando FluentValidation
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
        }

        // 2. Validação Resiliente entre Microsserviços (Usando o Polly por debaixo dos panos)
        var senderExists = await customerIntegrationService.CheckCustomerExistsAsync(request.SenderId);
        if (!senderExists)
            return BadRequest(new { Message = "A conta do remetente (SenderId) não existe ou está inativa." });

        var receiverExists = await customerIntegrationService.CheckCustomerExistsAsync(request.ReceiverId);
        if (!receiverExists)
            return BadRequest(new { Message = "A conta do destinatário (ReceiverId) não existe ou está inativa." });

        // 3. Criação da Entidade
        var transaction = new Transaction(request.SenderId, request.ReceiverId, request.Amount);

        // 4. Salva no banco de dados (PostgreSQL)
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // 5. Publica o evento no RabbitMQ de forma assíncrona
        var evento = new Transactions.Application.Events.TransferCompletedEvent(
            transaction.Id,
            transaction.SenderId,
            transaction.ReceiverId,
            transaction.Amount,
            transaction.CreatedAt
        );
        await publishEndpoint.Publish(evento);

        // 6. Monta o DTO de resposta
        var response = new TransactionResponseDto(transaction.Id, transaction.Status.ToString(), transaction.CreatedAt);

        // 7. Retorna 201 Created
        return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTransactionById([FromRoute] Guid id)
    {
        // Busca a transação no banco de dados pela chave primária
        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        // Tratamento de erro 404 (Not Found)
        if (transaction == null)
        {
            return NotFound(new { Message = "Transação não encontrada." });
        }

        // Mapeia a Entidade para o DTO de detalhes
        var response = new TransactionDetailsResponseDto(
            transaction.Id,
            transaction.SenderId,
            transaction.ReceiverId,
            transaction.Amount,
            transaction.Status.ToString(),
            transaction.CreatedAt
        );

        return Ok(response); // Retorna 200 OK com os dados
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetTransactionsByUserId([FromRoute] Guid userId)
    {
        // Consulta as transações filtrando por SenderId OU ReceiverId, ordenando pelas mais recentes
        var transactions = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.SenderId == userId || t.ReceiverId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        // Mapeia a lista de entidades para a lista de DTOs
        var response = transactions.Select(t => new UserTransactionHistoryDto(
            t.Id,
            t.SenderId,
            t.ReceiverId,
            t.Amount,
            t.Status.ToString(),
            t.CreatedAt
        )).ToList();

        // Retorna 200 OK com a lista (mesmo se vazia, o padrão REST para listagens é retornar array vazio [] com status 200)
        return Ok(response);
    }
}