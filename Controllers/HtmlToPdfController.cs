using Microsoft.AspNetCore.Mvc;

namespace APP.PDF.Controllers
{
    public class HtmlToPdfController : Controller
    {
        public IActionResult CovertPDF()
        {
            return View("Index");
        }

        public IActionResult PdfWithHtml()
        {
            ViewBag.ActiveTab = "Html";
            return View();
        }

        public IActionResult PdfWithUrl()
        {
            ViewBag.ActiveTab = "Url";
            return View();
        }
    }
}
