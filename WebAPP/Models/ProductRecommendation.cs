namespace WebAPP.Models
{
    public class ProductRecommendation
    {
        public string UserId { get; set; }
        public List<int> RecommendedProductIds { get; set; }
    }

}
