using Application.Interface;
using Application.Models;
using Domain.Entity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IMongoCollection<Category> _categoriesCollection;
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly MongoCollectionSettings _settings = new();
        private readonly IMongoCollection<Category> _warehouseCollection;

        public CategoryService(IOptions<StoreDatabaseSettings> storeDatabaseSettings, MongoCollectionSettings settings, IMongoCollection<Category> warehouseCollection)
        {
            var mongoClient = new MongoClient(
                storeDatabaseSettings.Value.ConnectionString);

            var connectionString = storeDatabaseSettings.Value.ConnectionString;

            if (storeDatabaseSettings.Value == null || string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB ConnectionString is null. Check appsettings.json or Docker environment variables.");
            }

            var mongoDatabase = mongoClient.GetDatabase(
                storeDatabaseSettings.Value.DatabaseName);

            _categoriesCollection = mongoDatabase.GetCollection<Category>(
                storeDatabaseSettings.Value.CategoriesCollectionName);

            _productsCollection = mongoDatabase.GetCollection<Product>(
                storeDatabaseSettings.Value.ProductsCollectionName);
            _settings = settings;



            var slaveSettings = new MongoCollectionSettings
            {
                ReadPreference = ReadPreference.SecondaryPreferred
            };

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
            await _categoriesCollection.InsertOneAsync(newCategory);
        }

        
        public async Task UpdateAsync(string id, Category updatedCategory)
        {
            // Ensure the ID in the object matches the ID in the URL/Parameter
            updatedCategory.Id = id;

            await _categoriesCollection.ReplaceOneAsync(x => x.Id == id, updatedCategory);
        }

        public async Task RemoveAsync(string id)
        {
            // CONSTRAINT CHECK: Are there any products using this category?
            var hasDependentProducts = await _productsCollection
                .Find(p => p.CategoryId == id)
                .AnyAsync();

            if (hasDependentProducts)
            {
                throw new Exception($"Integrity Error: Cannot delete Category '{id}' because it has assigned Products. Delete or reassign the products first.");
            }
           
            // Only delete if no products are linked
            await _categoriesCollection.DeleteOneAsync(x => x.Id == id);
        }
    }
}