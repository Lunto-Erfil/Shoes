using Shoes.Models;
using Shoes.Services;
using Shoes.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shoes
{
    public partial class MainWindow : Window
    {
        private List<Product> _all = new List<Product>();
        private bool _editingOpened = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var u = Session.CurrentUser;
            txtUserInfo.Text = (u == null)
                ? "Гость"
                : "Пользователь: " + u.FullName + " (" + u.RoleName + ")";

            pnlFilters.Visibility = Session.IsManager ? Visibility.Visible : Visibility.Collapsed;
            btnOrders.Visibility = Session.IsManager ? Visibility.Visible : Visibility.Collapsed;
            btnAdd.Visibility = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            ReloadList();
        }

        private void ReloadList()
        {
            try
            {
                _all = DbService.GetProducts();

                if (Session.IsManager)
                {
                    cmbSupplier.SelectionChanged -= Filter_Changed;

                    var prev = cmbSupplier.SelectedItem as string;
                    cmbSupplier.Items.Clear();
                    cmbSupplier.Items.Add("Все поставщики");

                    var suppliers = _all.Select(p => p.SupplierName)
                                        .Distinct()
                                        .OrderBy(x => x);
                    foreach (var s in suppliers) cmbSupplier.Items.Add(s);

                    if (prev != null) cmbSupplier.SelectedItem = prev;
                    if (cmbSupplier.SelectedIndex < 0) cmbSupplier.SelectedIndex = 0;

                    cmbSupplier.SelectionChanged += Filter_Changed;
                }

                ApplyFilters();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this,
                    "Не удалось загрузить список товаров:\n" + ex.Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<Product> q = _all;

            var search = (txtSearch.Text ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(p =>
                    Contains(p.Name, search) ||
                    Contains(p.Description, search) ||
                    Contains(p.CategoryName, search) ||
                    Contains(p.ManufacturerName, search) ||
                    Contains(p.SupplierName, search) ||
                    Contains(p.Article, search) ||
                    Contains(p.UnitName, search));
            }

            var sup = cmbSupplier.SelectedItem as string;
            if (sup != null && sup != "Все поставщики")
                q = q.Where(p => p.SupplierName == sup);

            if (cmbSort.SelectedIndex == 1) q = q.OrderBy(p => p.StockQuantity);
            else if (cmbSort.SelectedIndex == 2) q = q.OrderByDescending(p => p.StockQuantity);

            RenderList(q.ToList());
        }

        private static bool Contains(string src, string pattern)
        {
            if (string.IsNullOrEmpty(src)) return false;
            return src.ToLower().Contains(pattern);
        }

        private void RenderList(List<Product> items)
        {
            foreach (var p in items)
            {
                if (p.StockQuantity == 0)
                    p.RowBrush = (Brush)FindResource("OutOfStockBrush");
                else if (p.Discount > 15)
                    p.RowBrush = (Brush)FindResource("HighDiscountBrush");
                else
                    p.RowBrush = (Brush)FindResource("PrimaryBgBrush");
            }

            lstProducts.ItemsSource = null;
            lstProducts.ItemsSource = items;
            txtStatus.Text = "Показано: " + items.Count + " из " + _all.Count;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_editingOpened)
            {
                MessageBox.Show(this,
                    "Уже открыто окно редактирования товара.\n" +
                    "Закройте его перед открытием нового.",
                    "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _editingOpened = true;
            var wnd = new ProductEditWindow(null);
            wnd.Owner = this;
            wnd.Closed += (_, __) => { _editingOpened = false; ReloadList(); };
            wnd.Show();
        }

        private void LstProducts_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!Session.IsAdmin) return;

            if (_editingOpened)
            {
                MessageBox.Show(this, "Уже открыто окно редактирования.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var p = lstProducts.SelectedItem as Product;
            if (p == null) return;

            _editingOpened = true;
            var wnd = new ProductEditWindow(p);
            wnd.Owner = this;
            wnd.Closed += (_, __) => { _editingOpened = false; ReloadList(); };
            wnd.Show();
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new OrdersWindow();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.CurrentUser = null;
            new LoginWindow().Show();
            Close();
        }
    }
}
