using Dapper;
using FastFood.Data;
using FastFood.Interfaces;
using FastFood.Models;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastFood.Repository
{
    public class MenuRepository : IMenuRepository
    {
        private readonly DapperContext _context;

        public MenuRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            const string query = "SELECT * FROM Products";
            using (var connection = _context.CreateConnection())
            {
                return await connection.QueryAsync<Product>(query);
            }
        }

        public async Task<IEnumerable<Product>> GetDealsAsync()
        {
            const string query = "SELECT * FROM Products WHERE IsOnSale = 1";
            using (var connection = _context.CreateConnection())
            {
                return await connection.QueryAsync<Product>(query);
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            const string query = "SELECT * FROM Products WHERE Id = @Id";
            using (var connection = _context.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<Product>(query, new { Id = id });
            }
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            const string query = "SELECT * FROM Products WHERE Category = @Category";
            using (var connection = _context.CreateConnection())
            {
                return await connection.QueryAsync<Product>(query, new { Category = category });
            }
        }

        // Add this implementation to your MenuRepository for SQLINjection
        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
        // We use @Term to protect against SQL Injection
        const string sql = "SELECT * FROM Products WHERE Name LIKE @Term OR Description LIKE @Term";
    
        // We wrap the search term in % for the SQL LIKE operator
        var formattedSearch = $"%{searchTerm}%";

         using (IDbConnection db = _context.CreateConnection())
         {
            // Dapper safely handles the mapping
            return await db.QueryAsync<Product>(sql, new { Term = formattedSearch });
         }
        }
    }
}