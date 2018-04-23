using Microsoft.AspNetCore.Builder;

namespace Cactus.Aspnetcore.ExceptionMiddleware
{
    public static class AppExtensions
    {
        public static IApplicationBuilder UseJsonException(this IApplicationBuilder app, ExceptionMiddlewareConfig config = null)
        {
            if (config == null)
            {
                config = new ExceptionMiddlewareConfig();
            }

            app.UseMiddleware<JsonExceptionMiddleware>(config);
            return app;
        }
    }
}
