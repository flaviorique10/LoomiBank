using System.Net;
using Microsoft.AspNetCore.Http; 
using Transactions.Application.Interfaces;

namespace Transactions.Infrastructure.Services;

public class CustomerIntegrationService : ICustomerIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor; 

    public CustomerIntegrationService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CheckCustomerExistsAsync(Guid customerId)
    {
        // 1. Criamos a requisição manualmente em vez de usar só o GetAsync rápido
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/customers/{customerId}");

        // 2. Pegamos o "crachá" (Token JWT) da requisição original que chegou em Transações
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        // 3. Se o token existir, colamos ele no cabeçalho da nova requisição para o Customers API
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.Add("Authorization", authHeader);
        }

        // 4. Disparamos a requisição "carregando o crachá"
        var response = await _httpClient.SendAsync(request);

        // Se retornar 200 OK, o cliente existe
        if (response.IsSuccessStatusCode)
            return true;

        // Se retornar 404, o cliente não existe
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        // Se retornar erro 500 ou timeout, a exceção aciona o Polly
        response.EnsureSuccessStatusCode();

        return false;
    }
}