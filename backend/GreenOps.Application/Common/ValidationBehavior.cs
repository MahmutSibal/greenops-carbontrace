using System.Reflection;
using FluentValidation;
using MediatR;

namespace GreenOps.Application.Common;

/// <summary>
/// MediatR komutlarını handler'a ulaşmadan FluentValidation ile doğrular.
/// Yanıt tipi Result/Result&lt;T&gt; ise hata istisna yerine Result.Failure olarak döner.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count == 0)
            return await next();

        var message = string.Join(" ", failures.Select(f => f.ErrorMessage).Distinct());

        var failureMethod = typeof(TResponse).GetMethod(
            nameof(Result.Failure), BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
        if (failureMethod is not null && typeof(Result).IsAssignableFrom(typeof(TResponse)))
            return (TResponse)failureMethod.Invoke(null, new object[] { message })!;

        throw new ValidationException(failures);
    }
}
