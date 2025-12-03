using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interface
{
    public interface ICategoryService
    {
        public Task UpdateAsync(string id, Category updatedCategory);
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(string id);
        Task CreateAsync(Category entity);
        Task RemoveAsync(string id);
    }
}
