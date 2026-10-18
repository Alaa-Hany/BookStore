using System;
using System.Collections.Generic;

namespace BookStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
        public int OrderTotal { get; set; }
        public DateTime OrderPlaced { get; set; }

        // معلومات المستخدم
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserFullName { get; set; }

        // حالة الطلب
        public bool IsDelivered { get; set; } = false;
        public DateTime? DeliveredDate { get; set; }

        // حالة الشحن
        public bool IsShipped { get; set; } = false;
        public DateTime? ShippedDate { get; set; }
    }
}