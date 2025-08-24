using APP.PDF.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using PuppeteerSharp.Media;
using static System.Net.Mime.MediaTypeNames;

namespace APP.PDF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PDFController : ControllerBase
    {
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePdf([FromBody] HTMLRequest request)
        {
            try
            {
                // Download Chromium if not already present
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
                });


                using var page = await browser.NewPageAsync();
                await page.SetContentAsync(request.Html);

                using var pdfStream = await page.PdfStreamAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true
                });

                using var ms = new MemoryStream();
                await pdfStream.CopyToAsync(ms);

                return File(ms.ToArray(), "application/pdf", "document.pdf");
            }
            catch (Exception ex)
            {
               return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message+ex.StackTrace?.ToString(),
                    Details = ex.Message
                });
            }
        }

        [HttpPost("generate-from-url")]
        public async Task<IActionResult> GeneratePdfFromUrl([FromBody] UrlRequest request)
        {
            try
            {
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
                });
                using var page = await browser.NewPageAsync();
                await page.GoToAsync(request.Url, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } // only wait for HTML+JS
                });
             
                await Task.Delay(1000);

                using var pdfStream = await page.PdfStreamAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true
                });
                using var ms = new MemoryStream();
                await pdfStream.CopyToAsync(ms);
                return File(ms.ToArray(), "application/pdf", "document.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = ex.Message + ex.StackTrace?.ToString(),
                    Details = ex.Message
                });
            }
        }
    }
}
