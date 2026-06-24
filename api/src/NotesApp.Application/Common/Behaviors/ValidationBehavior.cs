using FluentValidation;
using MediatR;

namespace NotesApp.Application.Common.Behaviors;

/// <summary>
/// A MediatR "pipeline behavior" is middleware that wraps EVERY request handler.
/// This one runs FluentValidation before a command/query reaches its handler.
///
/// Why this matters: instead of each handler remembering to validate its input,
/// validation happens automatically in one place. If any rule fails we throw a
/// <see cref="ValidationException"/> and the handler never runs.
///
/// TRequest/TResponse are generics: this single class validates requests of any
/// type, as long as a validator for that type is registered.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // No validator registered for this request? Just continue down the pipe.
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Run every validator and collect all the failures.
        var failures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            // Stops the pipeline. The API layer turns this into HTTP 400.
            throw new ValidationException(failures);
        }

        // 'next()' invokes the actual handler (or the next behavior in line).
        return await next();
    }
}
