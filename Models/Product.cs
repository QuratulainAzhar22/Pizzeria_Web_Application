namespace FastFood.Models
{
    public class Product
    {
        public string? Id {get; set;}
        public string? Name {get; set;}
        public float Price {get; set;}
        public string? Description {get; set;}
        public string? Category{get;set;}
        // Add this so you can store the link to the pizza image
        public string? ImageUrl { get; set; }
    }
}