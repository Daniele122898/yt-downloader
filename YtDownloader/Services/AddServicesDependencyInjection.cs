using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YtDownloader.Services
{
    public static class AddServicesDependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configs)
            => services
                .AddScoped<DownloaderService>()
                .AddSingleton<CacheService>();
    }
}