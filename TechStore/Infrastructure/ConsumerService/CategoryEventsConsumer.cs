using Application.Models;
using Application.Models.DTO.Category;
using Domain.Entity;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerService
{
    public class CategoryEventsConsumer :
    IConsumer<CategoryCreateEvent>,
    IConsumer<CategoryUpdateEvent>,
    IConsumer<CategoryDeleteEvent>
    {
        private readonly IMongoCollection<Category> _localCollection;
        private readonly ILogger<CategoryEventsConsumer> _logger; // Add Logger

        public CategoryEventsConsumer(
            IOptions<CategoryDatabaseSettings> settings,
            ILogger<CategoryEventsConsumer> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.Value.ConnectionString);
            var db = client.GetDatabase(settings.Value.DatabaseName);
            _localCollection = db.GetCollection<Category>(settings.Value.CategoriesCollectionName);
        }

        // FIX: Handle Creation with Idempotency check
        public async Task Consume(ConsumeContext<CategoryCreateEvent> context)
        {
            var msg = context.Message;

            // 1. CHECK: Does this ID already exist?
            var existingCategory = await _localCollection
                .Find(x => x.Id == msg.Id)
                .FirstOrDefaultAsync();

            if (existingCategory != null)
            {
                // It exists! This means another node (or me) already saved it.
                // We do nothing and return successfully to remove the message from Queue.
                _logger.LogInformation($"[Sync] Category {msg.Id} already exists. Skipping insert.");
                return;
            }

            // 2. It doesn't exist, so we insert it.
            try
            {
                await _localCollection.InsertOneAsync(new Category
                {
                    Id = msg.Id,
                    Name = msg.Name,
                    Description = msg.Description,
                    lastChanged = msg.lastChanged
                });
                _logger.LogInformation($"[Sync] Inserted Category {msg.Id} from Broadcast.");
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Race condition: Someone inserted it while we were checking. 
                // This is fine, ignore the error.
                _logger.LogWarning($"[Sync] Duplicate Key caught for {msg.Id}. Ignored.");
            }
        }

        // Handle Updates (Use Upsert to be safe)
        public async Task Consume(ConsumeContext<CategoryUpdateEvent> context)
        {
            var msg = context.Message;
            var updateDef = Builders<Category>.Update
                .Set(c => c.Name, msg.Name)
                .Set(c => c.Description, msg.Description)
                .Set(c => c.lastChanged, msg.lastChanged);

            // Update if exists, or Insert if missing (Upsert)
            await _localCollection.UpdateOneAsync(
                c => c.Id == msg.Id,
                updateDef,
                new UpdateOptions { IsUpsert = true });
        }

        // Handle Deletion
        public async Task Consume(ConsumeContext<CategoryDeleteEvent> context)
        {
            await _localCollection.DeleteOneAsync(c => c.Id == context.Message.Id);
        }
    }
}
