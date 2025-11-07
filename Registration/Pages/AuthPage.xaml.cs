using Registration.Helpers;
using Registration.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Registration.Pages
{
    public partial class AuthPage : Page
    {
        private int _clickCount = 0;
        private string _captchaText = "";

        public AuthPage()
        {
            InitializeComponent();
            tblCaptcha.Visibility = System.Windows.Visibility.Collapsed;
            tbCaptcha.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void btnEnterGuest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ClientPage(null, "Client"));
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            _clickCount++;
            string login = tbLogin.Text.Trim();
            string password = tbPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            string passwordHash = PasswordHasher.ComputeSha256Hash(password);

            using (var context = new BeermagEntities2())
            {
                var user = context.Users.FirstOrDefault(u => u.Login == login && u.PasswordHash == passwordHash);

                if (_clickCount == 1)
                {
                    if (user != null)
                    {
                        var role = context.Roles.FirstOrDefault(r => r.RoleID == user.RoleID);
                        string roleName = role?.RoleName ?? "Client";
                        MessageBox.Show($"Вы вошли под: {roleName}");
                        LoadPage(user, roleName);
                    }
                    else
                    {
                        MessageBox.Show("Вы ввели логин или пароль неверно!");
                        GenerateCaptcha();
                        tbPassword.Clear(); // Очистка пароля
                    }
                }
                else if (_clickCount > 1)
                {
                    if (user != null && tbCaptcha.Text == _captchaText)
                    {
                        var role = context.Roles.FirstOrDefault(r => r.RoleID == user.RoleID);
                        string roleName = role?.RoleName ?? "Client";
                        MessageBox.Show($"Вы вошли под: {roleName}");
                        LoadPage(user, roleName);
                    }
                    else
                    {
                        MessageBox.Show("Неверная капча или данные!");
                        tbPassword.Clear();
                    }
                }
            }
        }

        private void GenerateCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tblCaptcha.Text = _captchaText;
            tblCaptcha.Visibility = Visibility.Visible;
            tbCaptcha.Visibility = Visibility.Visible;
            tblCaptcha.TextDecorations = TextDecorations.Strikethrough;
        }

        private void LoadPage(Users user, string roleName)
        {
            _clickCount = 0;
            tblCaptcha.Visibility = Visibility.Collapsed;
            tbCaptcha.Visibility = Visibility.Collapsed;
            tbCaptcha.Clear();
            tbLogin.Clear();
            tbPassword.Clear();

            Page nextPage;
            if (roleName == "Manager")
                nextPage = new ManagerPage(user, roleName);
            else if (roleName == "Production")
                nextPage = new ProductionPage(user, roleName);
            else if (roleName == "Warehouse")
                nextPage = new WarehousePage(user, roleName);
            else if (roleName == "Accountant")
                nextPage = new AccountantPage(user, roleName);
            else if (roleName == "Admin")
                nextPage = new ManagerPage(user, roleName);
            else
                nextPage = new ClientPage(user, roleName);

            NavigationService?.Navigate(nextPage);
        }

        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RegistrationPage());
        }
    }
}