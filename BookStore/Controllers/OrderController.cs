using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly BookStoreContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<DefaultUser> _userManager;

        public OrderController(BookStoreContext context, IHttpContextAccessor httpContextAccessor, UserManager<DefaultUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        private string GetCartId()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string cartId = session.GetString("Id") ?? Guid.NewGuid().ToString();
            session.SetString("Id", cartId);
            return cartId;
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            string cartId = GetCartId();

            var cartItems = _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .Include(ci => ci.Book)
                .ToList();

            if (cartItems.Count == 0)
            {
                ModelState.AddModelError("", "Cart is empty, please add a book first.");
                return View(order);
            }

            if (ModelState.IsValid)
            {
                // ✅ الحل: جلب بيانات المستخدم الحالي
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    order.UserId = user.Id;
                    order.UserEmail = user.Email ?? User.Identity?.Name;
                    order.UserFullName = $"{user.FirstName} {user.LastName}".Trim();
                    if (string.IsNullOrEmpty(order.UserFullName))
                    {
                        order.UserFullName = user.UserName ?? "Customer";
                    }
                }
                else
                {
                    order.UserId = "Unknown";
                    order.UserEmail = User.Identity?.Name ?? "Unknown";
                    order.UserFullName = "Unknown Customer";
                }

                order.OrderPlaced = DateTime.Now;
                order.OrderItems = new List<OrderItem>();
                order.OrderTotal = 0;
                order.IsDelivered = false;
                order.DeliveredDate = null;

                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        Quantity = item.Quantity,
                        BookId = item.Book.Id,
                        Price = item.Book.Price * item.Quantity
                    };

                    order.OrderItems.Add(orderItem);
                    order.OrderTotal += orderItem.Price;
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Clear the cart after checkout
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return View("CheckoutComplete", order);
            }

            return View(order);
        }
        public IActionResult CheckoutComplete(Order order)
        {
            return View(order);
        }
        // GET: My Orders for logged-in user
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderPlaced)
                .ToListAsync();

            return View(orders);
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order != null && order.IsShipped && !order.IsDelivered)
            {
                order.IsDelivered = true;
                order.DeliveredDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyOrders");
        }
    }
}