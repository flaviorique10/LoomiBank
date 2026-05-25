using FluentValidation;
using FluentValidation.Results;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Transactions.API.Controllers;
using Transactions.Application.DTOs;
using Transactions.Application.Events; 
using Transactions.Application.Interfaces;
using Transactions.Infrastructure.Persistence;

namespace Transactions.UnitTests.Controllers;

public class TransactionsControllerTests
{
    private readonly Mock<IValidator<CreateTransactionRequestDto>> _validatorMock;
    private readonly Mock<ICustomerIntegrationService> _customerServiceMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly TransactionDbContext _dbContext;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _validatorMock = new Mock<IValidator<CreateTransactionRequestDto>>();
        _customerServiceMock = new Mock<ICustomerIntegrationService>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        // Configura um banco de dados em memória do EF Core para o teste
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TransactionDbContext(options);

        _controller = new TransactionsController(_dbContext);
    }

    [Fact]
    public async Task CreateTransaction_WhenSenderDoesNotExist_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionRequestDto(Guid.NewGuid(), Guid.NewGuid(), 100m);

        // Simula que a validação primária passou (Garantindo que vem do FluentValidation)
        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Simula que a chamada pro microsserviço de Clientes retornou FALSE (remetente não existe)
        _customerServiceMock.Setup(c => c.CheckCustomerExistsAsync(request.SenderId))
                            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateTransaction(request, _validatorMock.Object, _customerServiceMock.Object, _publishEndpointMock.Object);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        // Garante que não salvou nada no banco
        var transactionsInDb = await _dbContext.Transactions.ToListAsync();
        transactionsInDb.Should().BeEmpty();

        // Garante que não publicou nenhum evento (Corrigido para o tipo exato)
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<TransferCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_WhenValid_ShouldSaveAndPublishEvent()
    {
        // Arrange
        var request = new CreateTransactionRequestDto(Guid.NewGuid(), Guid.NewGuid(), 500m);

        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Simula que AMBOS os clientes existem
        _customerServiceMock.Setup(c => c.CheckCustomerExistsAsync(It.IsAny<Guid>()))
                            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateTransaction(request, _validatorMock.Object, _customerServiceMock.Object, _publishEndpointMock.Object);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();

        // Garante que salvou no banco corretamente
        var transactionsInDb = await _dbContext.Transactions.ToListAsync();
        transactionsInDb.Should().HaveCount(1);
        transactionsInDb.First().Amount.Should().Be(500m);

        // Garante que o evento foi publicado para o RabbitMQ (Corrigido para o tipo exato)
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<TransferCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}