
using Microsoft.AspNetCore.Mvc;
using FastFood.Models;
using Microsoft.Data.SqlClient;
using Dapper;
namespace FastFood.Controllers
{
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    public HomeController(ILogger<HomeController> logger,IConfiguration configuration)
    {
        _logger = logger;
        _configuration=configuration;
    }

    public async Task<IActionResult> Index()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using (var connection = new SqlConnection(connectionString))
        {
            // Fetch only 3 items to show as "Today's Specials"
            var featuredItems = await connection.QueryAsync<Product>(
                "SELECT TOP 3 * FROM Products ORDER BY Price DESC");
            return View(featuredItems);
        }
    }
    public IActionResult Intro()
    {
        return View();
    }
    public IActionResult StoreLocator()
    {
        return View();
    }

    public IActionResult AboutUs()
     {
        return View();
    }

    public IActionResult ContactUs()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }

    // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // public IActionResult Error()
    // {
    //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    // }
}
}