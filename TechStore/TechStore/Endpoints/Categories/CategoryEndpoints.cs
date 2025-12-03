using Application.Interface;
using Application.Models;
using Domain.Entity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace TechStore.Endpoints.Categories
{
    public static class CategoryEndpoints
    {
        public static void MapCategoryEndpoints(this IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapGet("/categories", async (ICategoryService service) =>
            {
                try
                {
                    var categories = await service.GetAllAsync();
                    return Results.Ok(categories);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapGet("/categories/{id}", async (string id, ICategoryService service) =>
            {
                try
                {
                    var category = await service.GetByIdAsync(id);
                    if (category is null)
                    {
                        return Results.NotFound();
                    }
                    return Results.Ok(category);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapPost("/categories", async (CategoryDTO newCategory, ICategoryService service) =>
            {
                try
                {
                    var category = new Category
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Name = newCategory.Name,
                        Description = newCategory.Description
                    };
                    await service.CreateAsync(category);
                    return Results.Created($"/categories/{category.Id}", category);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapPut("/categories/{id}", async (string id, CategoryDTO updatedCategory,ICategoryService catService) =>
            {
                try
                {
                    var category = await catService.GetByIdAsync(id);
                    if (category is null)
                    {
                        return Results.NotFound();
                    }

                    var categoryToUpdate = new Category
                    {
                        Id = id,
                        Name = updatedCategory.Name,
                        Description = updatedCategory.Description
                    };  

                    await catService.UpdateAsync(id, categoryToUpdate);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }).WithOpenApi();

            routeBuilder.MapDelete("/categories/{id}", async (string id, ICategoryService service) =>
            {
                try
                {
                    var category = await service.GetByIdAsync(id);
                    if (category is null)
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