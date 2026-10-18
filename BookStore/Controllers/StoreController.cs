using System;
using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    [AllowAnonymous]
    public class StoreController : Controller
    {
        private readonly BookStoreContext _context;

        public StoreController(BookStoreContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index(string searchString, string minPrice, string maxPrice)
        {
            var books = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString) || b.Author.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(minPrice) && decimal.TryParse(minPrice, out decimal min))
            {
                books = books.Where(b => b.Price >= min);
            }
            if (!string.IsNullOrEmpty(maxPrice) && decimal.TryParse(maxPrice, out decimal max))
            {
                books = books.Where(b => b.Price <= max);
            }

            return View(await books.ToListAsync());
        }
        // AJAX: Add to cart without page refresh
        [HttpPost]
        public async Task<IActionResult> AddToCartAjax(int id)
        {
            // منع الوصول من غير تسجيل دخول
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "Please login first" });

            try
            {
                var session = HttpContext.Session;
                string cartId = session.GetString("Id") ?? Guid.NewGuid().ToString();
                session.SetString("Id", cartId);

                var book = await _context.Books.FindAsync(id);
                if (book == null) return Json(new { success = false, message = "Book not found" });

                var cartItem = _context.CartItems
                    .FirstOrDefault(ci => ci.BookId == id && ci.CartId == cartId);

                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        BookId = book.Id,
                        Quantity = 1,
                        CartId = cartId
                    };
                    _context.CartItems.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity++;
                }

                await _context.SaveChangesAsync();

                var cartCount = _context.CartItems.Where(ci => ci.CartId == cartId).Sum(ci => ci.Quantity);

                return Json(new
                {
                    success = true,
                    message = $"✅ {book.Title} added to cart!",
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();

            return View(book);
        }
    }
}