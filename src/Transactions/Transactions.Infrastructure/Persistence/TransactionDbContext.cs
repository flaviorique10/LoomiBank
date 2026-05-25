using Microsoft.EntityFrameworkCore;
using Transactions.Domain.Entities;

namespace Transactions.Infrastructure.Persistence;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Amount)
                  .IsRequired()
                  .HasColumnType("decimal(18,2)");

            // Salva o Enum como texto ("Pending", "Completed") no banco para facilitar a leitura humana
            entity.Property(t => t.Status)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(t => t.CreatedAt)
                  .IsRequired();

            // Índices para otimizar a busca do histórico de transações de um usuário específico
            entity.HasIndex(t => t.SenderId);
            entity.HasIndex(t => t.ReceiverId);
        });

        base.OnModelCreating(modelBuilder);
    }
}