using Newtonsoft.Json;

namespace CountriesApp.Modelos
{
    public class CountryFlags
    {
        [JsonProperty("png")]
        public string Png { get; set; }

        [JsonProperty("svg")]
        public string Svg { get; set; }
    }
}
