using Application.Interface;
using Application.Interfaces.Repository;
using Application.Models;
using Application.Services;
using Domain.Entity;

namespace TechStore.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure database settings from appsettings.json
            services.Configure<StoreDatabaseSettings>(
                configuration.GetSection("StoreDatabase"));

            // Register ProductService for its interfaces
            services.AddSingleton<ProductService>();
            services.AddSingleton<IProductService>(sp => sp.GetRequiredService<ProductService>());

            // Register CategoryService for its interfaces
            services.AddSingleton<CategoryService>();
            services.AddSingleton<ICategoryService>(sp => sp.GetRequiredService<CategoryService>());

            return services;
        }
    }
}