using Customers.Domain.ValueObjects;

namespace Customers.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Address { get; private set; }
    public string? ProfilePictureUrl { get; private set; }

    // Vínculo com o Value Object de dados bancários
    public BankingDetails BankingDetails { get; private set; }

    // Construtor principal para criar novos clientes
    public Customer(string name, string email, string address, BankingDetails bankingDetails, string? profilePictureUrl = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        Address = address;
        BankingDetails = bankingDetails;
        ProfilePictureUrl = profilePictureUrl;
    }

    // Construtor vazio exigido pelo Entity Framework Core
    protected Customer() { }

    // Método para atualização parcial mantendo a classe no controle de seu estado
    public void Update(string? name, string? email, string? address, BankingDetails? bankingDetails)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(email)) Email = email;
        if (!string.IsNullOrWhiteSpace(address)) Address = address;
        if (bankingDetails != null) BankingDetails = bankingDetails;
    }
}