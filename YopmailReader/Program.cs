using HtmlAgilityPack;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace YopmailReader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Service start");
            int port = Convert.ToInt32(args[0]);


            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost
            //.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True")
            .ConfigureKestrel((context, serverOptions) =>
            {

                serverOptions.Listen(IPAddress.Any, port);
                //serverOptions.Listen(IPAddress.Any, httpsPort, listenOptions =>
                //{
                //listenOptions.UseHttps();//nada de certificados.... por ahora-

                //});
            });

            var app = builder.Build();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("test", HealtCheck);
                endpoints.MapGet("messages", ReadEmail);

            });

            app.Run();

        }

        private static async Task HealtCheck(HttpContext context)
        {
            string message = "ready online";

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status200OK;

            var responseMessage = new
            {
                message = message
            };
            string jsonResponse = JsonConvert.SerializeObject(responseMessage);
            await context.Response.WriteAsync(jsonResponse);
        }


        private static async Task ReadEmail(HttpContext context)
        {
            string htmlContent;

            try
            {
                
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                var page = await browser.NewPageAsync();

                
                await page.GotoAsync("https://yopmail.com/es/");
                await page.FillAsync("#login", "pruebaemail"); //pruebaemail es el email
                await page.ClickAsync("button[title='Revisa el correo @yopmail.com']");

                //espero a que cargue el iframe del mensaje
                await page.WaitForTimeoutAsync(2000); 

                
                var mailFrame = page.Frames.FirstOrDefault(f => f.Name == "ifmail");
                string emailHtml = mailFrame != null ? await mailFrame.ContentAsync() : "<html><body><h2>No se encontró contenido del email.</h2></body></html>";

                htmlContent = $"<h1>Contenido del Email:</h1><hr>{emailHtml}";

                await browser.CloseAsync();
            }
            catch (Exception ex)
            {
                htmlContent = $"<html><body><h2>ERROR al acceder a Yopmail: {ex.Message}</h2></body></html>";
            }

            context.Response.ContentType = "text/html";
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync(htmlContent);
        }

    }
}
