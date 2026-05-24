namespace Customers.Application.DTOs;

public record BankingDetailsUpdateDto(string? Agency, string? AccountNumber);

public record CustomerUpdateRequestDto(
    string? Name,
    string? Email,
    string? Address,
    BankingDetailsUpdateDto? BankingDetails
);