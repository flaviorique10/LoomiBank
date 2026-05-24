namespace Transactions.Application.Interfaces;

public interface ICustomerIntegrationService
{
    Task<bool> CheckCustomerExistsAsync(Guid customerId);
}