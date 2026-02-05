using FastFood.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastFood.Interfaces
{
    public interface IMenuRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetDealsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        // Add this to your interface
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
    }
}