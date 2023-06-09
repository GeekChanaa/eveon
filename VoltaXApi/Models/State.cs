namespace VoltaXApi.Models
{
    public class State
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CountryID { get; set; }
        public string? CountryCode { get; set; }
        public string? FipsCode { get; set; }
        public string? Iso2 { get; set; }
        public string? Type { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Flag { get; set; }
        public string? WikiDataId { get; set; }

        public Country Country { get; set; }
        public ICollection<City> Cities { get; set; }
    }
}