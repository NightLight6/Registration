using Registration.Model;
using System.Windows;
using System.Windows.Controls;
using System;

namespace Registration.Pages
{
    public partial class AccountantPage : Page
    {
        public AccountantPage(Users user, string roleName)
        {
            InitializeComponent();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AuthPage());
        }
    }
}