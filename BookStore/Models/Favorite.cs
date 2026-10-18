using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book Book { get; set; }
    }
}