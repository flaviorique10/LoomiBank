using Customers.Application.DTOs;
using Customers.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Customers.UnitTests.Validators;

public class CustomerUpdateRequestValidatorTests
{
    private readonly CustomerUpdateRequestValidator _validator;

    public CustomerUpdateRequestValidatorTests()
    {
        // Arrange: Prepara o validador para todos os testes
        _validator = new CustomerUpdateRequestValidator();
    }

    [Fact]
    public void Validate_GivenValidRequest_ShouldNotHaveErrors()
    {
        // Arrange (Preparação)
        var request = new CustomerUpdateRequestDto("João da Silva", "joao@email.com", "Rua Principal, 123", null);

        // Act (Ação)
        var result = _validator.Validate(request);

        // Assert (Verificação)
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_GivenInvalidEmail_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CustomerUpdateRequestDto("João da Silva", "email-invalido-sem-arroba", "Rua Principal, 123", null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_GivenShortName_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CustomerUpdateRequestDto("Jo", "joao@email.com", "Rua Principal", null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}