using Application.Interface;
using Application.Interfaces.Repository;
using Application.Models;
using Domain.Entity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace TechStore.Endpoints.Products
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapGet("/products", async (IProductService service) =>
            {
                try
                {
                    var products = await service.GetAllAsync();
                    return Results.Ok(products);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapGet("/products/{id}", async (string id, [FromServices] IProductService service) =>
            {
                try
                {
                    var product = await service.GetByIdAsync(id);
                    if (product is null)
                    {
                        return Results.NotFound();
                    }
                    return Results.Ok(product);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            //routeBuilder.MapGet("/products/category/{categoryId}", async (string categoryId, IProductService service) =>
            //{
            //    try
            //    {
            //        var products = await service.GetProductsByCategoryIdAsync(categoryId);
            //        return Results.Ok(products);
            //    }
            //    catch (Exception ex)
            //    {
            //        return Results.Problem(ex.Message);
            //    }
            //}).WithOpenApi();

            routeBuilder.MapPost("/products", async (ProductDTO newProduct, IProductService service) =>
            {
                try
                {

                    var product = new Product
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Name = newProduct.Name,
                        Price = newProduct.Price,
                    };

                    await service.CreateAsync(product);
                    return Results.Created($"/products/{product.Id}", product);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapPut("/products/{id}", async (string id, Product updatedProduct, IProductService prodService) =>
            {
                try
                {
                    var product = await prodService.GetByIdAsync(id);
                    if (product is null)
                    {
                        return Results.NotFound();
                    }
                    await prodService.UpdateAsync(id, updatedProduct);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapDelete("/products/{id}", async (string id, IProductService service) =>
            {
                try
                {
                    var product = await service.GetByIdAsync(id);
                    if (product is null)
                    {
                        return Results.NotFound();
                    }
                    await service.RemoveAsync(id);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();
        }
    }
}