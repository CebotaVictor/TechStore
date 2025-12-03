

using Domain.Entity;

namespace Application.Interfaces.Repository
{
    public interface IProductService
    {
        public Task UpdateAsync(string id, Product updatedProduct);
        public Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(string categoryId);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(string id);
        Task CreateAsync(Product entity);
        Task RemoveAsync(string id);
    }
}
