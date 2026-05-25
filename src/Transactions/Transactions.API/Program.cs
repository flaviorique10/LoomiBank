using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polly.Extensions.Http;
using Polly;
using Transactions.Application.Interfaces;
using Transactions.Application.Validators;
using Transactions.Infrastructure.Persistence;
using Transactions.Infrastructure.Services;
using MassTransit;
using Transactions.Application.Consumers;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Swagger com suporte a JWT e XML Comments
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LoomiBank API",
        Version = "v1",
        Description = "API do sistema bancário LoomiBank"
    });

    // Configuração do botão de Authorize (JWT)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu_token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});

    // Configuração para ler os comentários XML dos endpoints
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddHttpContextAccessor();

// 1. Libera o uso de Controllers
builder.Services.AddControllers();

// CONFIGURAÇÃO DO JWT BEARER AUTHENTICATION (APENAS VALIDAÇÃO)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

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
    client.BaseAddress = new Uri("http://localhost:5130");

    // 3. Política de Timeout: Se a API de clientes demorar mais de 5 segundos para responder, corta a requisição
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// CONFIGURAÇÃO DO MASSTRANSIT (RABBITMQ)
builder.Services.AddMassTransit(x =>
{
    // Registra o Consumer que criamos
    x.AddConsumer<TransferCompletedEventConsumer>();

    // Configura o transporte usando RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configura automaticamente as filas baseadas nos Consumers registrados
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoomiBank API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 4. Mapeia as rotas para o TransactionsController que acabamos de criar
app.MapControllers();

app.Run();