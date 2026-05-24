using FluentAssertions;
using Transactions.Application.DTOs;
using Transactions.Application.Validators;
using Xunit;

namespace Transactions.UnitTests.Validators;

public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator;

    public CreateTransactionRequestValidatorTests()
    {
        _validator = new CreateTransactionRequestValidator();
    }

    [Fact]
    public void Validate_GivenValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new CreateTransactionRequestDto(Guid.NewGuid(), Guid.NewGuid(), 100.50m);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GivenSameSenderAndReceiver_ShouldHaveError()
    {
        // Arrange
        var sameId = Guid.NewGuid();
        var request = new CreateTransactionRequestDto(sameId, sameId, 100.00m);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReceiverId");
    }

    [Fact]
    public void Validate_GivenZeroOrNegativeAmount_ShouldHaveError()
    {
        // Arrange
        var request = new CreateTransactionRequestDto(Guid.NewGuid(), Guid.NewGuid(), 0m);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }
}