
namespace FastFood.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public Product? Product { get; set; }
    }
}