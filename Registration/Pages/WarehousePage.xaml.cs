using Registration.Model;
using System.Windows;
using System;
using System.Windows.Controls;

namespace Registration.Pages
{
    public partial class WarehousePage : Page
    {
        public WarehousePage(Users user, string roleName)
        {
            InitializeComponent();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AuthPage());
        }
    }
}