using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Shoes.Models
{
    public class Product
    {
        public string Article { get; set; }
        public string Name { get; set; }
        public int UnitId { get; set; }
        public int SupplierId { get; set; }
        public int ManufacturerId { get; set; }
        public int CategoryId { get; set; }

        public string UnitName { get; set; }
        public string SupplierName { get; set; }
        public string ManufacturerName { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public int StockQuantity { get; set; }
        public string Description { get; set; }
        public string PhotoPath { get; set; }
        public decimal FinalPrice
        {
            get
            {
                if (Discount > 0)
                    return Price - Price * Discount / 100m;
                return Price;
            }
        }
        public string PriceText
        {
            get
            {
                return Discount > 0
                    ? " " + FinalPrice.ToString("F2") + " \u20BD"   // ₽
                    : " " + Price.ToString("F2") + " \u20BD";
            }
        }

        public string OldPriceText
        {
            get
            {
                return Discount > 0 ? Price.ToString("F2") + " \u20BD" : "";
            }
        }

        public Brush RowBrush { get; set; } = Brushes.White;
        public string PhotoFullPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PhotoPath))
                    return "pack://application:,,,/Resources/picture.png";
                return "pack://siteoforigin:,,,/Images/" + PhotoPath;
            }
        }
    }
}