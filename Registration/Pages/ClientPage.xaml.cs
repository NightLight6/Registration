using Registration.Model;
using System.Windows;
using System;
using System.Windows.Controls;

namespace Registration.Pages
{
    public partial class ClientPage : Page
    {
        public ClientPage(Users user, string roleName)
        {
            InitializeComponent();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AuthPage());
        }
    }
}