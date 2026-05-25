using Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Persistence;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(builder =>
        {
            // Nome da tabela e Chave Primária
            builder.ToTable("Customers");
            builder.HasKey(c => c.Id);

            // Mapeamento das propriedades básicas
            builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Address).IsRequired().HasMaxLength(200);
            builder.Property(c => c.ProfilePictureUrl).HasMaxLength(500);

            // Mapeamento do Value Object (BankingDetails)
            // O EF Core vai criar essas colunas na mesma tabela "Customers"
            builder.OwnsOne(c => c.BankingDetails, b =>
            {
                b.Property(bd => bd.Agency)
                 .HasColumnName("Agency")
                 .IsRequired()
                 .HasMaxLength(10);

                b.Property(bd => bd.AccountNumber)
                 .HasColumnName("AccountNumber")
                 .IsRequired()
                 .HasMaxLength(20);
            });
        });
    }
}