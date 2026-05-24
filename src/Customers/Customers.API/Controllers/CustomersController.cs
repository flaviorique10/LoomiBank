using System.Text.Json;
using Customers.Application.DTOs;
using Customers.Infrastructure.Persistence;
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
}