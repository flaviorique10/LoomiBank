namespace Customers.Application.DTOs;

public record BankingDetailsDto(string Agency, string AccountNumber);

public record CustomerResponseDto(
    Guid Id,
    string Name,
    string Email,
    string Address,
    string? ProfilePictureUrl,
    BankingDetailsDto BankingDetails
);