using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FastFood.Models;
using FastFood.Interfaces;
using FastFood.Data;
namespace FastFood.Controllers
{
    public class FrontendController : Controller
    {
        private readonly IMenuRepository _menuRepository;
        private readonly string _connectionString;

        public FrontendController(IMenuRepository menuRepository,IConfiguration configuration)
        {
            _menuRepository = menuRepository;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }
public async Task<IActionResult> Index(string? searchTerm)
{
    IEnumerable<Product> products;

    if (!string.IsNullOrEmpty(searchTerm))
    {
        products = await _menuRepository.SearchProductsAsync(searchTerm);
        ViewBag.CurrentSearch = searchTerm; 
    }
    else
    {
        products = await _menuRepository.GetAllProductsAsync();
    }
    return View(products);
}
        
public async Task<IActionResult> Details(string id)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        var product = await connection.QueryFirstOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE Id = @id", new { id = id });
        
        if (product == null) return NotFound();
        return View(product);
    }
}
[HttpPost]
public async Task<IActionResult> Reorder(int orderId)
{
    var userEmail = HttpContext.Session.GetString("UserEmail");

    if (string.IsNullOrEmpty(userEmail))
        return RedirectToAction("Login", "Account");

    using (var connection = new SqlConnection(_connectionString))
    {
        // 1. Get items from previous order
        var items = await connection.QueryAsync<dynamic>(
            @"SELECT ProductId, Quantity 
              FROM OrderDetails 
              WHERE OrderId = @orderId",
            new { orderId });

        // 2. Add each item back to cart
        foreach (var item in items)
        {
            var existing = await connection.QueryFirstOrDefaultAsync<CartItem>(
                @"SELECT * FROM CartItems 
                  WHERE ProductId = @productId AND UserEmail = @userEmail",
                new { productId = (string)item.ProductId, userEmail });

            if (existing != null)
            {
                await connection.ExecuteAsync(
                    @"UPDATE CartItems 
                      SET Quantity = Quantity + @qty 
                      WHERE CartItemId = @id",
                    new { qty = (int)item.Quantity, id = existing.CartItemId });
            }
            else
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO CartItems (ProductId, UserEmail, Quantity) 
                      VALUES (@productId, @userEmail, @qty)",
                    new
                    {
                        productId = (string)item.ProductId,
                        userEmail,
                        qty = (int)item.Quantity
                    });
            }
        }
    }

    return RedirectToAction("Cart");
}


public async Task<IActionResult> AddToCart(string productId)
{
    var userEmail = HttpContext.Session.GetString("UserEmail");

    if (string.IsNullOrEmpty(userEmail)) 
    {
        // If not logged in, send them to Login instead of just "Unauthorized"
        return RedirectToAction("Login", "Account"); 
    }

    using (var connection = new SqlConnection(_connectionString))
    {
        // 1. Explicitly use <CartItem> to avoid dynamic mapping issues
        var existing = await connection.QueryFirstOrDefaultAsync<CartItem>(
            "SELECT * FROM CartItems WHERE ProductId = @productId AND UserEmail = @userEmail",
            new { productId, userEmail });

        if (existing != null) 
        {
            // 2. Use the ID from the model. 
            // Note: If CartItemId is a string in DB, this works perfectly now.
            await connection.ExecuteAsync(
                "UPDATE CartItems SET Quantity = Quantity + 1 WHERE CartItemId = @Id", 
                new { Id = existing.CartItemId });
        } 
        else 
        {
            // 3. New item - Quantity starts at 1
            await connection.ExecuteAsync(
                "INSERT INTO CartItems (ProductId, UserEmail, Quantity) VALUES (@productId, @userEmail, 1)", 
                new { productId, userEmail });
        }
    }

    // 4. Now the user is physically moved to the Cart page
    return RedirectToAction("Cart");
}
// 2. VIEW CART
public async Task<IActionResult> Cart()
{
    var userEmail = HttpContext.Session.GetString("UserEmail");

    if (string.IsNullOrEmpty(userEmail)) 
    {
        return RedirectToAction("Login", "Account"); 
    }

    using (var connection = new SqlConnection(_connectionString))
    {
        // Use Multi-Mapping to join CartItems and Products into the CartItem model
        var sql = @"SELECT c.*, p.* FROM CartItems c 
                    JOIN Products p ON c.ProductId = p.Id 
                    WHERE c.UserEmail = @userEmail";

        var cartItems = await connection.QueryAsync<CartItem, Product, CartItem>(
            sql, 
            (cartItem, product) => {
                cartItem.Product = product; // Link the product to the cart item
                return cartItem;
            },
            new { userEmail },
            splitOn: "Id" // This tells Dapper where the Product columns start
        );

        return View(cartItems); // Now returns IEnumerable<CartItem> instead of dynamic
    }
}
public IActionResult TrackOrder()
    {
        return View();
    }
