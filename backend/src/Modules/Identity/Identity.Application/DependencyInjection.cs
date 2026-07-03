using FluentValidation;
using Identity.Application.Users.GetMe;
using Identity.Application.Users.Login;
using Identity.Application.Users.Register;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

/// <summary>Identity.Application modülü için bağımlılık enjeksiyonu yapılandırması.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoginHandler, LoginHandler>();
        services.AddScoped<IRegisterUserHandler, RegisterUserHandler>();
        services.AddScoped<IGetMeHandler, GetMeHandler>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
