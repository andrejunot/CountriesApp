using CountriesApp.Modelos;
using Newtonsoft.Json;
using System.Net.Http;

namespace CountriesApp.Servicos
{
    public class ApiService
    {
        public async Task<List<Country>> GetCountries(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        List<Country> countries = JsonConvert.DeserializeObject<List<Country>>(json);
                        return countries;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
