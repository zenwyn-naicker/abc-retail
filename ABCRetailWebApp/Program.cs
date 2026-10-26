/*Author: Zenwyn Naicker*/
/*Code Attribution: */
/**************************************************************************************************************
 * Title: HOW TO MAKE AN MVC [CLDV6211]
 * Author: The IIE
 * Date: 2025
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Get started with ASP.NET Core MVC
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/start-mvc?view=aspnetcore-9.0&tabs=visual-studio
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Quickstart: Deploy an ASP.NET web app
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore?tabs=net80&pivots=development-environment-vs
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Customizing Navbar In MVC 5 With Bootstrap
 * Author: Hefaz, H
 * Date: 2025
 * Availability: https://www.csharp.com/article/customizing-navbar-in-mvc-5-with-bootstrap/
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: HTML, CSS, and C# in Web Development
 * Author: Dentistkiller Yusuf
 * Date: 2025
 * Availability: https://github.com/Dentistkiller/CSS
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Integrating Azure Blob Storage with .NET
 * Author: YogeshKumar Hadiya
 * Date: 2024
 * Availability: https://medium.com/c-sharp-programming/integrating-azure-blob-storage-with-net-b4fc16dfde73
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Introduction to Azure Table Storage
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Introduction to Azure Queue Storage
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Introduction to Azure File Share
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/files/storage-files-introduction
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Azure Functions HTTP trigger
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cfunctionsv2&pivots=programming-language-csharp
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: HTTP Functions [CLDV6211]
 * Author: The IIE
 * Date: 2025
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: MIGRATING A DATABASE TO AZURE [CLDV6211]
 * Author: The IIE
 * Date: 2025
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Use cookie authentication without ASP.NET Core Identity
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-9.0
 **************************************************************************************************************/

using ABCRetailWebApp.Data;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configure Entity Framework Core
            builder.Services.AddDbContext<ABCRetailDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("AzureSqlDatabase"),
                    sqlServerOptions => sqlServerOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                        .CommandTimeout(60)
                )
            );

            // Configure Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });

            builder.Services.AddAuthorization();

            // Register Azure Storage Services
            builder.Services.AddScoped<AzureTableStorageService>();
            builder.Services.AddScoped<AzureQueueStorageService>();
            builder.Services.AddScoped<AzureBlobStorageService>();
            builder.Services.AddScoped<AzureFileShareService>();

            // Register Database Services (Entity Framework Core)
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
            builder.Services.AddScoped<IOrderService, OrderService>();

            // Register Azure Functions Service
            builder.Services.AddScoped<AzureFunctionsService>();

            // Configure HttpClient for Azure Functions
            builder.Services.AddHttpClient("AzureFunctions", client =>
            {
                // Get the Function App URL from configuration or use default
                var functionAppUrl = builder.Configuration["AzureFunctions:BaseUrl"]
                    ?? "http://localhost:7071/";

                client.BaseAddress = new Uri(functionAppUrl);

                // Get function key from configuration
                var functionKey = builder.Configuration["AzureFunctions:FunctionKey"];
                if (!string.IsNullOrEmpty(functionKey))
                {
                    client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
                }

                // Set timeout
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
