using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace BookStore.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly BookStoreContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(BookStoreContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCartId()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string cartId = session.GetString("Id") ?? Guid.NewGuid().ToString();
            session.SetString("Id", cartId);
            return cartId;
        }

        public IActionResult Index()
        {
            string cartId = GetCartId();

            var items = _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .Include(ci => ci.Book)
                .ToList();

            var cart = new Cart
            {
                Id = cartId,
                CartItems = items
            };

            ViewBag.Total = items.Sum(ci => ci.Book.Price * ci.Quantity);
            return View(cart);
        }

        // AJAX: Increase quantity without page refresh
        [HttpPost]
        public async Task<IActionResult> IncreaseQuantityAjax(int id)
        {
            string cartId = GetCartId();
            var cartItem = _context.CartItems
                .Include(ci => ci.Book)
                .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

            if (cartItem != null)
            {
                cartItem.Quantity++;
                await _context.SaveChangesAsync();

                var cartTotal = _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .Sum(ci => ci.Book.Price * ci.Quantity);

                return Json(new
                {
                    success = true,
                    newQuantity = cartItem.Quantity,
                    newItemTotal = cartItem.Book.Price * cartItem.Quantity,
                    cartTotal = cartTotal
                });
            }
            return Json(new { success = false });
        }

        // AJAX: Decrease quantity without page refresh
        [HttpPost]
        public async Task<IActionResult> DecreaseQuantityAjax(int id)
        {
            string cartId = GetCartId();
            var cartItem = _context.CartItems
                .Include(ci => ci.Book)
                .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

            if (cartItem != null)
            {
                bool itemRemoved = false;

                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    itemRemoved = true;
                }

                var cartTotal = _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .Sum(ci => ci.Book.Price * ci.Quantity);

                if (itemRemoved)
                {
                    var itemCount = _context.CartItems.Count(ci => ci.CartId == cartId);
                    return Json(new
                    {
                        success = true,
                        itemRemoved = true,
                        cartTotal = cartTotal,
                        itemCount = itemCount
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        itemRemoved = false,
                        newQuantity = cartItem.Quantity,
                        newItemTotal = cartItem.Book.Price * cartItem.Quantity,
                        cartTotal = cartTotal
                    });
                }
            }
            return Json(new { success = false });
        }

        // ✅ AJAX: Remove item without page refresh
        [HttpPost]
        public async Task<IActionResult> RemoveFromCartAjax(int id)
        {
            string cartId = GetCartId();
            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var cartTotal = _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .Sum(ci => ci.Book.Price * ci.Quantity);
                var itemCount = _context.CartItems.Count(ci => ci.CartId == cartId);

                return Json(new { success = true, cartTotal = cartTotal, itemCount = itemCount });
            }
            return Json(new { success = false });
        }

        // Regular methods (for fallback)
        public IActionResult AddToCart(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null) return NotFound();

            string cartId = GetCartId();
            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.BookId == book.Id && ci.CartId == cartId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    BookId = book.Id,
                    Book = book,
                    Quantity = 1,
                    CartId = cartId
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity++;
            }

            _context.SaveChanges();
            return RedirectToAction("Index", "Store");
        }

        public IActionResult RemoveFromCart(int id)
        {
            string cartId = GetCartId();

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult ReduceQuantity(int id)
        {
            string cartId = GetCartId();

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                }
                else
                {
                    _context.CartItems.Remove(cartItem);
                }

                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult IncreaseQuantity(int id)
        {
            string cartId = GetCartId();

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

            if (cartItem != null)
            {
                cartItem.Quantity++;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult ClearCart()
        {
            string cartId = GetCartId();

            var cartItems = _context.CartItems
                .Where(ci => ci.CartId == cartId);

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}