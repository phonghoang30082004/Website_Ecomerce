using System.Security.Cryptography.X509Certificates;

namespace WebAPP.Models
{
    public class CartItemModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total
        {
            get { return Quantity * Price; }
        }
        public string Image { get; set; }
        public int SupplierId { get; set; }
        public string StoreName { get; set; }

        public CartItemModel() { }

        public CartItemModel(ProductModel product)
        {
            ProductID = product.Id;
            ProductName = product.Name;
            Price = product.Price;
            Quantity = 1;
            Image = product.Image;
            SupplierId = product.SupplierId??0;
            StoreName = product.Supplier?.StoreName;
        }
    }
}
