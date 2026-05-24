using Customers.Application.Services;
using Customers.Application.Validators;
using Customers.Infrastructure.Persistence;
using Customers.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Suporte a Controllers
builder.Services.AddControllers();

// Registrando o FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CustomerUpdateRequestValidator>();

// Configuração do Banco de Dados (PostgreSQL)
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração do Cache Distribuído (Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "LoomiBank_Customers_";
});

// Configuração do Serviço de Upload da Azure
var blobConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
builder.Services.AddScoped<IBlobStorageService>(provider =>
    new AzureBlobStorageService(blobConnectionString!));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Mapeamento dos Controllers
app.MapControllers();

app.Run();