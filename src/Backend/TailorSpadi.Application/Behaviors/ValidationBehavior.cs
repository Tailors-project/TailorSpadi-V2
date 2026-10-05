using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace TailorSpadi.Application.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = [];

        // Sequential validation allows future validators to share a scoped DbContext safely.
        foreach (IValidator<TMessage> validator in validators)
        {
            ValidationResult result = await validator.ValidateAsync(message, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(message, cancellationToken);
    }
}
