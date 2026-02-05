using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration; // Fixes IConfiguration warning
using Microsoft.Data;
using System.Data;

namespace FastFood.Data
{
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}