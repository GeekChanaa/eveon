using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ChargingStationSelectDto
    {
        public int ID { get; set; } 
        public string Name { get; set; } 
    }
}