using System.ComponentModel.DataAnnotations;

namespace ASPCommerce.Models
{
    public class BookModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Book title is required")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Author name is required")]
        public required string Author { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public  string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required")]
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        [Required(ErrorMessage = "ISBN is required")]
        public string ISBN { get; set; }

        public DateTime PublishedDate { get; set; }

        public string Publisher { get; set; }

        [Url(ErrorMessage = "Invalid URL format")]
        public string CoverImageUrl { get; set; }

        [Required(ErrorMessage = "Date added is required")]
        public DateTime DateAdded { get; set; }
    }

    public class CategoryModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
    }

    public class OrderModel
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required(ErrorMessage = "Total amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Total amount must be a non-negative value")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        public DateTime OrderDate { get; set; }

        public string OrderStatus { get; set; }

        public List<OrderDetailModel> OrderDetails { get; set; }
    }

    public class AddOrderModel
    {
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderDetailModel> OrderDetails { get; set; }
    }

    public class OrderDetailModel
    {
        public int ProductId { get; set; }

        public string ProductTitle { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be a non-negative value")]
        public decimal UnitPrice { get; set; }
    }

    public class CartModel
    {
        public int UserId { get; set; }

        public List<CartItemModel> Items { get; set; }

        public decimal TotalPrice { get; set; }
    }

    public class CartItemModel
    {
        public int ProductId { get; set; }

        public string Title { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}
