using Shoes.Models;
using Shoes.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class OrderEditWindow : Window
    {
        private readonly Order _order;
        private readonly bool _isEdit;
        private readonly ObservableCollection<OrderItem> _items
            = new ObservableCollection<OrderItem>();

        public OrderEditWindow(Order o)
        {
            InitializeComponent();
            _isEdit = o != null;
            Title = _isEdit ? "Редактирование заказа" : "Добавление заказа";

            // Заполняем списки
            cmbStatus.ItemsSource = DbService.GetOrderStatuses();
            cmbStatus.DisplayMemberPath = "Item2";
            cmbStatus.SelectedValuePath = "Item1";

            cmbPickup.ItemsSource = DbService.GetPickupPoints();
            cmbPickup.DisplayMemberPath = "Item2";
            cmbPickup.SelectedValuePath = "Item1";

            cmbClient.ItemsSource = DbService.GetClients();
            cmbClient.DisplayMemberPath = "FullName";
            cmbClient.SelectedValuePath = "Id";

            var products = DbService.GetProducts();
            cmbProductToAdd.ItemsSource = products;
            cmbProductToAdd.DisplayMemberPath = "Article";
            cmbProductToAdd.SelectedValuePath = "Article";
            if (products.Count > 0) cmbProductToAdd.SelectedIndex = 0;

            dgItems.ItemsSource = _items;

            if (_isEdit)
            {
                _order = o;
                txtNumber.Text = o.Id.ToString();
                txtCode.Text = o.PickupCode;
                dpOrderDate.SelectedDate = o.OrderDate;
                dpDeliveryDate.SelectedDate = o.DeliveryDate;
                cmbStatus.SelectedValue = o.StatusId;
                cmbPickup.SelectedValue = o.PickupPointId;
                cmbClient.SelectedValue = o.UserId;

                var existing = DbService.GetOrderItems(o.Id);
                foreach (var it in existing) _items.Add(it);
            }
            else
            {
                _order = new Order();
                txtNumber.Text = "(авто)";
                btnDelete.Visibility = Visibility.Collapsed;
                dpOrderDate.SelectedDate = DateTime.Today;
                dpDeliveryDate.SelectedDate = DateTime.Today.AddDays(3);

                if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
                if (cmbPickup.Items.Count > 0) cmbPickup.SelectedIndex = 0;
                if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;
            }
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            // C# 7.3: вместо `is not Product p` — используем as
            var p = cmbProductToAdd.SelectedItem as Product;
            if (p == null)
            {
                MessageBox.Show(this, "Выберите товар.",
                    "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int qty;
            if (!int.TryParse(txtQtyToAdd.Text, out qty) || qty <= 0)
            {
                MessageBox.Show(this, "Введите положительное количество.",
                    "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OrderItem existing = null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ProductArticle == p.Article)
                {
                    existing = _items[i];
                    break;
                }
            }

            if (existing != null)
                existing.Quantity += qty;
            else
                _items.Add(new OrderItem
                {
                    ProductArticle = p.Article,
                    Quantity = qty
                });

            dgItems.Items.Refresh();
            txtQtyToAdd.Clear();
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var it = dgItems.SelectedItem as OrderItem;
            if (it != null) _items.Remove(it);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStatus.SelectedValue == null ||
                cmbPickup.SelectedValue == null ||
                cmbClient.SelectedValue == null ||
                dpOrderDate.SelectedDate == null ||
                dpDeliveryDate.SelectedDate == null)
            {
                MessageBox.Show(this, "Заполните все обязательные поля.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dpDeliveryDate.SelectedDate.Value < dpOrderDate.SelectedDate.Value)
            {
                MessageBox.Show(this,
                    "Дата доставки не может быть раньше даты заказа.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show(this, "Введите код для получения.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_items.Count == 0)
            {
                MessageBox.Show(this, "Добавьте хотя бы одну позицию в заказ.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _order.OrderDate = dpOrderDate.SelectedDate.Value;
            _order.DeliveryDate = dpDeliveryDate.SelectedDate.Value;
            _order.StatusId = (int)cmbStatus.SelectedValue;
            _order.PickupPointId = (int)cmbPickup.SelectedValue;
            _order.UserId = (int)cmbClient.SelectedValue;
            _order.PickupCode = txtCode.Text.Trim();

            try
            {
                if (_isEdit)
                    DbService.UpdateOrder(_order, _items.ToList());
                else
                    DbService.AddOrder(_order, _items.ToList());

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Ошибка сохранения заказа: " + ex.Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEdit) return;

            if (MessageBox.Show(this,
                    "Удалить заказ №" + _order.Id + "?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                DbService.DeleteOrder(_order.Id);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Ошибка удаления: " + ex.Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
