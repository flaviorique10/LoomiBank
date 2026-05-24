using Customers.Application.DTOs;
using FluentValidation;

namespace Customers.Application.Validators;

public class CustomerUpdateRequestValidator : AbstractValidator<CustomerUpdateRequestDto>
{
    public CustomerUpdateRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("O formato do e-mail é inválido.")
            .When(x => !string.IsNullOrEmpty(x.Email)); // Só valida se o campo for enviado

        RuleFor(x => x.Name)
            .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        // Validação aninhada para os dados bancários
        When(x => x.BankingDetails != null, () =>
        {
            RuleFor(x => x.BankingDetails!.Agency)
                .NotEmpty().WithMessage("A agência é obrigatória quando os dados bancários são informados.")
                .MaximumLength(10).WithMessage("A agência deve ter no máximo 10 caracteres.");

            RuleFor(x => x.BankingDetails!.AccountNumber)
                .NotEmpty().WithMessage("O número da conta é obrigatório.")
                .MaximumLength(20).WithMessage("A conta deve ter no máximo 20 caracteres.");
        });
    }
}