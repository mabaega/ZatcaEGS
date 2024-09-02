namespace Zatca.EGS.Helpers
{
    public class DisclaimerMiddleware
    {
        private readonly RequestDelegate _next;

        public DisclaimerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the user is accessing the wizard page
            if (context.Request.Path.StartsWithSegments("/Wizard") &&
                context.Session.GetString("disclaimerSeen") != "true")
            {
                context.Response.Redirect("/Disclaimer");
                return;
            }

            await _next(context);
        }
    }
}