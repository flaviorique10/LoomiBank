using Customers.Application.DTOs;
using FluentValidation;

namespace Customers.Application.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequestDto>
{
    public CreateCustomerRequestValidator()
    {
        // Na criação, o nome é obrigatório
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        // O e-mail também é obrigatório e deve ser válido
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O formato do e-mail é inválido.");

        // Endereço obrigatório
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("O endereço é obrigatório.");

        // Validação aninhada para os dados bancários (mantivemos igual ao Update)
        // Como no DTO o BankingDetails pode ser nulo (opcional), só validamos se ele for enviado
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