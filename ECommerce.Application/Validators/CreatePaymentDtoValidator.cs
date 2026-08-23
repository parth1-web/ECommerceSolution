using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Enums;
using FluentValidation;

namespace ECommerce.Application.Validators;

public class CreatePaymentDtoValidator
    : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("OrderId must be greater than zero.");

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("Invalid payment method.");
    }
}