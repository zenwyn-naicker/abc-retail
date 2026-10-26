/*Author: Zenwyn Naicker*/
/*Code Attribution: */
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

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration
       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
       .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
       .AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
