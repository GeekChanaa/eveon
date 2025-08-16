namespace VoltaXApi.Models
{
    public class State  : IEntity
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public int? CountryID { get; set; }
        public string? CountryCode { get; set; }
        public string? FipsCode { get; set; }
        public string? Iso2 { get; set; }
        public string? Type { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool Flag { get; set; }
        public string? WikiDataId { get; set; }
        public Country? Country { get; set; }
        public ICollection<City>? Cities { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}