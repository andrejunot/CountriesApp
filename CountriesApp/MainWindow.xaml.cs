using CountriesApp.Modelos;
using CountriesApp.Servicos;
using LiveCharts;
using LiveCharts.Wpf;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CountriesApp
{
    public partial class MainWindow : Window
    {
        private const string ApiUrl = "https://restcountries.com/v3.1/all";
        private ApiService apiService;
        private DataService dataService;
        private NetworkService networkService;
        private List<Country> countries;

        public MainWindow()
        {
            InitializeComponent();

            apiService = new ApiService();
            dataService = new DataService();

            listBoxCountries.SelectionChanged += ListBoxCountries_SelectionChanged;
            listBoxCountries.KeyDown += ListBoxCountries_KeyDown; // Adds the KeyDown event handler

            // Calls the asynchronous method to load countries
            LoadCountriesAsync();
        }

        /// <summary>
        /// Loads the list of countries asynchronously.
        /// </summary>
        private async void LoadCountriesAsync()
        {
            bool loadFromApi;
            for (int i = 10; i <= 90; i += 10)
            {
                ProgressBar.Value = i;
                await Task.Delay(10); // 10ms delay between each value change
            }

            labelResultado.Content = "Updating countries....";

            NetworkService networkService = new NetworkService();
            var connectionResult = networkService.CheckConnection();

            if (!connectionResult.IsSuccess)
            {
                MessageBox.Show(connectionResult.Message);
                // Load countries from local database
                countries = await dataService.GetCountriesAsync();
                loadFromApi = false;
            }
            else
            {
                var progress = new Progress<int>(value => ProgressBar.Value = value);
                countries = await LoadApiCountries(progress);
                loadFromApi = true;
            }

            if (countries == null || countries.Count == 0)
            {
                labelResultado.Content = "No internet connection" + Environment.NewLine +
                                         "and no countries were previously loaded" + Environment.NewLine +
                                         "Try again later!";

                labelStatus.Content = "Initial setup requires an internet connection";
                return;
            }

            // Populate the listBox with countries
            await DisplayCountriesAsync();

            labelResultado.Content = "Countries updated...";

            if (loadFromApi)
            {
                labelStatus.Content = string.Format("Countries loaded from the internet at {0:F}", DateTime.Now);
                // Download and save flags locally
                await dataService.SaveFlagsLocallyAsync(countries);
            }
            else
            {
                labelStatus.Content = "Countries loaded from the Database.";
            }

            // When the API loading is completed, the bar will be filled
            ProgressBar.Value = 100;

            // Initially, show data for the first country
            if (countries.Count > 0)
            {
                await UpdateChartsAsync(countries[0]);
            }
        }

        /// <summary>
        /// Loads the list of countries from the API.
        /// </summary>
        /// <param name="progress">Progress of the operation.</param>
        /// <returns>List of Country objects.</returns>
        private async Task<List<Country>> LoadApiCountries(IProgress<int> progress)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var countries = await apiService.GetCountries(ApiUrl);

                    if (countries != null)
                    {
                        // Sort countries by name using LINQ
                        countries = countries.OrderBy(c => c.Name.Common).ToList();

                        await dataService.DeleteCountriesAsync();
                        // Save the countries to the local database (SQLite) upon loading
                        await dataService.SaveCountriesAsync(countries, progress);

                        return countries;
                    }
                    else
                    {
                        MessageBox.Show("Unable to load the list of countries.");
                        return new List<Country>(); // Returns an empty list in case of failure
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while loading the countries: " + ex.Message);
                    return new List<Country>(); // Returns an empty list in case of exception
                }
            });
        }

        /// <summary>
        /// Displays the list of countries in the interface.
        /// </summary>
        private async Task DisplayCountriesAsync()
        {
            countries = await dataService.GetCountriesAsync();

            if (countries != null)
            {
                listBoxCountries.ItemsSource = countries.Select(country =>
                {
                    string flagPath = dataService.GetFlagPath(country.Name.Common);
                    if (!string.IsNullOrEmpty(flagPath))
                    {
                        country.Flags.Png = flagPath;
                    }
                    return country;
                }).ToList();
                SelectFirstItem();
            }
        }

        /// <summary>
        /// Selects the first item in the list of countries.
        /// </summary>
        private void SelectFirstItem()
        {
            if (listBoxCountries.Items.Count > 0)
            {
                listBoxCountries.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Shows the details of the selected country.
        /// </summary>
        /// <param name="country">Selected Country object.</param>
        private async Task ShowCountryDetailsAsync(Country country)
        {
            labelName.Text = country.Name.Common;
            labelCapital.Text = country.GetFirstCapitalOrDefault();
            labelRegion.Text = country.Region;
            lblSubRegion.Text = country.Subregion;
            labelPopulation.Text = country.Population.ToString("N0");
            labelArea.Text = $"{country.Area.ToString("N0")} km²";
            labelLanguages.Text = country.LanguagesString;
            labelCurrencies.Text = country.CurrenciesString;
            labelGini.Text = country.GiniString;

            string flagPath = dataService.GetFlagPath(country.Name.Common);
            bool isConnected = IsInternetConnected();

            if (flagPath != null)
            {
                pictureBoxFlag.Source = new BitmapImage(new Uri(flagPath));
            }
            else if (isConnected)
            {
                pictureBoxFlag.Source = new BitmapImage(new Uri(country.Flags.Png));
            }
            else
            {
                // If the flag is not available locally and there is no connection, display a default image or message
                pictureBoxFlag.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/noimageAvi.jpg"));
            }
        }

        /// <summary>
        /// Checks if there is an internet connection.
        /// </summary>
        /// <returns>True if there is a connection, otherwise False.</returns>
        private bool IsInternetConnected()
        {
            try
            {
                using (var client = new System.Net.WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Event handler for selection change in the listBox.
        /// </summary>
        private async void ListBoxCountries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = listBoxCountries.SelectedIndex;
            if (selectedIndex >= 0)
            {
                Country selectedCountry = countries[selectedIndex];
                await ShowCountryDetailsAsync(selectedCountry);
                await UpdateChartsAsync(selectedCountry);
            }
        }

        /// <summary>
        /// Event handler to open Google Maps when clicking on the link.
        /// </summary>
        private void LblGoogleMaps_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            bool isConnected = IsInternetConnected();

            if (!isConnected)
            {
                MessageBox.Show("Service available only with an internet connection");
                return;
            }

            if (listBoxCountries.SelectedIndex >= 0 && listBoxCountries.SelectedIndex < countries.Count)
            {
                string googleMapsUrl = countries[listBoxCountries.SelectedIndex].Maps.GoogleMaps;

                if (!string.IsNullOrEmpty(googleMapsUrl))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(googleMapsUrl) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening Google Maps: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("No Google Maps URL available for this country.");
                }
            }
        }

        /// <summary>
        /// Updates the charts for population, Gini, and area for the selected country.
        /// </summary>
        private async Task UpdateChartsAsync(Country country)
        {
            await UpdatePopulationChartAsync(country);
            await UpdateGiniChartAsync(country);
            await UpdateAreaChartAsync(country);
        }

        /// <summary>
        /// Updates the population chart for the selected country.
        /// </summary>
        private async Task UpdatePopulationChartAsync(Country country)
        {
            await Task.Run(() =>
            {
                cartesianChart1.Dispatcher.Invoke(() =>
                {
                    cartesianChart1.Series.Clear();

                    var populationSeries = new ColumnSeries
                    {
                        Title = "Population",
                        Values = new ChartValues<double> { country.Population }
                    };

                    cartesianChart1.Series = new SeriesCollection { populationSeries };

                    cartesianChart1.AxisX.Clear();
                    cartesianChart1.AxisX.Clear();
                    cartesianChart1.AxisX.Add(new Axis
                    {
                        Title = "Country",
                        Labels = new[] { country.Name.Common }
                    });

                    cartesianChart1.AxisY.Clear();
                    cartesianChart1.AxisY.Add(new Axis
                    {
                        Title = "Population",
                        LabelFormatter = value => value.ToString("N0")
                    });
                });
            });

            cartesianChart1.Dispatcher.Invoke(() =>
            {
                lblPopulationStatus.Visibility = country.Population <= 0 ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// Updates the Gini index chart for the selected country.
        /// </summary>
        private async Task UpdateGiniChartAsync(Country country)
        {
            await Task.Run(() =>
            {
                cartesianChart2.Dispatcher.Invoke(() =>
                {
                    cartesianChart2.Series.Clear();

                    var giniSeries = new ColumnSeries
                    {
                        Title = "Gini Index",
                        Fill = System.Windows.Media.Brushes.DarkGray,
                        Values = new ChartValues<double> { country.Gini != null && country.Gini.Values.Count > 0 ? country.Gini.Values.First() : 0 }
                    };

                    cartesianChart2.Series = new SeriesCollection { giniSeries };

                    cartesianChart2.AxisX.Clear();
                    cartesianChart2.AxisX.Add(new Axis
                    {
                        Title = "Country",
                        Labels = new[] { country.Name.Common }
                    });

                    cartesianChart2.AxisY.Clear();
                    cartesianChart2.AxisY.Add(new Axis
                    {
                        Title = "Gini Index",
                        LabelFormatter = value => value.ToString("N2")
                    });
                });
            });

            cartesianChart2.Dispatcher.Invoke(() =>
            {
                lblGiniStatus.Visibility = country.Gini == null || !country.Gini.Values.Any() || country.Gini.Values.First() == 0 ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// Updates the area chart for the selected country.
        /// </summary>
        private async Task UpdateAreaChartAsync(Country country)
        {
            await Task.Run(() =>
            {
                cartesianChart3.Dispatcher.Invoke(() =>
                {
                    cartesianChart3.Series.Clear();

                    var areaSeries = new ColumnSeries
                    {
                        Title = "Area",
                        Fill = System.Windows.Media.Brushes.DarkSeaGreen,
                        Values = new ChartValues<double> { country.Area }
                    };

                    cartesianChart3.Series = new SeriesCollection { areaSeries };

                    cartesianChart3.AxisX.Clear();
                    cartesianChart3.AxisX.Add(new Axis
                    {
                        Title = "Country",
                        Labels = new[] { country.Name.Common }
                    });

                    cartesianChart3.AxisY.Clear();
                    cartesianChart3.AxisY.Add(new Axis
                    {
                        Title = "Area (km²)",
                        LabelFormatter = value => value.ToString("N0")
                    });
                });
            });

            cartesianChart3.Dispatcher.Invoke(() =>
            {
                lblAreaStatus.Visibility = country.Area <= 0 ? Visibility.Visible : Visibility.Hidden;
                if (country.Area <= 0)
                {
                    Canvas.SetZIndex(lblAreaStatus, 1);
                }
            });
        }

        /// <summary>
        /// Event handler to capture the key pressed and select the corresponding country.
        /// </summary>
        private void ListBoxCountries_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                char pressedKey = (char)('A' + (e.Key - Key.A));
                SelectCountryByKey(pressedKey);
            }
        }

        /// <summary>
        /// Selects the country based on the pressed key.
        /// </summary>
        /// <param name="pressedKey">Pressed key.</param>
        private void SelectCountryByKey(char pressedKey)
        {
            for (int i = 0; i < countries.Count; i++)
            {
                if (countries[i].Name.Common.StartsWith(pressedKey.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    listBoxCountries.SelectedIndex = i;
                    listBoxCountries.ScrollIntoView(listBoxCountries.Items[i]);
                    break;
                }
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
    }
}
