namespace WondersAPI.Models
{
    /// <summary>
    /// Represents a wonder of the world.
    /// </summary>
    public class Wonder
    {
        /// <summary>
        /// The unique identifier of the wonder.
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// The name of the wonder.
        /// </summary>
        /// <example>Pyramids of Giza</example>
        public string Name { get; set; }

        /// <summary>
        /// The country where the wonder is located.
        /// </summary>
        /// <example>Egypt</example>
        public string Country { get; set; }

        /// <summary>
        /// The historical era of the wonder.
        /// </summary>
        /// <example>Ancient</example>
        public string Era { get; set; }

        /// <summary>
        /// The type of the wonder.
        /// </summary>
        /// <example>Tomb</example>
        public string Type { get; set; }

        /// <summary>
        /// A short description of the wonder.
        /// </summary>
        /// <example>One of the Seven Wonders of the Ancient World.</example>
        public string Description { get; set; }

        /// <summary>
        /// The year the wonder was discovered or built.
        /// </summary>
        /// <example>-2560</example>
        public int DiscoveryYear { get; set; }
    }
}
