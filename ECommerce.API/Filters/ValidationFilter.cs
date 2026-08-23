using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null)
            {
                continue;
            }

            var validatorType =
                typeof(IValidator<>)
                    .MakeGenericType(argument.GetType());

            var validator =
                _serviceProvider.GetService(validatorType);

            if (validator == null)
            {
                continue;
            }

            var validationContextType =
                typeof(ValidationContext<>)
                    .MakeGenericType(argument.GetType());

            var validationContext =
                Activator.CreateInstance(
                    validationContextType,
                    argument);

            var validateAsyncMethod =
                validatorType.GetMethod(
                    "ValidateAsync",
                    new[]
                    {
                        validationContextType,
                        typeof(CancellationToken)
                    });

            if (validateAsyncMethod == null ||
                validationContext == null)
            {
                continue;
            }

            var task =
                (Task)validateAsyncMethod.Invoke(
                    validator,
                    new[]
                    {
                        validationContext,
                        context.HttpContext.RequestAborted
                    })!;

            await task;

            var resultProperty =
                task.GetType().GetProperty("Result");

            var result =
                resultProperty?.GetValue(task);

            if (result == null)
            {
                continue;
            }

            var isValid =
                (bool)result.GetType()
                    .GetProperty("IsValid")!
                    .GetValue(result)!;

            if (isValid)
            {
                continue;
            }

            var errors =
                result.GetType()
                    .GetProperty("Errors")!
                    .GetValue(result);

            context.Result = new BadRequestObjectResult(
                new
                {
                    message = "Validation failed.",
                    errors
                });

            return;
        }

        await next();
    }
}