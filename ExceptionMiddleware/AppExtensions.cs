using Microsoft.Owin.Logging;
using Owin;

namespace Cactus.Owin
{
    public static class AppExtensions
    {
        public static IAppBuilder UseJsonException(this IAppBuilder app, ExceptionMiddlewareConfig config = null)
        {
            if (config == null)
            {
                config = new ExceptionMiddlewareConfig();
            }

            if (config.Log == null)
            {
                config.Log = app.GetLoggerFactory().Create(typeof(JsonExceptionMiddleware).FullName);
            }

            app.Use<JsonExceptionMiddleware>(config);
            return app;
        }
    }
}
