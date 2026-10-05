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
    public partial class LoginWindow : Window
    {
        public LoginWindow() => InitializeComponent();

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var login = txtLogin.Text.Trim();
            var pwd = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pwd))
            {
                MessageBox.Show(this,
                    "Введите логин и пароль.",
                    "Ошибка авторизации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = DbService.Authenticate(login, pwd);
            if (user == null)
            {
                MessageBox.Show(this,
                    "Неверный логин или пароль.\n" +
                    "Проверьте раскладку клавиатуры и повторите попытку.",
                    "Ошибка авторизации",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Session.CurrentUser = user;
            new MainWindow().Show();
            Close();
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            Session.CurrentUser = null;
            new MainWindow().Show();
            Close();
        }
    }
}
