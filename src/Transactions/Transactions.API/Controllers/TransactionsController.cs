using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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

    // Endpoint de consulta (Mock) apenas para o CreatedAtAction funcionar e não quebrar a API
    // (Implementaremos a busca real no próximo card)
    [HttpGet("{id:guid}")]
    public IActionResult GetTransactionById(Guid id)
    {
        return Ok();
    }
}