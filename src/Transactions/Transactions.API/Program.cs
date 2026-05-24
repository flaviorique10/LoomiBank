using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Transactions.Application.Validators;
using Transactions.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Libera o uso de Controllers
builder.Services.AddControllers();

// 2. Registra o FluentValidation automaticamente para a nossa classe de validação
builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();

// 3. Configuração do Banco de Dados (PostgreSQL)
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseHttpsRedirection();

// 4. Mapeia as rotas para o TransactionsController que acabamos de criar
app.MapControllers();

app.Run();