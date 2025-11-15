using Registration.Model;
using System.Windows;
using System;
using System.Windows.Controls;
using Registration.Services;

namespace Registration.Pages
{
    public partial class WarehousePage : Page
    {
        public WarehousePage(Users user, string rolename)
        {
            InitializeComponent();
            LoadGreeting();
        }

        private void LoadGreeting()
        {
            if (AuthPage.CurrentUser != null)
            {
                var user = AuthPage.CurrentUser;
                string fullName = $"{user.Name} {user.Surname} {user.Otchestvo}".Trim();

                if (string.IsNullOrEmpty(fullName))
                    fullName = $"{user.Name}";

                string greeting = TimeHelper.GetGreeting();
                tbGreeting.Text = $"{greeting}, {fullName}!";
            }
            else
            {
                tbGreeting.Text = "Здравствуйте!";
            }
        }

        private void LogoutButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AuthPage.CurrentUser = null;
            NavigationService?.Navigate(new AuthPage());
        }
    }
}