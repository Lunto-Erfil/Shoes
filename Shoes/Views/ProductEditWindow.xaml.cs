using Microsoft.Win32;
using Shoes.Models;
using Shoes.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Shoes.Views
{
    public partial class ProductEditWindow : Window
    {
        private readonly Product _product;
        private readonly bool _isEdit;
        private string _currentPhotoFile;
        private string _oldArticle;

        private const int PhotoWidth = 300;
        private const int PhotoHeight = 200;

        public ProductEditWindow(Product p)
        {
            InitializeComponent();
            _isEdit = p != null;
            Title = _isEdit ? "Редактирование товара" : "Добавление товара";

            LoadComboBoxes();

            if (_isEdit)
            {
                _product = p;
                _oldArticle = p.Article;
                _currentPhotoFile = p.PhotoPath;

                txtArticle.Text = p.Article;
                txtName.Text = p.Name;
                txtDescription.Text = p.Description;
                txtPrice.Text = p.Price.ToString("0.##");
                txtStock.Text = p.StockQuantity.ToString();
                txtDiscount.Text = p.Discount.ToString();

                cmbCategory.SelectedValue = p.CategoryId;
                cmbManufacturer.SelectedValue = p.ManufacturerId;
                cmbSupplier.SelectedValue = p.SupplierId;
                cmbUnit.SelectedValue = p.UnitId;

                UpdatePhotoPreview();
            }
            else
            {
                _product = new Product();
                txtArticle.Text = DbService.GenerateNextArticle();
                txtDiscount.Text = "0";
                txtStock.Text = "0";
                txtPrice.Text = "0";

                cmbCategory.SelectedIndex = 0;
                cmbManufacturer.SelectedIndex = 0;
                cmbSupplier.SelectedIndex = 0;
                cmbUnit.SelectedIndex = 0;

                btnDelete.Visibility = Visibility.Collapsed;
                UpdatePhotoPreview();
            }
        }

        private void LoadComboBoxes()
        {
            cmbCategory.ItemsSource = DbService.GetCategories();
            cmbCategory.DisplayMemberPath = "Item2";
            cmbCategory.SelectedValuePath = "Item1";

            cmbManufacturer.ItemsSource = DbService.GetManufacturers();
            cmbManufacturer.DisplayMemberPath = "Item2";
            cmbManufacturer.SelectedValuePath = "Item1";

            cmbSupplier.ItemsSource = DbService.GetSuppliers();
            cmbSupplier.DisplayMemberPath = "Item2";
            cmbSupplier.SelectedValuePath = "Item1";

            cmbUnit.ItemsSource = DbService.GetUnits();
            cmbUnit.DisplayMemberPath = "Item2";
            cmbUnit.SelectedValuePath = "Item1";
        }

        private void UpdatePhotoPreview()
        {
            if (!string.IsNullOrEmpty(_currentPhotoFile))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                        "Images", _currentPhotoFile);
                if (File.Exists(path))
                {
                    imgPhoto.Source = new BitmapImage(new Uri(path));
                    return;
                }
            }
            imgPhoto.Source = new BitmapImage(
                new Uri("pack://application:,,,/Resources/picture.png"));
        }

        private void BtnLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp";
            dlg.Title = "Выбор фото товара";
            if (dlg.ShowDialog() != true) return;

            try
            {
                var resized = ResizeImage(dlg.FileName, PhotoWidth, PhotoHeight);

                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                Directory.CreateDirectory(dir);

                if (!string.IsNullOrEmpty(_currentPhotoFile))
                {
                    var old = Path.Combine(dir, _currentPhotoFile);
                    if (File.Exists(old)) File.Delete(old);
                }

                var newName = Guid.NewGuid().ToString("N") + ".jpg";
                var full = Path.Combine(dir, newName);

                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 90;
                encoder.Frames.Add(BitmapFrame.Create(resized));

                using (var fs = File.Create(full))
                {
                    encoder.Save(fs);
                }

                _currentPhotoFile = newName;
                UpdatePhotoPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Не удалось загрузить фото: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPhotoFile)) return;

            if (MessageBox.Show(this,
                    "Удалить текущее фото?",
                    "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            var file = Path.Combine(dir, _currentPhotoFile);
            if (File.Exists(file)) File.Delete(file);

            _currentPhotoFile = null;
            UpdatePhotoPreview();
        }

        private static BitmapSource ResizeImage(string file, int maxW, int maxH)
        {
            var src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(file);
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.EndInit();

            double ratio = Math.Min((double)maxW / src.PixelWidth,
                                    (double)maxH / src.PixelHeight);

            if (ratio >= 1.0) return src;

            var transform = new ScaleTransform(ratio, ratio);
            var transformed = new TransformedBitmap(src, transform);
            transformed.Freeze();
            return transformed;
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]+$");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Введите наименование товара.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }
            if (cmbCategory.SelectedValue == null ||
                cmbManufacturer.SelectedValue == null ||
                cmbSupplier.SelectedValue == null ||
                cmbUnit.SelectedValue == null)
            {
                MessageBox.Show(this, "Заполните все выпадающие списки.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal price;
            if (!decimal.TryParse(txtPrice.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out price) || price < 0)
            {
                MessageBox.Show(this,
                    "Цена должна быть неотрицательным числом.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int stock;
            if (!int.TryParse(txtStock.Text, out stock) || stock < 0)
            {
                MessageBox.Show(this,
                    "Количество на складе не может быть отрицательным.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int disc;
            if (!int.TryParse(txtDiscount.Text, out disc) || disc < 0 || disc > 100)
            {
                MessageBox.Show(this,
                    "Скидка должна быть в диапазоне 0…100 %.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _product.Article = txtArticle.Text.Trim();
            _product.Name = txtName.Text.Trim();
            _product.Description = txtDescription.Text.Trim();
            _product.Price = price;
            _product.StockQuantity = stock;
            _product.Discount = disc;
            _product.CategoryId = (int)cmbCategory.SelectedValue;
            _product.ManufacturerId = (int)cmbManufacturer.SelectedValue;
            _product.SupplierId = (int)cmbSupplier.SelectedValue;
            _product.UnitId = (int)cmbUnit.SelectedValue;
            _product.PhotoPath = _currentPhotoFile;

            try
            {
                if (_isEdit)
                    DbService.UpdateProduct(_product, _oldArticle);
                else
                    DbService.AddProduct(_product);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Ошибка сохранения: " + ex.Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEdit) return;

            if (DbService.IsProductInOrders(_product.Article))
            {
                MessageBox.Show(this,
                    "Товар присутствует в заказах и не может быть удалён.",
                    "Удаление запрещено",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(this,
                    "Удалить товар «" + _product.Name + "» (" + _product.Article + ")?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                DbService.DeleteProduct(_product.Article);

                if (!string.IsNullOrEmpty(_currentPhotoFile))
                {
                    var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                            "Images", _currentPhotoFile);
                    if (File.Exists(file)) File.Delete(file);
                }

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