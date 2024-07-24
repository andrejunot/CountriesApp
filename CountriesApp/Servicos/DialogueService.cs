using System.Windows;

namespace CountriesApp.Servicos
{
    public class DialogueService
    {
        public void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title);
        }
    }
}
