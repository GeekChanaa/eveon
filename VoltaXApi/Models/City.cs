namespace VoltaXApi.Models
{
    public class City : IEntity
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public int? StateID { get; set; }
        public string? StateCode { get; set; }
        public int? CountryID { get; set; }
        public string? CountryCode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Flag { get; set; }
        public string? WikiDataId { get; set; }

        public State State { get; set; }
        public Country Country { get; set; }
    }
}