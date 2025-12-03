using Application.Interface;
using Application.Interfaces.Repository;
using Application.Models;
using Domain.Entity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class ProductService : IProductService
    
{
    private readonly IMongoCollection<Product> _productsCollection;
    // We need the Category collection to enforce the Foreign Key constraint
    private readonly IMongoCollection<Category> _categoriesCollection;

    public ProductService(IOptions<StoreDatabaseSettings> storeDatabaseSettings)
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

        _productsCollection = mongoDatabase.GetCollection<Product>(
            storeDatabaseSettings.Value.ProductsCollectionName);

        _categoriesCollection = mongoDatabase.GetCollection<Category>(
            storeDatabaseSettings.Value.CategoriesCollectionName);
    }

   
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        // Define settings to prefer reading from Slave nodes
        var slaveSettings = new MongoCollectionSettings
        {
            ReadPreference = ReadPreference.SecondaryPreferred
        };

        // Create a temporary collection handle that points to the slaves
        var warehouseCollection = _productsCollection.Database
            .GetCollection<Product>(_productsCollection.CollectionNamespace.CollectionName, slaveSettings);

        return await warehouseCollection.Find(_ => true).ToListAsync();
    }


    
    public async Task<Product?> GetByIdAsync(string id) =>
        await _productsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    
    public async Task CreateAsync(Product newProduct)
    {
        // CONSTRAINT CHECK: Does this Category actually exist?
        var categoryExists = await _categoriesCollection
            .Find(c => c.Id == newProduct.CategoryId)
            .AnyAsync();

        if (!categoryExists)
        {
            throw new Exception($"Integrity Error: Category with ID '{newProduct.CategoryId}' does not exist.");
        }

        // If valid, write to Master
        await _productsCollection.InsertOneAsync(newProduct);
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(string categoryId)
    {
        // Simple filter: Find all products where the CategoryId matches the input
        return await _productsCollection
            .Find(p => p.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task UpdateAsync(string id, Product updatedProduct)
    {
        // CONSTRAINT CHECK: Check validity again, as the user might be changing the category
        var categoryExists = await _categoriesCollection
            .Find(c => c.Id == updatedProduct.CategoryId)
            .AnyAsync();

        if (!categoryExists)
        {
            throw new Exception($"Integrity Error: Category with ID '{updatedProduct.CategoryId}' does not exist.");
        }

        await _productsCollection.ReplaceOneAsync(x => x.Id == id, updatedProduct);
    }


    public async Task RemoveAsync(string id) =>
        await _productsCollection.DeleteOneAsync(x => x.Id == id);

}