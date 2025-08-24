


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.

app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=HtmlToPdf}/{action=PdfWithHtml}/{id?}");
app.MapControllers();



app.Run();

