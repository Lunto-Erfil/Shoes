using Shoes.Models;
using Shoes.Services;
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
using System.Windows.Shapes;

namespace Shoes.Views
{
    public partial class OrdersWindow : Window
    {
        public OrdersWindow()
        {
            InitializeComponent();
            Loaded += OrdersWindow_Loaded;
        }

        private void OrdersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.IsAdmin) btnAddOrder.Visibility = Visibility.Visible;
            Reload();
        }

        private void Reload()
        {
            try
            {
                lstOrders.ItemsSource = DbService.GetOrders();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this,
                    "Не удалось загрузить заказы: " + ex.Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) { Reload(); }
        private void BtnBack_Click(object sender, RoutedEventArgs e) { Close(); }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new OrderEditWindow(null);
            wnd.Owner = this;
            if (wnd.ShowDialog() == true) Reload();
        }

        private void LstOrders_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!Session.IsAdmin) return;

            var o = lstOrders.SelectedItem as Order;
            if (o == null) return;

            var wnd = new OrderEditWindow(o);
            wnd.Owner = this;
            if (wnd.ShowDialog() == true) Reload();
        }
    }
}
