using Microsoft.EntityFrameworkCore;
using FastFood.Data;
using FastFood.Models;
using FastFood.Repository;
using FastFood.Interfaces;
// using FastFood.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// In Program.cs
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "User.Auth";
        config.LoginPath = "/Account/Login"; // Redirect here if unauthorized
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthorization(options =>
{
    // Example 1: Simple Claim Check
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("AdminStatus", "true"));

    // Example 2: Multiple Claims (Must be an Employee AND a Manager)
    options.AddPolicy("ManagerLevel", policy => 
        policy.RequireClaim("EmployeeNumber")
              .RequireClaim("Role", "Manager"));
});
// 1. Register the Dapper Context (already done previously)
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddSession();
// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

 //builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();
// builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
//     options.SignIn.RequireConfirmedAccount = false;
//     options.Password.RequireDigit = false; // Optional: Makes testing easier
//     options.Password.RequiredLength = 6;
// })
//     .AddEntityFrameworkStores<ApplicationDbContext>();
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//     .AddEntityFrameworkStores<ApplicationDbContext>();

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(connectionString));
// builder.Services.AddDefaultIdentity<IdentityUser>(options => {
//     options.SignIn.RequireConfirmedAccount = false;
//     options.Password.RequireDigit = false; // Make it easy for testing
//     options.Password.RequiredLength = 6;
// })
// .AddRoles<IdentityRole>() // This allows "Admin" and "Customer" roles
// .AddEntityFrameworkStores<ApplicationDbContext>();    
builder.Services.AddControllersWithViews();
// builder.Services.AddRazorPages();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
// builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
//     // This ensures the database and tables exist before we try to add data
//     context.Database.EnsureCreated(); 

//     if (!context.Products.Any())
//     {
//         context.Products.AddRange(
            // new Product { Name = "Margherita Classico", Price = 12.99f, Description = "Simple perfection with fresh basil, mozzarella, and San Marzano tomatoes.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1574071318508-1cdbad80ad38?w=500" },
            // new Product { Name = "Pepperoni Feast", Price = 14.50f, Description = "The fan favorite. Double pepperoni and extra mozzarella cheese.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=500" },
            // new Product { Name = "BBQ Smokehouse", Price = 15.99f, Description = "Grilled chicken, red onions, and smoky BBQ sauce on a thin crust.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=500" },
            // new Product { Name = "The Garden Veggie", Price = 13.50f, Description = "Roasted bell peppers, mushrooms, red onions, and black olives.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1571407970349-bc81e7e96d47?w=500" },
            // new Product { Name = "Spicy Fajita", Price = 16.00f, Description = "Zesty chicken, jalapeños, and sliced bell peppers for a kick.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=500" },
            // new Product { Name = "Truffle Mushroom", Price = 18.25f, Description = "Creamy white sauce with sautéed mushrooms and premium truffle oil.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1541745537411-b8046dc6d66c?w=500" },
            // new Product { Name = "Meat Lovers", Price = 17.50f, Description = "A hearty mix of beef, ham, sausage, and pepperoni.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1593504049359-7b7d92c7185d?w=500" },
            // new Product { Name = "Hawaiian Tropical", Price = 14.00f, Description = "Sweet pineapple and savory ham on a golden crust.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=500" },
            // new Product { Name = "Four Cheese Delight", Price = 15.50f, Description = "A blend of Mozzarella, Parmesan, Cheddar, and Gorgonzola.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1573821663912-56990145564c?w=500" },
            // new Product { Name = "Mediterranean Greek", Price = 15.00f, Description = "Feta cheese, sun-dried tomatoes, and kalamata olives.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1534308983496-4fabb1a015ee?w=500" },
            // new Product { Name = "Buffalo Chicken", Price = 16.50f, Description = "Spicy buffalo sauce, crispy chicken, and ranch drizzle.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=500" },
            // new Product { Name = "Seafood Supreme", Price = 19.99f, Description = "Garlic butter base with fresh shrimp and calamari.", Category = "Pizza", ImageUrl = "https://images.unsplash.com/photo-1590947132387-155cc02f3212?w=500" }
//         );
//         context.SaveChanges();
//     }
// }
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
 app.UseAuthentication();
app.UseAuthorization();
// app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 2. Map the hub to a route
//app.MapHub<NotificationHubs>("/notificationHubs");
app.Run();
