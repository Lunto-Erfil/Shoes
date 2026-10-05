using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Shoes
{
    public partial class App : Application
    {
        // Глобальная обработка необработанных исключений (пункт ТЗ «обработка ИС»)
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"Произошла непредвиденная ошибка:\n{args.Exception.Message}\n\n" +
                    "Обратитесь к администратору.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}