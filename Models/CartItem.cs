using System.Text.Json.Serialization; // Thêm dòng này
using Newtonsoft.Json; // Thêm dòng này

namespace CoffeeHouseAdmin.Models
{
    public class CartItem
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("image")]
        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonProperty("price")]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonProperty("quantity")]
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public decimal Total => Price * Quantity;
    }
}