using FluentValidation;
using Transactions.Application.DTOs;

namespace Transactions.Application.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequestDto>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.SenderId)
            .NotEmpty().WithMessage("O ID do remetente é obrigatório.");

        RuleFor(x => x.ReceiverId)
            .NotEmpty().WithMessage("O ID do destinatário é obrigatório.")
            .NotEqual(x => x.SenderId).WithMessage("O remetente e o destinatário não podem ser a mesma pessoa.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor da transferência deve ser maior que zero.");
    }
}