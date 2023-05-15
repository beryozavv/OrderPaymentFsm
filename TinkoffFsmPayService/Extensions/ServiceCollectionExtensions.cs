using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderPaymentFsm.Services;

namespace TinkoffFsmPayService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTinkoffServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IFsmPayService, TinkoffFsmPayService>();
        services.AddTransient<ITinkoffTokenService, TinkoffTokenService>();
        services.AddOptions<TinkoffApiOptions>(nameof(TinkoffApiOptions));

        return services;
    }
}