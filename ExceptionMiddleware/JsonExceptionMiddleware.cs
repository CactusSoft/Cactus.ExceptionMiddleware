using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Newtonsoft.Json;

namespace Cactus.Owin
{
    public class JsonExceptionMiddleware : OwinMiddleware
    {
        private readonly ILogger log;
        private readonly bool isDetailsEnabled;

        public JsonExceptionMiddleware(OwinMiddleware next, ExceptionMiddlewareConfig config)
            : base(next)
        {
            log = config.Log;
            isDetailsEnabled = config.EnableDetails;
        }

        public override async Task Invoke(IOwinContext context)
        {
            var isTooLate = false;
            context.Response.OnSendingHeaders(_ => { isTooLate = true; }, null);
            try
            {
                await Next.Invoke(context);
            }

            catch (Exception ex)
            {
                if (!isTooLate)
                {
                    if (!IsExceptionHandable(context, ex))
                    {
                        log.WriteVerbose("Exception throwed up by pipline, cause of IsExceptionHandable returned false");
                        throw;
                    }

                    var status = BuildHttpStatus(ex);
                    var body = BuildResponseBody(ex);
                    await HandleResponse(context, status, body);

                    if (log.IsEnabled(TraceEventType.Verbose))
                    {
                        log.WriteVerbose($"{ex.GetType().Name}: {ex.Message} converted to HTTP {status}");
                    }
                }
                else
                {
                    if (log.IsEnabled(TraceEventType.Warning))
                    {
                        log.WriteWarning($"Exception {ex.GetType().Name}: {ex.Message} doesn't proceed because HTTP headers are already sent");
                    }
                }
            }

        }

        protected virtual async Task HandleResponse(IOwinContext context, int status, dynamic body)
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
                log.WriteWarning("Response body is not writtable stream, error message dropped.");
            }
        }

        protected virtual dynamic BuildResponseBody(Exception ex)
        {
            dynamic res = new ExpandoObject();
            if (ex is AggregateException)
            {
                var aex = (AggregateException)ex;
                log.WriteVerbose("AggregateException detected, use InternalException to full up Message");
                res.Message = aex.InnerExceptions.FirstOrDefault()?.Message;
            }
            else
            {
                res.Message = ex.Message;
            }

            if (isDetailsEnabled)
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

        protected virtual bool IsExceptionHandable(IOwinContext context, Exception exception)
        {
            return true;
        }
    }
}
