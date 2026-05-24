using Customers.Domain.Entities;
using Customers.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Customers.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void UpdateProfilePicture_GivenValidUrl_ShouldUpdateProperty()
    {
        // Arrange
        var banking = new BankingDetails("1234", "56789-0");
        var customer = new Customer("João", "joao@email.com", "Rua Y", banking);
        var newUrl = "https://azure.com/foto.jpg";

        // Act
        customer.UpdateProfilePicture(newUrl);

        // Assert
        customer.ProfilePictureUrl.Should().Be(newUrl);
    }
}