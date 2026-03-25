using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;
using System.Text.Json;

namespace MyAspNetApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Shop
        public IActionResult Shop()
        {
            var q = Request.Query["q"].ToString();
            var label = Request.Query["label"].ToString();

            if (string.Equals(q, "challenge", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(label, "challenges", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Challenges));
            }

            return View();
        }

        // GET: /Home/Challenges
        public async Task<IActionResult> Challenges()
        {
            var now = DateTime.Now;

            var rawChallenges = await _db.Challenges
                .AsNoTracking()
                .OrderBy(c => c.StartDate)
                .ToListAsync();

            var allCards = rawChallenges.Select(ToChallengeCard).ToList();

            var active = allCards
                .Where(c =>
                    string.Equals(c.Status, "active", StringComparison.OrdinalIgnoreCase) ||
                    (c.StartDate <= now && c.EndDate >= now))
                .OrderBy(c => c.EndDate)
                .ToList();

            var upcoming = allCards
                .Where(c => c.StartDate > now || string.Equals(c.Status, "upcoming", StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.StartDate)
                .ToList();

            var featured = active.FirstOrDefault() ?? upcoming.FirstOrDefault();
            var nextDropDate = upcoming.FirstOrDefault()?.StartDate;
            var fallbackChallenges = allCards
                .Where(c => featured == null || c.ChallengeId != featured.ChallengeId)
                .OrderBy(c => c.StartDate)
                .Take(3)
                .ToList();

            var vm = new ChallengesPageViewModel
            {
                SeasonLabel = $"{now.Year} Season",
                ActiveCount = active.Count,
                TotalParticipants = active.Sum(c => c.TotalParticipants),
                TotalGoalKm = active.Sum(c => c.GoalKm),
                DaysUntilNextDrop = nextDropDate.HasValue ? Math.Max(0, (nextDropDate.Value.Date - now.Date).Days) : 0,
                FeaturedChallenge = featured,
                UpcomingChallenges = upcoming.Any() ? upcoming.Take(3).ToList() : fallbackChallenges,
                TopChallenges = allCards
                    .OrderByDescending(c => c.CompletionPercent)
                    .ThenByDescending(c => c.TotalParticipants)
                    .Take(5)
                    .ToList()
            };

            return View(vm);
        }

        // GET: /Home/ChallengeBanner/5
        [HttpGet]
        public async Task<IActionResult> ChallengeBanner(int id)
        {
            var challenge = await _db.Challenges
                .AsNoTracking()
                .Where(c => c.ChallengeId == id)
                .Select(c => new
                {
                    c.BannerImage,
                    c.BannerImageContentType
                })
                .FirstOrDefaultAsync();

            if (challenge?.BannerImage == null || challenge.BannerImage.Length == 0)
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(challenge.BannerImageContentType)
                ? "application/octet-stream"
                : challenge.BannerImageContentType;

            return File(challenge.BannerImage, contentType);
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

        // GET: /Home/Reviews?id=5
        public IActionResult Reviews(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/WriteReview?id=5
        public IActionResult WriteReview(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/Cart
        public IActionResult Cart()
        {
            return View();
        }

        // GET: /Home/Checkout
        public IActionResult Checkout()
        {
            if (!ProductData.Cart.Any())
                return RedirectToAction("Cart");

            var vm = BuildCheckoutVm();
            return View(vm);
        }

        // POST: /Home/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel vm)
        {
            // Always re-populate cart items from server data
            vm.CartItems = GetCheckoutItems();
            vm.Subtotal = vm.CartItems.Sum(i => i.Price * i.Quantity);
            vm.ShippingFee = vm.DeliveryOption == "Express" ? 300m : 150m;

            ModelState.Remove("CartItems");
            ModelState.Remove("Subtotal");
            ModelState.Remove("ShippingFee");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CardExpiry");
            ModelState.Remove("CardCvv");

            if (!ModelState.IsValid)
                return View("Checkout", vm);

            foreach (var item in vm.CartItems)
            {
                ProductData.PurchaseRecords.Add(new PurchaseRecord
                {
                    ProductId = item.ProductId,
                    PurchaseDate = DateTime.Now,
                    DeliveryDate = DateTime.Now.AddDays(vm.DeliveryOption == "Express" ? 2 : 5)
                });
            }

            var confirmVm = new OrderConfirmationViewModel
            {
                OrderId = $"ORD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                OrderStatus = "Placed",
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                City = vm.City,
                PostalCode = vm.PostalCode,
                DeliveryOption = vm.DeliveryOption,
                PaymentMethod = vm.PaymentMethod,
                CreatedAt = DateTime.Now,
                EstimatedDeliveryDate = DateTime.Now.AddDays(vm.DeliveryOption == "Express" ? 2 : 5),
                OrderItems = vm.CartItems,
                Subtotal = vm.Subtotal,
                ShippingFee = vm.ShippingFee,
            };

            ProductData.Orders.Insert(0, confirmVm);
            ProductData.Cart.Clear();

            TempData["OrderConfirmation"] = JsonSerializer.Serialize(confirmVm);
            return RedirectToAction("OrderConfirmation");
        }

        // GET: /Home/OrderConfirmation
        public IActionResult OrderConfirmation()
        {
            if (TempData["OrderConfirmation"] is not string json)
                return RedirectToAction("Cart");

            var vm = JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
            if (vm == null)
                return RedirectToAction("Cart");

            // Keep so OrderDetail can also read it
            TempData.Keep("OrderConfirmation");
            return View(vm);
        }

        // GET: /Home/MyOrders
        public IActionResult MyOrders()
        {
            return View(ProductData.Orders);
        }

        // GET: /Home/OrderDetail?id=ORD-...
        public IActionResult OrderDetail(string? id)
        {
            OrderConfirmationViewModel? vm = null;

            if (!string.IsNullOrEmpty(id))
                vm = ProductData.Orders.FirstOrDefault(o => o.OrderId == id);

            if (vm == null)
            {
                if (TempData["OrderConfirmation"] is string json)
                    vm = System.Text.Json.JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
            }

            if (vm == null)
                return RedirectToAction("MyOrders");

            return View(vm);
        }

        private List<CheckoutItem> GetCheckoutItems()
        {
            return ProductData.Cart.Select(ci =>
            {
                var product = ProductData.Products.FirstOrDefault(p => p.Id == ci.ProductId);
                return new CheckoutItem
                {
                    ProductId = ci.ProductId,
                    Name = product?.Name ?? "Unknown",
                    Image = product?.Image ?? "",
                    Size = ci.Size,
                    Price = product?.Price ?? 0,
                    Quantity = ci.Quantity
                };
            }).Where(i => i.Price > 0).ToList();
        }

        private CheckoutViewModel BuildCheckoutVm()
        {
            var items = GetCheckoutItems();
            return new CheckoutViewModel
            {
                CartItems = items,
                Subtotal = items.Sum(i => i.Price * i.Quantity),
                ShippingFee = 150m,
            };
        }

        // GET: /Home/Wishlist
        public IActionResult Wishlist()
        {
            return View();
        }

        // GET: /Home/SellerShop?id=1
        public IActionResult SellerShop(int id)
        {
            ViewData["SellerId"] = id;
            return View();
        }

        // GET: /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            return View(feature?.Error);
        }

        private static ChallengeCardViewModel ToChallengeCard(DbChallenge challenge)
        {
            var totalParticipants = challenge.TotalParticipants ?? 0;
            var totalCompleted = challenge.TotalCompleted ?? 0;
            var completionPercent = totalParticipants > 0
                ? Math.Round((double)totalCompleted / totalParticipants * 100, 0)
                : 0;
            var durationDays = Math.Max(1, (challenge.EndDate.Date - challenge.StartDate.Date).Days + 1);

            var (difficultyLabel, difficultyCssClass) = challenge.GoalKm switch
            {
                <= 10m => ("Easy", "diff-easy"),
                <= 30m => ("Medium", "diff-med"),
                _ => ("Hard", "diff-hard")
            };

            return new ChallengeCardViewModel
            {
                ChallengeId = challenge.ChallengeId,
                Title = challenge.Title,
                Description = challenge.Description,
                Rules = challenge.Rules,
                Prizes = challenge.Prizes,
                GoalKm = challenge.GoalKm,
                ActivityType = challenge.ActivityType,
                StartDate = challenge.StartDate,
                EndDate = challenge.EndDate,
                Status = challenge.Status ?? string.Empty,
                TotalParticipants = totalParticipants,
                TotalCompleted = totalCompleted,
                CompletionPercent = completionPercent,
                DurationDays = durationDays,
                DifficultyLabel = difficultyLabel,
                DifficultyCssClass = difficultyCssClass,
                HasBannerImage = challenge.BannerImage != null && challenge.BannerImage.Length > 0
            };
        }
    }
}
