using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FastFood.Models;
using FastFood.Data;
namespace FastFood.Controllers
{

public class AdminController : Controller
{
    private readonly string? _connectionString;
    public AdminController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    public IActionResult AddProduct()
{
    return View(); // This looks for AddProduct.cshtml
}
    public async Task<IActionResult> Edit(string id)
    {
        using var conn = new SqlConnection(_connectionString);
        var product = await conn.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE Id = @id", new { id });
        return View(product);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(Product p)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = "UPDATE Products SET Name=@Name, Description=@Description, Price=@Price, Category=@Category, ImageUrl=@ImageUrl WHERE Id=@Id";
        await conn.ExecuteAsync(sql, p);
        return RedirectToAction("ManageMenu");
    }
   // 1. This shows the "Are you sure?" page
[HttpGet]
public async Task<IActionResult> Delete(string id)
{
    using var conn = new SqlConnection(_connectionString);
    var product = await conn.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE Id = @id", new { id });
    
    if (product == null) return NotFound();
    
    return View(product);
}

// 2. This handles the actual deletion after clicking "Confirm"
[HttpPost, ActionName("Delete")]
public async Task<IActionResult> DeleteConfirmed(string id)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.ExecuteAsync("DELETE FROM Products WHERE Id = @id", new { id });
    return RedirectToAction("ManageMenu");
}

// GET: Admin/ManageMenu
        public async Task<IActionResult> ManageMenu()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
        // If not admin, send them to login or an Access Denied page
                return RedirectToAction("Login", "Account");
            }
            using (var connection = new SqlConnection(_connectionString))
            {
                // Fetch all products to display in the management table
                var sql = "SELECT * FROM Products ORDER BY Category, Name";
                var products = await connection.QueryAsync<Product>(sql);
                
                return View(products);
            }
        }
public IActionResult OrderStatistics()
{
    var dates = new List<string?>();
    var counts = new List<int>();

    using (SqlConnection con = new SqlConnection(_connectionString))
    {
        string query = @"
            SELECT CAST(OrderDate AS DATE) AS OrderDate, COUNT(*) AS TotalOrders
            FROM Orders    
            
            GROUP BY CAST(OrderDate AS DATE)
             ORDER BY OrderDate";

        SqlCommand cmd = new SqlCommand(query, con);
        con.Open();

        SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0)) continue;
            else
            {
                dates.Add(reader["OrderDate"].ToString());
                 counts.Add(Convert.ToInt32(reader["TotalOrders"]));
            }
        }
    }

    ViewBag.Dates = dates;
    ViewBag.Counts = counts;

    return View();
}


[HttpPost]
public async Task<IActionResult> AddProduct(Product product, IFormFile? imageFile)
{
    if (imageFile != null && imageFile.Length > 0)
    {
        // 1. Save the image to wwwroot/images
        var fileName = Path.GetFileName(imageFile.FileName);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        // 2. Set the ImageUrl property for the database
        product.ImageUrl = "/images/" + fileName;
    }

    using (var connection = new SqlConnection(_connectionString))
    {
        var sql = "INSERT INTO Products (Name, Category, Price, ImageUrl) VALUES (@Name, @Category, @Price, @ImageUrl)";
        await connection.ExecuteAsync(sql, product);
    }

    return RedirectToAction("ManageMenu");
}
}
}