[HttpPost]
public async Task<IActionResult> Checkout()
{
    var userEmail = HttpContext.Session.GetString("UserEmail");

    if (string.IsNullOrEmpty(userEmail)) 
    {
        return RedirectToAction("Login", "Account"); 
    }

    decimal deliveryCharge = 0.3m; 

    using (var db = new SqlConnection(_connectionString))
    {
        await db.OpenAsync();
        using (var transaction = db.BeginTransaction())
        {
            try
            {
                var cartSql = @"SELECT c.*, p.Price 
                                FROM CartItems c 
                                JOIN Products p ON c.ProductId = p.Id 
                                WHERE c.UserEmail = @userEmail";
                
                var cartItems = (await db.QueryAsync<dynamic>(cartSql, new { userEmail }, transaction)).ToList();

                if (!cartItems.Any()) return RedirectToAction("Cart");

                decimal subtotal = cartItems.Sum(x => (decimal)x.Quantity * (decimal)x.Price);
                decimal totalAmount = subtotal + deliveryCharge;

                var orderSql = @"INSERT INTO Orders (UserEmail, TotalAmount, OrderDate, Status) 
                                 VALUES (@userEmail, @totalAmount, GETDATE(), 'Pending');
                                 SELECT CAST(SCOPE_IDENTITY() as int);";
                
                int orderId = await db.ExecuteScalarAsync<int>(orderSql, new { userEmail, totalAmount }, transaction);

                foreach (var item in cartItems)
                {
                    await db.ExecuteAsync(@"INSERT INTO OrderDetails (OrderId, ProductId, Quantity, PriceAtTime) 
                                           VALUES (@orderId, @ProductId, @Quantity, @Price)", 
                                           new { 
                                               orderId, 
                                               ProductId = (string)item.ProductId, // Ensure string type
                                               Quantity = (int)item.Quantity, 
                                               Price = (decimal)item.Price 
                                           }, transaction);
                }

                await db.ExecuteAsync("DELETE FROM CartItems WHERE UserEmail = @userEmail", new { userEmail }, transaction);

                transaction.Commit();

                return RedirectToAction("OrderSuccess", new { id = orderId });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine(ex.Message);
                return View("Error");
            }
        }
    }
}


// public async Task<IActionResult> Cart()
// {
//     // 1. We get the 'userId' string from Identity
//     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 

//     if (userId == null)
//     {
//         // For a 'Cart' page, it's friendlier to redirect to Login than to show 'Unauthorized'
//         return RedirectToPage("/Account/Login", new { area = "Identity" });
//     }

//     using (var connection = new SqlConnection(_connectionString))
//     {
//         // 2. Change 'UserEmail' to 'UserId' in the WHERE clause
//         var sql = @"SELECT c.*, p.Name, p.Price, p.ImageUrl 
//                     FROM CartItems c 
//                     JOIN Products p ON c.ProductId = p.Id 
//                     WHERE c.UserId = @userId"; // <-- Match the DB column

//         // 3. Pass 'userId' into the Dapper parameter object
//         var cartItems = await connection.QueryAsync<dynamic>(sql, new { userId });
        
//         return View(cartItems);
//     }
// }
// [HttpPost]
// public async Task<IActionResult> AddToCart(int productId)
// {
//     // 1. Get the ID from Identity
//     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 

//     if (userId == null)
//     {
//         // If it's an AJAX call, 401 Unauthorized is correct. 
//         // Your JavaScript 'error' function can then redirect to Login.
//         return Unauthorized(); 
//     }

//     using (var connection = new SqlConnection(_connectionString))
//     {
//         // 2. Updated SQL: Changed 'UserEmail' to 'UserId'
//         // Updated Parameters: Changed 'userEmail' to 'userId'
//         var existing = await connection.QueryFirstOrDefaultAsync(
//             "SELECT * FROM CartItems WHERE ProductId = @productId AND UserId = @userId",
//             new { productId, userId });

//         if (existing != null) 
//         {
//             await connection.ExecuteAsync(
//                 "UPDATE CartItems SET Quantity = Quantity + 1 WHERE CartItemId = @Id", 
//                 new { Id = existing.CartItemId });
//         } 
//         else 
//         {
//             // 3. Updated SQL: Insert into 'UserId' column
//             await connection.ExecuteAsync(
//                 "INSERT INTO CartItems (ProductId, UserId, Quantity) VALUES (@productId, @userId, 1)", 
//                 new { productId, userId });
//         }
//     }
//     return Json(new { success = true });
// }     
[HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.ExecuteAsync("DELETE FROM CartItems WHERE CartItemId = @cartItemId", new { cartItemId });
        }
        return RedirectToAction("Cart");
        }

