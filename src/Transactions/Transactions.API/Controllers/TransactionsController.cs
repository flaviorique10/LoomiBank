using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transactions.Application.DTOs;
using Transactions.Domain.Entities;
using Transactions.Infrastructure.Persistence;

namespace Transactions.API.Controllers;

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
        [FromServices] IValidator<CreateTransactionRequestDto> validator)
    {
        // 1. Validação dos dados de entrada usando FluentValidation
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
        }

        // 2. Criação da Entidade (Nasce como Pending e gera a data UTC automaticamente pelo construtor)
        var transaction = new Transaction(request.SenderId, request.ReceiverId, request.Amount);

        // 3. Salva no banco de dados (PostgreSQL)
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // 4. Monta o DTO de resposta
        var response = new TransactionResponseDto(transaction.Id, transaction.Status.ToString(), transaction.CreatedAt);

        // 5. Retorna 201 Created (Padrão REST) com os dados do protocolo
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
}