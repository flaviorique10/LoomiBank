using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polly.Extensions.Http;
using Polly;
using Transactions.Application.Interfaces;
using Transactions.Application.Validators;
using Transactions.Infrastructure.Persistence;
using Transactions.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Libera o uso de Controllers
builder.Services.AddControllers();

// 2. Registra o FluentValidation automaticamente para a nossa classe de validação
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();

// 3. Configuração do Banco de Dados (PostgreSQL)
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CONFIGURAÇÃO DE RESILIÊNCIA (POLLY) PARA O MICROSERVIÇO
// 1. Política de Retry (Tenta 3 vezes. Espera 1s, depois 2s, depois 4s antes de tentar de novo)
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // Lida com Erro 5xx e Timeout
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

// 2. Política de Circuit Breaker (Se falhar 5 vezes seguidas, "abre o circuito" e para de tentar por 30 segundos)
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}

// Registro do HttpClient com a injeção do serviço e as políticas
builder.Services.AddHttpClient<ICustomerIntegrationService, CustomerIntegrationService>(client =>
{    
    client.BaseAddress = new Uri("https://localhost:7285");

    // 3. Política de Timeout: Se a API de clientes demorar mais de 5 segundos para responder, corta a requisição
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

var app = builder.Build();

app.UseHttpsRedirection();

// 4. Mapeia as rotas para o TransactionsController que acabamos de criar
app.MapControllers();

app.Run();