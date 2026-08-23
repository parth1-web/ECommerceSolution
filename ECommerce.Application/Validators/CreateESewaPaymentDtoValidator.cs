using ECommerce.Application.DTOs.Payments;
using FluentValidation;

namespace ECommerce.Application.Validators;

public class CreateESewaPaymentDtoValidator
    : AbstractValidator<CreateESewaPaymentDto>
{
    public CreateESewaPaymentDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("OrderId must be greater than zero.");
    }
}