// [HttpPost]
// public async Task<IActionResult> Checkout()
// {
//    // var userEmail = HttpContext.Session.GetString("UserEmail");
//     if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

//     // Use a unique name like 'db' or 'conn' if 'connection' is already used elsewhere
//     using (var db = new SqlConnection(_connectionString)) 
//     {
//         await db.OpenAsync();
//         using (var transaction = db.BeginTransaction())
//         {
//             try
//             {
//                 // 1. Get current cart items
//                 // Rename this string to 'getCartSql' to avoid conflict with 'sql'
//                 var getCartSql = @"SELECT c.*, p.Price FROM CartItems c 
//                                    JOIN Products p ON c.ProductId = p.Id 
//                                    WHERE c.UserEmail = @userEmail";
                
//                 var cartItems = (await db.QueryAsync<dynamic>(getCartSql, new { userEmail }, transaction)).ToList();

//                 if (!cartItems.Any()) return RedirectToAction("Cart");

//                 decimal totalAmount = cartItems.Sum(item => (decimal)item.Price * (int)item.Quantity);

//                 // 2. Create the Order
//                 var insertOrderSql = @"INSERT INTO Orders (UserEmail, TotalAmount, OrderDate, Status) 
//                                        VALUES (@userEmail, @totalAmount, GETDATE(), 'Pending');
//                                        SELECT CAST(SCOPE_IDENTITY() as int);";
                
//                 int orderId = await db.ExecuteScalarAsync<int>(insertOrderSql, new { userEmail, totalAmount }, transaction);

//                 // 3. Move items to details
//                 foreach (var item in cartItems)
//                 {
//                     var insertDetailSql = @"INSERT INTO OrderDetails (OrderId, ProductId, Quantity, PriceAtTime) 
//                                            VALUES (@orderId, @ProductId, @Quantity, @Price)";
                    
//                     await db.ExecuteAsync(insertDetailSql, new { 
//                         orderId, 
//                         item.ProductId, 
//                         item.Quantity, 
//                         Price = item.Price 
//                     }, transaction);
//                 }

//                 // 4. Clear Cart
//                 await db.ExecuteAsync("DELETE FROM CartItems WHERE UserEmail = @userEmail", new { userEmail }, transaction);

//                 transaction.Commit();
//                 ViewBag.Subtotal = subtotal;
//                 ViewBag.Delivery = deliveryCharge;
//                 ViewBag.Total = total;
//                 return RedirectToAction("OrderSuccess", new { id = orderId });
//             }
//             catch (Exception)
//             {
//                 transaction.Rollback();
//                 return View("Error");
//             }
//         }
//     }
// }
public async Task<IActionResult> OrderHistory()
{
    // 1. Get the current logged-in user's Email from Session
    var userEmail = HttpContext.Session.GetString("UserEmail");

    if (string.IsNullOrEmpty(userEmail)) 
    {
        return RedirectToAction("Login", "Account"); 
    }

    using (var connection = new SqlConnection(_connectionString))
    {
        // 2. Updated SQL: Filter by UserEmail (since we removed UserId)
        // We order by Date DESC so the newest orders appear at the top
        var sql = "SELECT * FROM Orders WHERE UserEmail = @userEmail ORDER BY OrderDate DESC";
        
        // 3. Pass the userEmail variable to Dapper
        var orders = await connection.QueryAsync<dynamic>(sql, new { userEmail });
        
        return View(orders);
    }
}
[HttpPost]
public async Task<IActionResult> UpdateQuantity(int cartItemId, int change)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        // 1. Get current quantity
        var currentQty = await connection.ExecuteScalarAsync<int>(
            "SELECT Quantity FROM CartItems WHERE CartItemId = @cartItemId", 
            new { cartItemId });

        int newQty = currentQty + change;

        if (newQty <= 0)
        {
            // 2. If quantity becomes 0, remove the item
            await connection.ExecuteAsync(
                "DELETE FROM CartItems WHERE CartItemId = @cartItemId", 
                new { cartItemId });
        }
        else
        {
            // 3. Otherwise, update the quantity
            await connection.ExecuteAsync(
                "UPDATE CartItems SET Quantity = @newQty WHERE CartItemId = @cartItemId", 
                new { newQty, cartItemId });
        }
    }
    return RedirectToAction("Cart");
}
public IActionResult OrderSuccess(int id) // Use 'int' because SCOPE_IDENTITY returned an int
{
    // You can pass the ID to the view via ViewBag or a ViewModel
    return View();
}
    }

    
}