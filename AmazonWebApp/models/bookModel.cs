using System.ComponentModel.DataAnnotations;

namespace AmazonWebApp.Models
{
    public class BookModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Book title is required")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Author name is required")]
        public required string Author { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
        public required decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer")]
        public required int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required")]
        public required int CategoryId { get; set; }

        public required string CategoryName { get; set; }

        [Required(ErrorMessage = "ISBN is required")]
        public required string ISBN { get; set; }

        public required DateTime PublishedDate { get; set; }

        public required string Publisher { get; set; }

        [Url(ErrorMessage = "Invalid URL format")]
        public required string CoverImageUrl { get; set; }

        [Required(ErrorMessage = "Date added is required")]
        public required DateTime DateAdded { get; set; }
    }

    public class CategoryModel
    {
        public required int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }
    }

    public class OrderModel
    {
        public required int Id { get; set; }

        public required int UserId { get; set; }

        [Required(ErrorMessage = "Total amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Total amount must be a non-negative value")]
        public required decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        public required DateTime OrderDate { get; set; }

        public required string OrderStatus { get; set; }

        public required List<OrderDetailModel> OrderDetails { get; set; }
    }

    public class AddOrderModel
    {
        public required int UserId { get; set; }
        public required decimal TotalAmount { get; set; }
        public required List<OrderDetailModel> OrderDetails { get; set; }
    }

    public class OrderDetailModel
    {
        public required int ProductId { get; set; }

        public required string ProductTitle { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer")]
        public required int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be a non-negative value")]
        public required decimal UnitPrice { get; set; }
    }

    public class CartModel
    {
        public required int UserId { get; set; }

        public required List<CartItemModel> Items { get; set; }

        public required decimal TotalPrice { get; set; }
    }

    public class CartItemModel
    {
        public required int ProductId { get; set; }

        public required string Title { get; set; }

        public required decimal Price { get; set; }

        public required int Quantity { get; set; }
    }
}
