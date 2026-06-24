using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NotesApp.Application.Common.Behaviors;

namespace NotesApp.Application;

/// <summary>
/// Each layer exposes one extension method that registers its own services into
/// the DI container. The API's Program.cs then just calls
/// builder.Services.AddApplication() — it does not need to know the internals.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Scan THIS assembly and register every IRequestHandler it finds, so we
        // never have to register handlers one by one.
        services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));

        // Register every FluentValidation validator in this assembly.
        services.AddValidatorsFromAssembly(assembly);

        // Plug the validation behavior into the MediatR pipeline so it runs for
        // every request before the handler executes.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
