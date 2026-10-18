using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using BookStore.Data;
using BookStore.Models;

namespace BookStore.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly BookStoreContext _context;
        private readonly UserManager<DefaultUser> _userManager;

        public FavoritesController(BookStoreContext context, UserManager<DefaultUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // AJAX: Add to favorites without page refresh
        [HttpPost]
        public async Task<IActionResult> AddToFavoritesAjax(int bookId)
        {
            Console.WriteLine($"AddToFavoritesAjax called with bookId: {bookId}");

            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                    return Json(new { success = false, message = "Book not found" });

                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.BookId == bookId && f.UserId == userId);

                if (existingFavorite == null)
                {
                    var favorite = new Favorite
                    {
                        UserId = userId,
                        BookId = bookId
                    };
                    _context.Favorites.Add(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"❤️ {book.Title} added to favorites!" });
                }
                else
                {
                    return Json(new { success = false, message = $"⚠️ {book.Title} already in favorites" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // AJAX: Remove from favorites without page refresh
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavoritesAjax(int id)
        {
            try
            {
                var favorite = await _context.Favorites.FindAsync(id);
                if (favorite != null)
                {
                    _context.Favorites.Remove(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "✅ Book removed from favorites!" });
                }
                return Json(new { success = false, message = "Favorite not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Regular AddToFavorites (for fallback)
        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int bookId)
        {
            var userId = _userManager.GetUserId(User);
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.BookId == bookId && f.UserId == userId);

            if (existingFavorite == null)
            {
                var favorite = new Favorite
                {
                    UserId = userId,
                    BookId = bookId
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "✅ Book added to favorites!";
            }
            else
            {
                TempData["InfoMessage"] = "📚 Book is already in your favorites.";
            }

            return RedirectToAction("Index", "Store");
        }

        // Regular RemoveFromFavorites (for fallback)
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int favoriteId)
        {
            var favorite = await _context.Favorites.FindAsync(favoriteId);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "✅ Book removed from favorites!";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .ToListAsync();

            return View(favorites);
        }
    }
}