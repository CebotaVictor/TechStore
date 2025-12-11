using Application.Models;
using Application.Models.DTO.Product;
using Domain.Entity;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.ConsumerService
{
    public class ProductEventsConsumer :
        IConsumer<ProductCreatedEvent>,
        IConsumer<ProductUpdateEvent>,
        IConsumer<ProductDeleteEvent>
    {
        private readonly IMongoCollection<Product> _localCollection;
        private readonly ILogger<ProductEventsConsumer> _logger;

        public ProductEventsConsumer(
            IOptions<ProductDatabaseSettings> settings,
            ILogger<ProductEventsConsumer> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.Value.ConnectionString);
            var db = client.GetDatabase(settings.Value.DatabaseName);

            // Connect to the Local Product Collection
            _localCollection = db.GetCollection<Product>(settings.Value.ProductsCollectionName);
        }

        // SYNC: Create
        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var msg = context.Message;

            // Idempotency Check: Do we already have it?
            var exists = await _localCollection.Find(x => x.Id == msg.Id).AnyAsync();
            if (exists) return;

            try
            {
                await _localCollection.InsertOneAsync(new Product
                {
                    Id = msg.Id,
                    Name = msg.Name,
                    Price = msg.Price,
                    lastChanged = msg.lastChanged
                });
                _logger.LogInformation($"[Sync] Created Product {msg.Id}");
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Ignore duplicates
            }
        }

        // SYNC: Update
        public async Task Consume(ConsumeContext<ProductUpdateEvent> context)
        {
            var msg = context.Message;

            var updateDef = Builders<Product>.Update
                .Set(p => p.Name, msg.Name)
                .Set(p => p.Price, msg.Price)
                .Set(p => p.lastChanged, msg.lastChanged);

            await _localCollection.UpdateOneAsync(
                p => p.Id == msg.Id,
                updateDef,
                new UpdateOptions { IsUpsert = true }); // Upsert ensures we have the data
        }

        // SYNC: Delete
        public async Task Consume(ConsumeContext<ProductDeleteEvent> context)
        {
            await _localCollection.DeleteOneAsync(p => p.Id == context.Message.Id);
            _logger.LogInformation($"[Sync] Deleted Product {context.Message.Id}");
        }
    }
}