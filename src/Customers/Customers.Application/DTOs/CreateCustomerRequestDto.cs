namespace Customers.Application.DTOs;

public record BankingDetailsCreateDto(string Agency, string AccountNumber);

public record CreateCustomerRequestDto(
    string Name,
    string Email,
    string Address,
    BankingDetailsCreateDto? BankingDetails 
);