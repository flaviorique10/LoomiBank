using System.Text.Json;
using Customers.Application.DTOs;
using Customers.Application.Services;
using Customers.Domain.Entities;
using Customers.Domain.ValueObjects;
using Customers.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Customers.API.Controllers;

[Authorize]
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

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(
        [FromRoute] Guid id,
        IFormFile file,
        [FromServices] IBlobStorageService blobStorageService)
    {
        // 1. Validação básica de segurança
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Message = "Nenhum arquivo foi enviado." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { Message = "Apenas imagens JPG, JPEG e PNG são permitidas." });
        }

        // 2. Busca o cliente no banco
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return NotFound(new { Message = "Cliente não encontrado." });
        }

        // 3. Faz o upload para o Azure Blob Storage
        using var stream = file.OpenReadStream();
        var imageUrl = await blobStorageService.UploadProfilePictureAsync(stream, file.FileName, file.ContentType);

        // 4. Atualiza a entidade e salva no banco de dados
        customer.UpdateProfilePicture(imageUrl);
        await _context.SaveChangesAsync();

        // 5. Invalida o cache do Redis para não retornar a foto velha
        var cacheKey = $"customer_{id}";
        await _cache.RemoveAsync(cacheKey);

        return Ok(new { ProfilePictureUrl = imageUrl });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer(
    [FromBody] CreateCustomerRequestDto request,
    [FromServices] IValidator<CreateCustomerRequestDto> validator)
    {
        // 1. Validação do DTO usando FluentValidation (mesmo padrão do seu Patch)
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
        }

        // 2. Regra de Negócio: Verifica se o e-mail já está em uso
        var emailExists = await _context.Customers.AnyAsync(c => c.Email == request.Email);
        if (emailExists)
        {
            return Conflict(new { Message = "Já existe um cliente cadastrado com este e-mail." });
        }

        // 3. Montagem do Value Object (BankingDetails)
        BankingDetails? bankingDetails = null;
        if (request.BankingDetails != null)
        {
            bankingDetails = new BankingDetails(
                request.BankingDetails.Agency!,
                request.BankingDetails.AccountNumber!);
        }

        // 4. Instancia a Entidade de Domínio (Ajuste conforme o construtor da sua classe Customer)
        var customer = new Customer(
            request.Name,
            request.Email,
            request.Address,
            bankingDetails
        );

        // 5. Salva no banco de dados
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // 6. Retorna 201 Created apontando para a rota de GetCustomerById
        return CreatedAtAction(
            nameof(GetCustomerById),
            new { id = customer.Id },
            new { Message = "Cliente criado com sucesso!", CustomerId = customer.Id }
        );
    }
}