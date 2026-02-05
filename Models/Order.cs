using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastFood.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public string UserEmail { get; set; } = string.Empty;
        
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending";

        public ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}