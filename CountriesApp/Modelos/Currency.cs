using Newtonsoft.Json;

namespace CountriesApp.Modelos
{
    public class Currency
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
