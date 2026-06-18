using WebMarkupMin.AspNetCore6;

using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebMarkupMin(options =>
{
    options.AllowMinificationInDevelopmentEnvironment = true; // Only minify in production
})
.AddHtmlMinification();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseWebMarkupMin();

app.UseStaticFiles();

// Configure the HTTP request pipeline.

app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=HtmlToPdf}/{action=PdfWithHtml}/{id?}");
app.MapControllers();



app.Run();

