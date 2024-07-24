using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using static CountriesApp.Modelos.CountryName;


namespace CountriesApp.Modelos
{
    public class Country
    {
        [JsonProperty("name")]
        public CountryName Name { get; set; }

        private List<string> capital;

        [JsonProperty("capital")]
        public List<string> Capital
        {
            get
            {
                return capital;
            }
            set
            {
                capital = value;
            }
        }

        public string GetFirstCapitalOrDefault()
        {
            if (capital != null && capital.Count > 0)
            {
                return capital[0];
            }
            return "N/A";
        }

        private string region;
        [JsonProperty("region")]
        public string Region
        {
            get
            {
                return string.IsNullOrEmpty(region) ? "N/A" : region;
            }
            set
            {
                region = value;
            }
        }

        private string subregion;
        [JsonProperty("subregion")]
        public string Subregion
        {
            get
            {
                return string.IsNullOrEmpty(subregion) ? "N/A" : subregion;
            }
            set
            {
                subregion = value;
            }
        }

        [JsonProperty("population")]
        public long Population { get; set; }

        private Dictionary<string, double> _gini;
        [JsonProperty("gini")]
        public Dictionary<string, double> Gini
        {
            get
            {
                return _gini;
            }
            set
            {
                _gini = value;
            }
        }

        public string GiniString
        {
            get
            {
                if (_gini == null || !_gini.Values.Any() || _gini.Values.FirstOrDefault() == 0)
                {
                    return "N/A";
                }
                return _gini.Values.FirstOrDefault().ToString("N2");
            }
        }

        private Dictionary<string, string> _languages;
        [JsonProperty("languages")]
        public Dictionary<string, string> Languages
        {
            get
            {
                return _languages;
            }
            set
            {
                _languages = value;
            }
        }

        public string LanguagesString
        {
            get
            {
                if (_languages != null && _languages.Count > 0)
                {
                    return string.Join(", ", _languages.Values);
                }
                return "N/A";
            }
        }

        private Dictionary<string, Currency> _currencies;
        [JsonProperty("currencies")]
        public Dictionary<string, Currency> Currencies
        {
            get
            {
                return _currencies;
            }
            set
            {
                _currencies = value;
            }
        }

        public string CurrenciesString
        {
            get
            {
                if (_currencies != null && _currencies.Count > 0)
                {
                    var currencies = _currencies.Values.Select(c => $"{c.Name} ({c.Symbol})");
                    return string.Join(", ", currencies);
                }
                return "N/A";
            }
        }

        private double area;
        [JsonProperty("area")]
        public double Area
        {
            get
            {
                return area;
            }
            set
            {
                area = value;
            }
        }

        [JsonProperty("flags")]
        public CountryFlags Flags { get; set; }

        // Propriedade calculada para retornar a imagem adequada
        public async Task<BitmapImage> LoadFlagAsync()
        {
            if (Flags != null && !string.IsNullOrEmpty(Flags.Png))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        byte[] data = await client.GetByteArrayAsync(Flags.Png);
                        BitmapImage image = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(data))
                        {
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.StreamSource = stream;
                            image.EndInit();
                        }
                        return image;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao carregar imagem: " + ex.Message);
                }
            }

            // Se não houver URL de bandeira válida, retorna a imagem padrão
            return await LoadDefaultImageAsync();
        }

        private async Task<BitmapImage> LoadDefaultImageAsync()
        {
            try
            {
                Uri uri = new Uri("pack://application:,,,/CountriesApp;component/Resources/noimageAvi.jpg");
                BitmapImage defaultImage = new BitmapImage(uri);
                return defaultImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao carregar imagem padrão: " + ex.Message);
                return null; // Ou outra estratégia para lidar com erro na imagem padrão
            }
        }


        // Propriedades para os links dos mapas
        public Maps Maps { get; set; }
    }
}
