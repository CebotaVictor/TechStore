using Application.Interface;
using Application.Models;
using Application.Models.DTO.Category;
using Domain.Entity;
using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IMongoCollection<Category> _categoriesCollection;
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly IMongoCollection<Category> _warehouseCollection;
        private readonly IPublishEndpoint _publishEndpoint;

        public CategoryService(IOptions<CategoryDatabaseSettings> storeDatabaseSettings, IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;

            var connectionString = storeDatabaseSettings.Value.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback check if the binding failed
                throw new InvalidOperationException("MongoDB ConnectionString is null. Ensure 'CategoryDb' or 'RecordStreamDatabase' is mapped in Program.cs");
            }

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(storeDatabaseSettings.Value.DatabaseName);

            _categoriesCollection = mongoDatabase.GetCollection<Category>(
                storeDatabaseSettings.Value.CategoriesCollectionName);

            // Note: In a split DB architecture, you might not have access to Products here directly.
            // Assuming we still connect to Product DB via a second connection string or if they share the DB:
            _productsCollection = mongoDatabase.GetCollection<Product>("Products");

            // FIX: Instantiate the settings manually here
            var slaveSettings = new MongoCollectionSettings
            {
                ReadPreference = ReadPreference.SecondaryPreferred
            };

            // Create the warehouse collection handle using the slave settings
            _warehouseCollection = _categoriesCollection.Database
                .GetCollection<Category>(_categoriesCollection.CollectionNamespace.CollectionName, slaveSettings);
        }



        //read from follower(slaves [or niggers]) nodes
        public async Task<IEnumerable<Category>> GetAllAsync()
        {  
            return await _warehouseCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(string id) =>
            await _categoriesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();


        public async Task CreateAsync(Category newCategory)
        {
            // No foreign key check needed here, as Category is the "Parent"

            await _publishEndpoint.Publish(new CategoryCreateEvent
            {
                Id = newCategory.Id,
                Name = newCategory.Name,
                Description = newCategory.Description,
                lastChanged = DateTime.UtcNow
            });


            await _categoriesCollection.InsertOneAsync(newCategory);
        }

        
        public async Task UpdateAsync(string id, Category updatedCategory)
        {
            // Ensure the ID in the object matches the ID in the URL/Parameter
            updatedCategory.Id = id;
            updatedCategory.lastChanged = DateTime.UtcNow;

            await _publishEndpoint.Publish(new CategoryUpdateEvent
            {
                Id = updatedCategory.Id,
                Name = updatedCategory.Name,
                Description = updatedCategory.Description,
                lastChanged = DateTime.UtcNow
            });

            await _categoriesCollection.ReplaceOneAsync(x => x.Id == id, updatedCategory);
        }

        public async Task RemoveAsync(string id)
        {
            // CONSTRAINT CHECK: Are there any products using this category?
            //var hasDependentProducts = await _productsCollection
            //    .Find(p => p.CategoryId == id)
            //    .AnyAsync();

            //if (hasDependentProducts)
            //{
            //    throw new Exception($"Integrity Error: Cannot delete Category '{id}' because it has assigned Products. Delete or reassign the products first.");
            //}

            //await _publishEndpoint.Publish(new CategoryDeleteEvent
            //{
            //    Id = id,
            //});

            //// Only delete if no products are linked
            //await _categoriesCollection.DeleteOneAsync(x => x.Id == id);
        }
    }
}