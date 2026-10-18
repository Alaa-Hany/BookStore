using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book Book { get; set; }

        public int Quantity { get; set; }
        public string CartId { get; set; }
    }
}