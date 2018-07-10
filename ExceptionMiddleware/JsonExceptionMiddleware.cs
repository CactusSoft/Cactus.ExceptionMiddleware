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
using Newtonsoft.Json.Serialization;

namespace Cactus.Owin
{
    public class JsonExceptionMiddleware : OwinMiddleware
    {
        private readonly ILogger _log;
        private readonly bool _isDetailsEnabled;
        private readonly IContractResolver _contractResolver;

        public JsonExceptionMiddleware(OwinMiddleware next, ExceptionMiddlewareConfig config)
            : base(next)
        {
            _log = config.Log;
            _isDetailsEnabled = config.EnableDetails;
            _contractResolver = config.ContractResolver ?? new DefaultContractResolver();
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
                if (_log.IsEnabled(TraceEventType.Warning))
                    _log.WriteWarning("Exception catched during process {0} {1} from IP {2}: {3}", context.Request.Method, context.Request.Uri.ToString(), context.Request.RemoteIpAddress, ex.ToString());

                if (!isTooLate)
                {
                    if (!IsExceptionHandable(context, ex))
                    {
                        _log.WriteVerbose("Exception throwed up by pipline, cause of IsExceptionHandable returned false");
                        throw;
                    }

                    var status = BuildHttpStatus(ex);
                    var body = BuildResponseBody(ex);
                    await HandleResponse(context, status, body);

                    if (_log.IsEnabled(TraceEventType.Verbose))
                    {
                        _log.WriteVerbose($"{ex.GetType().Name}: {ex.Message} converted to HTTP {status}");
                    }
                }
                else
                {
                    if (_log.IsEnabled(TraceEventType.Warning))
                    {
                        _log.WriteWarning($"Exception {ex.GetType().Name}: {ex.Message} doesn't proceed because HTTP headers are already sent");
                    }
                }
            }

        }

        protected virtual async Task HandleResponse(IOwinContext context, int status, ExceptionResponse body)
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
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        ContractResolver = _contractResolver
                    }));
                }
            }
            else
            {
                _log.WriteWarning("Response body is not writtable stream, error message dropped.");
            }
        }

        protected virtual ExceptionResponse BuildResponseBody(Exception ex)
        {
            var res = new ExceptionResponse(ex.Message);
            if (ex is AggregateException aex)
            {
                _log.WriteVerbose("AggregateException detected, use InternalException to full up Message");
                res.Message = aex.InnerExceptions.FirstOrDefault()?.Message;
            }

            if (_isDetailsEnabled)
            {
                res.Details = ex.ToString();
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
