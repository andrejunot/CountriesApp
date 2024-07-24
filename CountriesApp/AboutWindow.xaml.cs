using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace CountriesApp
{
    /// <summary>
    /// Interação lógica para janela About
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ImgGitHub_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/andrejunot",
                UseShellExecute = true
            });
        }

        private void ImgLinkedin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.linkedin.com/in/andréjúnior",
                UseShellExecute = true
            });
        }

    }
}
