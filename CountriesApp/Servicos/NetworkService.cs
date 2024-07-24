namespace CountriesApp.Servicos
{
        using System.Net;
        using CountriesApp.Modelos;


        public class NetworkService
        {
            public Response CheckConnection()
            {
                var client = new WebClient();

                try
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        return new Response
                        {
                            IsSuccess = true,
                        };
                    }
                }
                catch
                {
                    return new Response
                    {
                        IsSuccess = false,
                        Message = "Configure your Internet connection",
                    };
                }
            }
        }
    }

