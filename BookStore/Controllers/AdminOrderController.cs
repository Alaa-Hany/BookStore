using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStore.Data;
using BookStore.Models;

namespace BookStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly BookStoreContext _context;

        public AdminOrderController(BookStoreContext context)
        {
            _context = context;
        }

        // عرض جميع الطلبات
        public IActionResult OrdersList()
        {
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderPlaced)
                .ToList();

            return View(orders);
        }

        // ✅ Mark as Shipped (تم الشحن) - بس
        [HttpPost]
        public async Task<IActionResult> MarkAsShipped(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null && !order.IsShipped && !order.IsDelivered)
            {
                order.IsShipped = true;
                order.ShippedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("OrdersList");
        }

        // ✅ Revert to Pending (تراجع)
        [HttpPost]
        public async Task<IActionResult> RevertToPending(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.IsShipped = false;
                order.IsDelivered = false;
                order.ShippedDate = null;
                order.DeliveredDate = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("OrdersList");
        }
    }
}