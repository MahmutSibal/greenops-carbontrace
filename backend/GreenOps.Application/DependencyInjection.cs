using FluentValidation;
using GreenOps.Application.Common;
using GreenOps.Application.EmissionEntries.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GreenOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(CreateEmissionEntryCommand).Assembly;

        services.AddMediatR(assembly);

        services.AddValidatorsFromAssembly(assembly);

        // Validator'lar artık her komutta çalışır (önceden kayıtlı ama hiç tetiklenmiyordu).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
