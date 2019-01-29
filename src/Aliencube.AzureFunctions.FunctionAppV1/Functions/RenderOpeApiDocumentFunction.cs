﻿using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Aliencube.AzureFunctions.Extensions.DependencyInjection.Abstractions;
using Aliencube.AzureFunctions.Extensions.OpenApi.Abstractions;
using Aliencube.AzureFunctions.Extensions.OpenApi.Extensions;
using Aliencube.AzureFunctions.FunctionAppV1.Configurations;
using Aliencube.AzureFunctions.FunctionAppV1.Functions.FunctionOptions;

using Microsoft.Extensions.Logging;

namespace Aliencube.AzureFunctions.FunctionAppV1.Functions
{
    public class RenderOpeApiDocumentFunction : FunctionBase<ILogger>, IRenderOpeApiDocumentFunction
    {
        private readonly AppSettings _settings;
        private readonly IDocument _document;

        public RenderOpeApiDocumentFunction(AppSettings settings, IDocument document)
        {
            this._settings = settings;
            this._document = document;
        }

        public override async Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input, FunctionOptionsBase options = null)
        {
            this.Log.LogInformation("C# HTTP trigger function processed a request.");

            var req = input as HttpRequestMessage;
            var opt = options as RenderOpeApiDocumentFunctionOptions;

            var contentType = opt.Format.GetContentType();
            var result = await this._document.AddMetadata(this._settings.OpenApiInfo)
                                             .AddServer(req, this._settings.HttpSettings.RoutePrefix)
                                             .Build(Assembly.GetExecutingAssembly())
                                             .RenderAsync(opt.Version, opt.Format)
                                             .ConfigureAwait(false);

            var content = new StringContent(result, Encoding.UTF8, contentType);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

            return (TOutput)Convert.ChangeType(response, typeof(TOutput));
        }
    }
}