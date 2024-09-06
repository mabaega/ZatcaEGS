namespace ZatcaEGS.Extensions
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
            // Periksa apakah pengguna sedang mengakses halaman wizard
            if (context.Request.Path.StartsWithSegments("/Wizard") &&
                context.Request.Cookies["disclaimerSeen"] != "true")
            {
                context.Response.Redirect("/Disclaimer");
                return;
            }

            await _next(context);
        }
    }
}