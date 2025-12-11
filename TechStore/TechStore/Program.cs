using Application.Models;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer; // REQUIRED: Make sure this package is installed
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using TechStore.Endpoints.Categories;
using TechStore.Endpoints.Products;
using TechStore.Extensions;

namespace TechStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Bind "CategoryDb" section -> CategoryDatabaseSettings Class
            builder.Services.Configure<CategoryDatabaseSettings>(
                builder.Configuration.GetSection("CategoryDb"));

            // 2. Bind "ProductDb" section -> ProductDatabaseSettings Class
            builder.Services.Configure<ProductDatabaseSettings>(
                builder.Configuration.GetSection("ProductDb"));


            builder.Services.AddApplicationServices(builder.Configuration);
            builder.Services.AddAuthorization();

            // 2. Configure Versioning AND ApiExplorer (Critical for Swagger)
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                // Combine Header AND QueryString so you can test in browser easily
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("api-version"),
                    new QueryStringApiVersionReader("api-version")
                );
            })
            .AddApiExplorer(options =>
            {
                // Format the version as "'v'major.minor" (e.g., v1.0)
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // 3. Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(); 

            app.UseHttpsRedirection();
            app.UseAuthorization();

            //app.MapCategoryEndpoints();
            app.MapProductEndpoints();

            app.Run();
        }
    }
}