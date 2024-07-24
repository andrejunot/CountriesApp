using CountriesApp.Modelos;
using Newtonsoft.Json;
using static CountriesApp.Modelos.CountryName;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class DataService
{
    private readonly SQLiteConnection _connection;
    private readonly string _dbPath;
    private readonly string _flagsFolderPath;
    private readonly string _mapsApiUrl = "https://restcountries.com/v3.1/name/";

    public DataService()
    {
        _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "countries.db3");
        _flagsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flags");

        if (!Directory.Exists(_flagsFolderPath))
        {
            Directory.CreateDirectory(_flagsFolderPath);
        }

        _connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        InitializeDatabaseAsync().Wait();
    }

    /// <summary>
    /// Initializes the database by creating the table if it does not exist.
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            await _connection.OpenAsync();

            // Creates the countries table if it does not exist
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS countries (
                    Name TEXT NOT NULL,
                    Capital TEXT,
                    Region TEXT,
                    Subregion TEXT,
                    Population INTEGER,
                    Gini REAL,
                    Flag TEXT,
                    Languages TEXT,
                    Currencies TEXT,
                    Area REAL,
                    GoogleMaps TEXT,
                    OpenStreetMaps TEXT
                )";
            using (var command = new SQLiteCommand(createTableQuery, _connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            _connection.Close();
        }
        catch (Exception e)
        {
            throw new Exception("Error initializing the database", e);
        }
    }

    /// <summary>
    /// Retrieves the list of countries from the database.
    /// </summary>
    /// <returns>List of Country objects.</returns>
    public async Task<List<Country>> GetCountriesAsync()
    {
        try
        {
            await _connection.OpenAsync();
            string query = "SELECT * FROM countries";
            var command = new SQLiteCommand(query, _connection);
            var reader = await command.ExecuteReaderAsync();

            var countries = new List<Country>();
            while (await reader.ReadAsync())
            {
                var country = new Country
                {
                    Name = new CountryName { Common = reader["Name"].ToString() },
                    Capital = new List<string> { reader["Capital"].ToString() },
                    Region = reader["Region"].ToString(),
                    Subregion = reader["Subregion"].ToString(),
                    Population = Convert.ToInt64(reader["Population"]),
                    Gini = new Dictionary<string, double> { { "value", Convert.ToDouble(reader["Gini"]) } },
                    Flags = new CountryFlags { Png = reader["Flag"].ToString() },
                    Languages = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader["Languages"].ToString()),
                    Currencies = JsonConvert.DeserializeObject<Dictionary<string, Currency>>(reader["Currencies"].ToString()),
                    Area = Convert.ToDouble(reader["Area"]),
                    Maps = new Maps()
                };

                // Checks if there are map links in the database
                if (reader["GoogleMaps"] != DBNull.Value)
                {
                    country.Maps.GoogleMaps = reader["GoogleMaps"].ToString();
                }

                if (reader["OpenStreetMaps"] != DBNull.Value)
                {
                    country.Maps.OpenStreetMaps = reader["OpenStreetMaps"].ToString();
                }

                countries.Add(country);
            }

            _connection.Close();
            return countries;
        }
        catch (Exception e)
        {
            throw new Exception("Error loading countries", e);
        }
    }

    /// <summary>
    /// Saves the list of countries to the database and stores flags locally.
    /// </summary>
    /// <param name="countries">List of countries to save.</param>
    /// <param name="progress">Progress of the operation.</param>
    public async Task SaveCountriesAsync(List<Country> countries, IProgress<int> progress = null)
    {
        try
        {
            await _connection.OpenAsync();
            using (var transaction = _connection.BeginTransaction())
            {
                for (int i = 0; i < countries.Count; i++)
                {
                    var country = countries[i];
                    var giniValue = country.Gini != null && country.Gini.Values.Count > 0 ? country.Gini.Values.First() : 0;
                    var languagesJson = JsonConvert.SerializeObject(country.Languages);
                    var currenciesJson = JsonConvert.SerializeObject(country.Currencies);

                    string insertQuery = @"
                        INSERT OR REPLACE INTO countries (Name, Capital, Region, Subregion, Population, Gini, Flag, Languages, Currencies, Area, GoogleMaps, OpenStreetMaps)
                        VALUES (@Name, @Capital, @Region, @Subregion, @Population, @Gini, @Flag, @Languages, @Currencies, @Area, @GoogleMaps, @OpenStreetMaps)";
                    using (var command = new SQLiteCommand(insertQuery, _connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Name", country.Name.Common);
                        command.Parameters.AddWithValue("@Capital", country.GetFirstCapitalOrDefault());
                        command.Parameters.AddWithValue("@Region", country.Region);
                        command.Parameters.AddWithValue("@Subregion", country.Subregion);
                        command.Parameters.AddWithValue("@Population", country.Population);
                        command.Parameters.AddWithValue("@Gini", giniValue);
                        command.Parameters.AddWithValue("@Flag", country.Flags.Png);
                        command.Parameters.AddWithValue("@Languages", languagesJson);
                        command.Parameters.AddWithValue("@Currencies", currenciesJson);
                        command.Parameters.AddWithValue("@Area", country.Area);

                        // Checks for null values before assigning
                        if (country.Maps?.GoogleMaps != null)
                        {
                            command.Parameters.AddWithValue("@GoogleMaps", country.Maps.GoogleMaps);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@GoogleMaps", DBNull.Value);
                        }

                        if (country.Maps?.OpenStreetMaps != null)
                        {
                            command.Parameters.AddWithValue("@OpenStreetMaps", country.Maps.OpenStreetMaps);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@OpenStreetMaps", DBNull.Value);
                        }

                        await command.ExecuteNonQueryAsync();
                    }
                    progress?.Report((i + 1) * 100 / countries.Count);
                }
                transaction.Commit();
            }
            _connection.Close();

            // Download and save flags locally after saving countries to the database
            await SaveFlagsLocallyAsync(countries);
        }
        catch (Exception e)
        {
            throw new Exception("Error saving countries", e);
        }
    }

    /// <summary>
    /// Deletes all countries from the database.
    /// </summary>
    public async Task DeleteCountriesAsync()
    {
        try
        {
            await _connection.OpenAsync();
            string deleteQuery = "DELETE FROM countries";
            using (var command = new SQLiteCommand(deleteQuery, _connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            _connection.Close();
        }
        catch (Exception e)
        {
            throw new Exception("Error deleting countries", e);
        }
    }

    /// <summary>
    /// Saves country flags locally.
    /// </summary>
    /// <param name="countries">List of countries whose flags will be saved.</param>
    public async Task SaveFlagsLocallyAsync(List<Country> countries)
    {
        if (!Directory.Exists(_flagsFolderPath))
        {
            Directory.CreateDirectory(_flagsFolderPath);
        }

        using (HttpClient client = new HttpClient())
        {
            var tasks = countries.Select(async country =>
            {
                string flagUrl = country.Flags.Png;
                string fileName = $"{country.Name.Common}.png";
                string filePath = Path.Combine(_flagsFolderPath, fileName);

                if (!File.Exists(filePath))
                {
                    try
                    {
                        byte[] flagBytes = await client.GetByteArrayAsync(flagUrl);
                        await File.WriteAllBytesAsync(filePath, flagBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to download flag for {country.Name.Common}: {ex.Message}");
                    }
                }
            });

            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Retrieves the path of a specific country's flag.
    /// </summary>
    /// <param name="countryName">Country name.</param>
    /// <returns>Flag path.</returns>
    public string GetFlagPath(string countryName)
    {
        string fileName = $"{countryName}.png";
        string filePath = Path.Combine(_flagsFolderPath, fileName);
        return File.Exists(filePath) ? filePath : null;
    }
}
