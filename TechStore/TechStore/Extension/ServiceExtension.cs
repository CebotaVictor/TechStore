using Application.Interface;
using Application.Interfaces.Repository;
using Application.Models;
using Application.Services;
using Application.Models.DTO;
using Domain.Entity;
using Infrastructure.ConsumerService;
using MassTransit;

namespace TechStore.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Scoped Services
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();

            // 2. MassTransit (CRITICAL: This must be here for RabbitMQ to work)
            services.AddMassTransit(x => {
                x.AddConsumer<ProductEventsConsumer>();
                x.AddConsumer<CategoryEventsConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    // Default to "rabbitmq" (Docker), fallback to localhost
                    var rabbitHost = configuration["RabbitMq:Host"] ?? "rabbitmq";

                    cfg.UseRawJsonSerializer();

                    cfg.Host(rabbitHost, "/", h => {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.UseRawJsonSerializer(); // Good practice

                    // CRITICAL: This creates a unique queue for THIS specific container
                    // e.g., "product-updates-c3f4a1..."
                    var uniqueQueueName = $"product-updates-{Environment.MachineName}";

                    cfg.ReceiveEndpoint(uniqueQueueName, e =>
                    {
                        e.ConfigureConsumer<ProductEventsConsumer>(context);
                    });

                });
            });

            return services;
        }
    }
}