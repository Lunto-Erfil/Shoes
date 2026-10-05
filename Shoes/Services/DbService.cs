using Shoes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using System.Threading.Tasks;
using System.Configuration;
using NLog.Internal;

namespace Shoes.Services
{
    public static class DbService
    {
        private static string Conn
        {
            get { return ConfigurationManager.ConnectionStrings["ShoesDB"].ConnectionString; }
        }

        public static User Authenticate(string login, string password)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT u.id, u.last_name, u.first_name,
                           COALESCE(u.middle_name,''), u.login, r.name
                    FROM users u
                    JOIN roles r ON r.id = u.role_id
                    WHERE u.login = @l AND u.password = @p", cn))
                {
                    cmd.Parameters.AddWithValue("l", login);
                    cmd.Parameters.AddWithValue("p", password);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new User
                        {
                            Id = r.GetInt32(0),
                            LastName = r.GetString(1),
                            FirstName = r.GetString(2),
                            MiddleName = r.GetString(3),
                            Login = r.GetString(4),
                            RoleName = r.GetString(5)
                        };
                    }
                }
            }
        }
        public static List<Product> GetProducts()
        {
            var list = new List<Product>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT p.article, p.name,
                           p.unit_id, u.name,
                           p.price,
                           p.supplier_id, s.name,
                           p.manufacturer_id, m.name,
                           p.category_id, c.name,
                           p.discount, p.stock_quantity,
                           COALESCE(p.description,''), COALESCE(p.photo_path,'')
                    FROM products p
                    JOIN units         u ON u.id = p.unit_id
                    JOIN suppliers     s ON s.id = p.supplier_id
                    JOIN manufacturers m ON m.id = p.manufacturer_id
                    JOIN categories    c ON c.id = p.category_id
                    ORDER BY p.article", cn))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new Product
                            {
                                Article = r.GetString(0),
                                Name = r.GetString(1),
                                UnitId = r.GetInt32(2),
                                UnitName = r.GetString(3),
                                Price = r.GetDecimal(4),
                                SupplierId = r.GetInt32(5),
                                SupplierName = r.GetString(6),
                                ManufacturerId = r.GetInt32(7),
                                ManufacturerName = r.GetString(8),
                                CategoryId = r.GetInt32(9),
                                CategoryName = r.GetString(10),
                                Discount = r.GetInt32(11),
                                StockQuantity = r.GetInt32(12),
                                Description = r.GetString(13),
                                PhotoPath = r.GetString(14)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static void AddProduct(Product p)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO products
                      (article, name, unit_id, price, supplier_id, manufacturer_id,
                       category_id, discount, stock_quantity, description, photo_path)
                    VALUES
                      (@a, @n, @u, @pr, @s, @m, @c, @d, @q, @ds, @ph)", cn))
                {
                    cmd.Parameters.AddWithValue("a", p.Article);
                    cmd.Parameters.AddWithValue("n", p.Name);
                    cmd.Parameters.AddWithValue("u", p.UnitId);
                    cmd.Parameters.AddWithValue("pr", p.Price);
                    cmd.Parameters.AddWithValue("s", p.SupplierId);
                    cmd.Parameters.AddWithValue("m", p.ManufacturerId);
                    cmd.Parameters.AddWithValue("c", p.CategoryId);
                    cmd.Parameters.AddWithValue("d", p.Discount);
                    cmd.Parameters.AddWithValue("q", p.StockQuantity);
                    cmd.Parameters.AddWithValue("ds", (object)p.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("ph", (object)p.PhotoPath ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateProduct(Product p, string oldArticle)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    UPDATE products SET
                        article         = @a,
                        name            = @n,
                        unit_id         = @u,
                        price           = @pr,
                        supplier_id     = @s,
                        manufacturer_id = @m,
                        category_id     = @c,
                        discount        = @d,
                        stock_quantity  = @q,
                        description     = @ds,
                        photo_path      = @ph
                    WHERE article = @old", cn))
                {
                    cmd.Parameters.AddWithValue("a", p.Article);
                    cmd.Parameters.AddWithValue("n", p.Name);
                    cmd.Parameters.AddWithValue("u", p.UnitId);
                    cmd.Parameters.AddWithValue("pr", p.Price);
                    cmd.Parameters.AddWithValue("s", p.SupplierId);
                    cmd.Parameters.AddWithValue("m", p.ManufacturerId);
                    cmd.Parameters.AddWithValue("c", p.CategoryId);
                    cmd.Parameters.AddWithValue("d", p.Discount);
                    cmd.Parameters.AddWithValue("q", p.StockQuantity);
                    cmd.Parameters.AddWithValue("ds", (object)p.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("ph", (object)p.PhotoPath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("old", oldArticle);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static bool IsProductInOrders(string article)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM order_items WHERE product_article = @a", cn))
                {
                    cmd.Parameters.AddWithValue("a", article);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public static void DeleteProduct(string article)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM products WHERE article = @a", cn))
                {
                    cmd.Parameters.AddWithValue("a", article);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GenerateNextArticle()
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                var candidate = string.Format("{0}{1}{2}{3}",
                    letters[rnd.Next(letters.Length)],
                    rnd.Next(100, 999),
                    letters[rnd.Next(letters.Length)],
                    rnd.Next(1, 9));

                using (var cn = new NpgsqlConnection(Conn))
                {
                    cn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM products WHERE article=@a", cn))
                    {
                        cmd.Parameters.AddWithValue("a", candidate);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                            return candidate;
                    }
                }
            }
            throw new InvalidOperationException("Не удалось сгенерировать артикул.");
        }
        public static List<Tuple<int, string>> GetCategories()
        {
            return GetRef("categories");
        }
        public static List<Tuple<int, string>> GetManufacturers()
        {
            return GetRef("manufacturers");
        }
        public static List<Tuple<int, string>> GetSuppliers()
        {
            return GetRef("suppliers");
        }
        public static List<Tuple<int, string>> GetUnits()
        {
            return GetRef("units");
        }
        public static List<Tuple<int, string>> GetOrderStatuses()
        {
            return GetRef("order_statuses");
        }

        public static List<Tuple<int, string>> GetPickupPoints()
        {
            var list = new List<Tuple<int, string>>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, address FROM pickup_points ORDER BY id", cn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(Tuple.Create(r.GetInt32(0), r.GetString(1)));
                }
            }
            return list;
        }

        private static List<Tuple<int, string>> GetRef(string table)
        {
            var list = new List<Tuple<int, string>>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, name FROM " + table + " ORDER BY name", cn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(Tuple.Create(r.GetInt32(0), r.GetString(1)));
                }
            }
            return list;
        }
        public static List<Order> GetOrders()
        {
            var list = new List<Order>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT o.id, o.order_date, o.delivery_date,
                           o.pickup_point_id, pp.address,
                           o.user_id,
                           u.last_name || ' ' || u.first_name ||
                             COALESCE(' ' || u.middle_name,'') AS fio,
                           o.pickup_code,
                           o.status_id, os.name
                    FROM orders o
                    JOIN pickup_points  pp ON pp.id = o.pickup_point_id
                    JOIN users          u  ON u.id  = o.user_id
                    JOIN order_statuses os ON os.id = o.status_id
                    ORDER BY o.id", cn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Order
                        {
                            Id = r.GetInt32(0),
                            OrderDate = r.GetDateTime(1),
                            DeliveryDate = r.GetDateTime(2),
                            PickupPointId = r.GetInt32(3),
                            PickupAddress = r.GetString(4),
                            UserId = r.GetInt32(5),
                            UserFullName = r.GetString(6),
                            PickupCode = r.GetString(7),
                            StatusId = r.GetInt32(8),
                            StatusName = r.GetString(9)
                        });
                    }
                }
            }
            foreach (var o in list)
                o.ArticlesDisplay = GetArticlesDisplay(o.Id);
            return list;
        }

        private static string GetArticlesDisplay(int orderId)
        {
            var parts = new List<string>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT product_article, quantity
                    FROM order_items WHERE order_id = @id ORDER BY id", cn))
                {
                    cmd.Parameters.AddWithValue("id", orderId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            parts.Add(r.GetString(0) + " x" + r.GetInt32(1));
                    }
                }
            }
            return string.Join(", ", parts);
        }

        public static List<OrderItem> GetOrderItems(int orderId)
        {
            var list = new List<OrderItem>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT id, order_id, product_article, quantity
                    FROM order_items WHERE order_id = @id ORDER BY id", cn))
                {
                    cmd.Parameters.AddWithValue("id", orderId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new OrderItem
                            {
                                Id = r.GetInt32(0),
                                OrderId = r.GetInt32(1),
                                ProductArticle = r.GetString(2),
                                Quantity = r.GetInt32(3)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static int AddOrder(Order o, List<OrderItem> items)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO orders
                          (order_date, delivery_date, pickup_point_id,
                           user_id, pickup_code, status_id)
                        VALUES (@od, @dd, @pp, @u, @pc, @st)
                        RETURNING id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("od", o.OrderDate);
                        cmd.Parameters.AddWithValue("dd", o.DeliveryDate);
                        cmd.Parameters.AddWithValue("pp", o.PickupPointId);
                        cmd.Parameters.AddWithValue("u", o.UserId);
                        cmd.Parameters.AddWithValue("pc", o.PickupCode);
                        cmd.Parameters.AddWithValue("st", o.StatusId);
                        o.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var it in items)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO order_items(order_id, product_article, quantity)
                            VALUES (@o, @a, @q)", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("o", o.Id);
                            cmd.Parameters.AddWithValue("a", it.ProductArticle);
                            cmd.Parameters.AddWithValue("q", it.Quantity);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
            return o.Id;
        }

        public static void UpdateOrder(Order o, List<OrderItem> items)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    using (var cmd = new NpgsqlCommand(@"
                        UPDATE orders SET
                            order_date      = @od,
                            delivery_date   = @dd,
                            pickup_point_id = @pp,
                            user_id         = @u,
                            pickup_code     = @pc,
                            status_id       = @st
                        WHERE id = @id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("od", o.OrderDate);
                        cmd.Parameters.AddWithValue("dd", o.DeliveryDate);
                        cmd.Parameters.AddWithValue("pp", o.PickupPointId);
                        cmd.Parameters.AddWithValue("u", o.UserId);
                        cmd.Parameters.AddWithValue("pc", o.PickupCode);
                        cmd.Parameters.AddWithValue("st", o.StatusId);
                        cmd.Parameters.AddWithValue("id", o.Id);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new NpgsqlCommand(
                        "DELETE FROM order_items WHERE order_id = @id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("id", o.Id);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (var it in items)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO order_items(order_id, product_article, quantity)
                            VALUES (@o, @a, @q)", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("o", o.Id);
                            cmd.Parameters.AddWithValue("a", it.ProductArticle);
                            cmd.Parameters.AddWithValue("q", it.Quantity);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }

        public static void DeleteOrder(int id)
        {
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM orders WHERE id = @id", cn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<User> GetClients()
        {
            var list = new List<User>();
            using (var cn = new NpgsqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT u.id, u.last_name, u.first_name,
                           COALESCE(u.middle_name,''), u.login, r.name
                    FROM users u
                    JOIN roles r ON r.id = u.role_id
                    WHERE r.name = 'Авторизированный клиент'
                    ORDER BY u.last_name", cn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new User
                        {
                            Id = r.GetInt32(0),
                            LastName = r.GetString(1),
                            FirstName = r.GetString(2),
                            MiddleName = r.GetString(3),
                            Login = r.GetString(4),
                            RoleName = r.GetString(5)
                        });
                    }
                }
            }
            return list;
        }
    }
}