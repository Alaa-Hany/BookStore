using BookStore.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Models
{
    public class Cart
    {
        public string Id { get; set; }
        public List<CartItem> CartItems { get; set; }
    }
}