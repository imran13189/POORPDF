using APP.PDF.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using WebMarkupMin.AspNetCore6;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebMarkupMin(options =>
{
    options.AllowMinificationInDevelopmentEnvironment = true; // Only minify in production
})
.AddHtmlMinification();

// 1. Register Queue (Singleton, shared across all requests)
builder.Services.AddSingleton<IRequestLogQueue, RequestLogQueue>();

// 2. Register Background Consumer (Hosted Service)
builder.Services.AddHostedService<RequestLogConsumerService>();
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
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=HtmlToPdf}/{action=PdfWithHtml}/{id?}");
app.MapControllers();



app.Run();

