using System.Net;
using Transactions.Application.Interfaces;

namespace Transactions.Infrastructure.Services;

public class CustomerIntegrationService : ICustomerIntegrationService
{
    private readonly HttpClient _httpClient;

    public CustomerIntegrationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckCustomerExistsAsync(Guid customerId)
    {
        // Tenta buscar o cliente lá no outro microsserviço
        var response = await _httpClient.GetAsync($"/api/customers/{customerId}");

        // Se retornar 200 OK, o cliente existe
        if (response.IsSuccessStatusCode)
            return true;

        // Se retornar 404, o cliente não existe
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        // Se retornar erro 500 (banco caiu lá no cliente) ou timeout, 
        // a exceção aciona o Polly para tentar de novo (Retry) ou abrir o Circuito
        response.EnsureSuccessStatusCode();

        return false;
    }
}