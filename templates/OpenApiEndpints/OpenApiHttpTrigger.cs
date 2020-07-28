#if !NET461
using System.Net;
using System.Threading.Tasks;

using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Aliencube.AzureFunctions.Extensions.OpenApi.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Aliencube.AzureFunctions.Extensions.OpenApi
{
    /// <summary>
    /// This represents the HTTP trigger entity for Open API documents.
    /// </summary>
    public static class OpenApiHttpTrigger
    {
        private const string V2 = "v2";
        private const string V3 = "v3";
        private const string JSON = "json";
        private const string YAML = "yaml";

        private readonly static IOpenApiHttpTriggerContext context = new OpenApiHttpTriggerContext();

        /// <summary>
        /// Invokes the HTTP trigger endpoint to get Open API document.
        /// </summary>
        /// <param name="req"><see cref="HttpRequest"/> instance.</param>
        /// <param name="extension">File extension representing the document format. This MUST be either "json" or "yaml".</param>
        /// <param name="log"><see cref="ILogger"/> instance.</param>
        /// <returns>Open API document in a format of either JSON or YAML.</returns>
        [FunctionName(nameof(OpenApiHttpTrigger.RenderSwaggerDocument))]
        [OpenApiIgnore]
        public static async Task<IActionResult> RenderSwaggerDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/swagger.{extension}")] HttpRequest req,
            string extension,
            ILogger log)
        {
            if (IsProduction())
                return new NotFoundResult();

            log.LogInformation($"swagger.{extension} was requested.");

            var result = await context.Document
                                      .InitialiseDocument()
                                      .AddMetadata(context.OpenApiInfo)
                                      .AddServer(req, context.HttpSettings.RoutePrefix)
                                      .Build(context.GetExecutingAssembly(), context.NamingStrategy)
                                      .RenderAsync(context.GetOpenApiSpecVersion(V2), context.GetOpenApiFormat(extension))
                                      .ConfigureAwait(false);

            var content = new ContentResult()
            {
                Content = result,
                ContentType = context.GetOpenApiFormat(extension).GetContentType(),
                StatusCode = (int)HttpStatusCode.OK
            };

            return content;
        }

        /// <summary>
        /// Invokes the HTTP trigger endpoint to get Open API document.
        /// </summary>
        /// <param name="req"><see cref="HttpRequest"/> instance.</param>
        /// <param name="version">Open API document spec version. This MUST be either "v2" or "v3".</param>
        /// <param name="extension">File extension representing the document format. This MUST be either "json" or "yaml".</param>
        /// <param name="log"><see cref="ILogger"/> instance.</param>
        /// <returns>Open API document in a format of either JSON or YAML.</returns>
        [FunctionName(nameof(OpenApiHttpTrigger.RenderOpenApiDocument))]
        [OpenApiIgnore]
        public static async Task<IActionResult> RenderOpenApiDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/openapi/{version}.{extension}")] HttpRequest req,
            string version,
            string extension,
            ILogger log)
        {
            if (IsProduction())
                return new NotFoundResult();

            log.LogInformation($"{version}.{extension} was requested.");

            var result = await context.Document
                                      .InitialiseDocument()
                                      .AddMetadata(context.OpenApiInfo)
                                      .AddServer(req, context.HttpSettings.RoutePrefix)
                                      .Build(context.GetExecutingAssembly(), context.NamingStrategy)
                                      .RenderAsync(context.GetOpenApiSpecVersion(version), context.GetOpenApiFormat(extension))
                                      .ConfigureAwait(false);

            var content = new ContentResult()
            {
                Content = result,
                ContentType = context.GetOpenApiFormat(extension).GetContentType(),
                StatusCode = (int)HttpStatusCode.OK
            };

            return content;
        }

        /// <summary>
        /// Invokes the HTTP trigger endpoint to render Swagger UI in HTML.
        /// </summary>
        /// <param name="req"><see cref="HttpRequest"/> instance.</param>
        /// <param name="log"><see cref="ILogger"/> instance.</param>
        /// <returns>Swagger UI in HTML.</returns>
        [FunctionName(nameof(OpenApiHttpTrigger.RenderSwaggerUIIndex))]
        [OpenApiIgnore]
        public static async Task<IActionResult> RenderSwaggerUIIndex(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/index.html")] HttpRequest req,
            ILogger log)
        {
            if (IsProduction())
                return new NotFoundResult();

            log.LogInformation($"SwaggerUI page was requested.");

            var result = await context.SwaggerUI
                                      .AddMetadata(context.OpenApiInfo)
                                      .AddServer(req, context.HttpSettings.RoutePrefix)
                                      .BuildAsync()
                                      .RenderAsync("swagger/openapi/1.json", context.GetSwaggerAuthKey())
                                      .ConfigureAwait(false);

            var content = new ContentResult()
            {
                Content = result,
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK
            };

            return content;
        }

        [FunctionName(nameof(OpenApiHttpTrigger.RenderSwaggerUI))]
        [OpenApiIgnore]
        public static Task<IActionResult> RenderSwaggerUI(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger")] HttpRequest req,
        ILogger log)
        {
            return RenderSwaggerUIIndex(req, log);
        }

        public static bool IsProduction()
        {
            const string ProductionEnvironmentName = "Production";
            const string EnvironmentVariableName = "AZURE_FUNCTIONS_ENVIRONMENT";

            return ProductionEnvironmentName
                .Equals(System.Environment.GetEnvironmentVariable(EnvironmentVariableName));
        }
    }
}
#endif
