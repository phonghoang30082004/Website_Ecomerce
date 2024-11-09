using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace WebAPP.Services
{
    public class ProductRecommendationService
    {
        private readonly HttpClient _httpClient;

        public ProductRecommendationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<int>> GetRecommendationsAsync(string userId, int nRecommendations = 2)
        {

            var url = $"http://localhost:5002/recommend?user_id={userId}&n_recommendations={nRecommendations}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecommendationResult>(content);
                return result?.RecommendedProducts ?? new List<int>(); // Sử dụng toán tử null-coalescing
            }
            return new List<int>();
        }
    }
    public class RecommendationResult
    {
        [JsonPropertyName("recommended_products")]
        public List<int> RecommendedProducts { get; set; }
    }
}
