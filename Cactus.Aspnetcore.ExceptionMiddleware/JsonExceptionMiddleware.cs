using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cactus.Aspnetcore.ExceptionMiddleware
{
    public class JsonExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _log;
        private readonly bool _isDetailsEnabled;

        public JsonExceptionMiddleware(RequestDelegate next, ILogger<JsonExceptionMiddleware> log, ExceptionMiddlewareConfig config)
        {
            _next = next;
            _log = log;
            _isDetailsEnabled = config.EnableDetails;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }

            catch (Exception ex)
            {
                _log?.LogWarning("Exception catched during process {0} {1}: {3}", context.Request.Method, context.Request.Path, ex.ToString());

                if (!context.Response.HasStarted)
                {
                    if (!IsExceptionHandable(context, ex))
                    {
                        _log?.LogDebug("Exception throwed up by pipline, cause of IsExceptionHandable returned false");
                        throw;
                    }

                    var status = BuildHttpStatus(ex);
                    var body = BuildResponseBody(ex);
                    await HandleResponse(context, status, body);
                    _log?.LogDebug("{0}: {1} converted to HTTP {2}", ex.GetType().Name, ex.Message, status);
                }
                else
                {
                    _log?.LogWarning("Exception {0}: {1} doesn't proceed because HTTP headers are already sent",
                        ex.GetType().Name, ex.Message);
                }
            }

        }

        protected virtual async Task HandleResponse(HttpContext context, int status, dynamic body)
        {
            context.Response.StatusCode = status;
            if (context.Response.Body.CanWrite)
            {
                context.Response.ContentType = "application/json";
                using (var writter = new StreamWriter(context.Response.Body))
                {
                    await writter.WriteAsync(JsonConvert.SerializeObject(body, Formatting.None, new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        MaxDepth = 3,
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    }));
                }
            }
            else
            {
                _log?.LogWarning("Response body is not writtable stream, error message dropped.");
            }
        }

        protected virtual dynamic BuildResponseBody(Exception ex)
        {
            dynamic res = new ExpandoObject();
            if (ex is AggregateException)
            {
                var aex = (AggregateException)ex;
                _log?.LogDebug("AggregateException detected, use InternalException to full up Message");
                res.Message = aex.InnerExceptions.FirstOrDefault()?.Message;
            }
            else
            {
                res.Message = ex.Message;
            }

            if (_isDetailsEnabled)
            {
                res.Details = ex;
            }

            return res;
        }

        protected virtual int BuildHttpStatus(Exception ex)
        {
            if (ex is ArgumentException || ex is FormatException)
                return 400; //Bad Request
            if (ex is NotImplementedException)
                return 501; //Not Implemented
            if (ex is SecurityException || ex is UnauthorizedAccessException)
                return 403; //Forbidden
            return 500; //Internal Server Error
        }

        protected virtual bool IsExceptionHandable(HttpContext context, Exception exception)
        {
            return true;
        }
    }
}
