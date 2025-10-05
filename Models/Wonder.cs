namespace WondersAPI.Models
{
    public class Wonder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Era { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int DiscoveryYear { get; set; } 

        public string Description { get; set; } = string.Empty;
    }
}