namespace csce360ChrisExampleUI.Server.Models
{
    // Mirrors csce360ChrisExampleAPI's Result model.
    public class ProductResult
    {
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool OnSale { get; set; }
    }
}
