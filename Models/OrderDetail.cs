
using System.ComponentModel.DataAnnotations.Schema;
namespace FastFood.Models
{
    public class OrderDetail
    {
       
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        public string ProductId { get; set; } = string.Empty;

        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTime { get; set; }

        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}