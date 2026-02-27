using Microsoft.AspNetCore.Mvc;

namespace MyAspNetApp.Controllers
{
    public class HomeController : Controller
    {
        // GET: /
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Shop
        public IActionResult Shop()
        {
            return View();
        }

        // GET: /Home/About
        public IActionResult About()
        {
            return View();
        }

        // GET: /Home/Product?id=5
        public IActionResult Product(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/Contact
        public IActionResult Contact()
        {
            return View();
        }

        // GET: /Home/Cart
        public IActionResult Cart()
        {
            return View();
        }

        // GET: /Home/Wishlist
        public IActionResult Wishlist()
        {
            return View();
        }

        // GET: /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
