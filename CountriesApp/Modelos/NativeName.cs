using Newtonsoft.Json;

namespace CountriesApp.Modelos
{
    public class NativeName
    {
        [JsonProperty("official")]
        public string Official { get; set; }

        [JsonProperty("common")]
        public string Common { get; set; }
    }
}
