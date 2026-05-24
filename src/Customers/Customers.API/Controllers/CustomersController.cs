using System.Text.Json;
using Customers.Application.DTOs;
using Customers.Domain.ValueObjects;
using Customers.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Customers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _context;
    private readonly IDistributedCache _cache;

    public CustomersController(CustomerDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerById(Guid id)
    {
        var cacheKey = $"customer_{id}";

        // 1. Tenta buscar no Redis primeiro
        var cachedCustomer = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedCustomer))
        {
            // Cache Hit: Encontrou no Redis, desserializa e retorna direto
            var customerDto = JsonSerializer.Deserialize<CustomerResponseDto>(cachedCustomer);
            return Ok(customerDto);
        }

        // 2. Cache Miss: Não encontrou no Redis, vai no PostgreSQL
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return NotFound(new { Message = "Cliente não encontrado." });
        }

        // 3. Mapeia a Entidade para o DTO
        var responseDto = new CustomerResponseDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Address,
            customer.ProfilePictureUrl,
            new BankingDetailsDto(customer.BankingDetails.Agency, customer.BankingDetails.AccountNumber)
        );

        // 4. Salva no Redis para as próximas consultas (com expiração de 10 minutos)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(responseDto), cacheOptions);

        // 5. Retorna o dado original
        return Ok(responseDto);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateCustomerPartial(
        [FromRoute] Guid id,
        [FromBody] CustomerUpdateRequestDto request,
        [FromServices] IValidator<CustomerUpdateRequestDto> validator)
    {
        // 1. Validação do DTO usando FluentValidation
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
        }

        // 2. Busca do Cliente no Banco
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return NotFound(new { Message = "Cliente não encontrado." });
        }

        // 3. Montagem do Value Object (se enviado)
        BankingDetails? newBankingDetails = null;
        if (request.BankingDetails != null)
        {
            newBankingDetails = new BankingDetails(
                request.BankingDetails.Agency!,
                request.BankingDetails.AccountNumber!);
        }

        // 4. Atualização segura da Entidade
        customer.Update(request.Name, request.Email, request.Address, newBankingDetails);

        // 5. Salva no banco de dados
        await _context.SaveChangesAsync();

        // 6. Invalidação do Cache no Redis (Para evitar dados "fantasmas")
        var cacheKey = $"customer_{id}";
        await _cache.RemoveAsync(cacheKey);

        return NoContent(); // 204 No Content é o padrão REST para atualizações bem-sucedidas
    }
}