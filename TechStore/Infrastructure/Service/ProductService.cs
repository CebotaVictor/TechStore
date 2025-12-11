using Application.Interface;
using Application.Interfaces.Repository;
using Application.Models;
using Application.Models.DTO.Product;
using Domain.Entity;
using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IMongoCollection<Product> _productsCollection;   // Master (Writes)
        private readonly IMongoCollection<Product> _warehouseCollection;  // Slaves (Reads)
        private readonly IPublishEndpoint _publishEndpoint;

        public ProductService(IOptions<ProductDatabaseSettings> settings, IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
            var dbSettings = settings.Value;

            if (string.IsNullOrEmpty(dbSettings.ConnectionString))
            {
                throw new InvalidOperationException("ProductDb ConnectionString is null.");
            }

            var mongoClient = new MongoClient(dbSettings.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.DatabaseName);

            // 1. Master Connection (Writes)
            _productsCollection = mongoDatabase.GetCollection<Product>(
                dbSettings.ProductsCollectionName);

            // 2. Warehouse Connection (Reads - Secondary Preferred)
            var slaveSettings = new MongoCollectionSettings
            {
                ReadPreference = ReadPreference.SecondaryPreferred
            };

            _warehouseCollection = _productsCollection.Database
                .GetCollection<Product>(dbSettings.ProductsCollectionName, slaveSettings);
        }

        // READ: From Slave
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _warehouseCollection.Find(_ => true).ToListAsync();
        }

        // READ: From Master (Consistency)
        public async Task<Product?> GetByIdAsync(string id) =>
            await _productsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();


        // WRITE: Create + Broadcast
        public async Task CreateAsync(Product newProduct)
        {
            // 1. Publish to RabbitMQ (Notify other nodes)
            await _publishEndpoint.Publish(new ProductCreatedEvent
            {
                Id = newProduct.Id,
                Name = newProduct.Name,
                Price = newProduct.Price,
                lastChanged = DateTime.UtcNow
            });

            // 2. Write to Local Master
            await _productsCollection.InsertOneAsync(newProduct);
        }

        // WRITE: Update + Broadcast
        public async Task UpdateAsync(string id, Product updatedProduct)
        {
            updatedProduct.Id = id;

            // 1. Publish to RabbitMQ
            await _publishEndpoint.Publish(new ProductUpdateEvent
            {
                Id = updatedProduct.Id,
                Name = updatedProduct.Name,
                Price = updatedProduct.Price,
                lastChanged = DateTime.UtcNow
            });

            // 2. Write to Local Master
            await _productsCollection.ReplaceOneAsync(x => x.Id == id, updatedProduct);
        }

        // WRITE: Delete + Broadcast
        public async Task RemoveAsync(string id)
        {
            // 1. Publish to RabbitMQ
            await _publishEndpoint.Publish(new ProductDeleteEvent
            {
                Id = id,
            });

            // 2. Delete from Local Master
            await _productsCollection.DeleteOneAsync(x => x.Id == id);
        }
    }
}