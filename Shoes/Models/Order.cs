using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shoes.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int PickupPointId { get; set; }
        public string PickupAddress { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public string PickupCode { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string ArticlesDisplay { get; set; }

        public string OrderDateText => OrderDate.ToString("dd.MM.yyyy");
        public string DeliveryDateText => DeliveryDate.ToString("dd.MM.yyyy");
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProductArticle { get; set; }
        public int Quantity { get; set; }
    }
}
