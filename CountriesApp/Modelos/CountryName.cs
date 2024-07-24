using Newtonsoft.Json;

namespace CountriesApp.Modelos
{
    public class CountryName
    {
        [JsonProperty("common")]
        public string Common { get; set; }

        [JsonProperty("official")]
        public string Official { get; set; }

        [JsonProperty("nativeName")]
        public Dictionary<string, NativeName> NativeName { get; set; }

        public class Maps
        {
            public string GoogleMaps { get; set; }
            public string OpenStreetMaps { get; set; }
        }
    }
}
