using Registration.Helpers;
using Registration.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Registration.Pages
{
    public partial class AuthPage : Page
    {
        private int _failedAttempts = 0;
        private DispatcherTimer _blockTimer;
        private string _captchaText = "";

        public AuthPage()
        {
            InitializeComponent();
            tbBlockTimer.Visibility = Visibility.Collapsed;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            ClearCaptcha();
            if (_blockTimer != null && _blockTimer.IsEnabled)
            {
                return;
            }

            string login = tbLogin.Text.Trim();
            string password = tbPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.");
                return;
            }

            string passwordHash = PasswordHasher.ComputeSha256Hash(password);

            using (var context = new BeermagEntities2())
            {
                var user = context.Users.FirstOrDefault(u => u.Login == login && u.PasswordHash == passwordHash);

                if (user != null)
                {
                    _failedAttempts = 0;
                    var role = context.Roles.FirstOrDefault(r => r.RoleID == user.RoleID);
                    string roleName = role?.RoleName ?? "Client";
                    MessageBox.Show($"Вы вошли под: {roleName}");
                }
                else
                {
                    _failedAttempts++;

                    if (_failedAttempts >= 3)
                    {
                        BlockUI();
                    }
                    else
                    {
                        if (_failedAttempts >= 1)
                        {
                            ShowCaptcha();
                        }

                        MessageBox.Show("Неверный логин или пароль.");
                        tbPassword.Clear();
                    }
                }
            }
        }

        private void ShowCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tblCaptcha.Text = _captchaText;
            tblCaptcha.Visibility = Visibility.Visible;
            tbCaptcha.Visibility = Visibility.Visible;
        }

        private void BlockUI()
        {
            tbLogin.IsEnabled = false;
            tbPassword.IsEnabled = false;
            tbCaptcha.IsEnabled = false;
            btnEnter.IsEnabled = false;
            btnEnterGuest.IsEnabled = false;

            int secondsLeft = 10;
            tbBlockTimer.Text = $"Заблокировано на {secondsLeft} секунд...";
            tbBlockTimer.Visibility = Visibility.Visible;

            _blockTimer = new DispatcherTimer();
            _blockTimer.Interval = TimeSpan.FromSeconds(1);
            _blockTimer.Tick += (s, e) =>
            {
                secondsLeft--;
                tbBlockTimer.Text = $"Заблокировано на {secondsLeft} секунд...";

                if (secondsLeft <= 0)
                {
                    UnBlockUI();
                }
            };
            _blockTimer.Start();
        }

        private void UnBlockUI()
        {
            _blockTimer?.Stop();
            tbBlockTimer.Visibility = Visibility.Collapsed;

            tbLogin.IsEnabled = true;
            tbPassword.IsEnabled = true;
            tbCaptcha.IsEnabled = true;
            btnEnter.IsEnabled = true;
            btnEnterGuest.IsEnabled = true;

            _failedAttempts = 0;
            HideCaptcha();
        }

        private void HideCaptcha()
        {
            tblCaptcha.Visibility = Visibility.Collapsed;
            tbCaptcha.Visibility = Visibility.Collapsed;
            tbCaptcha.Clear();
        }

        private void btnEnterGuest_Click(object sender, RoutedEventArgs e)
        {
            if (_blockTimer != null && _blockTimer.IsEnabled)
            {
                return;
            }
            NavigationService?.Navigate(new ClientPage(null, "Client"));
        }

        private void ClearCaptcha()
        {
            tbCaptcha.Clear();
        }

        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            if (_blockTimer != null && _blockTimer.IsEnabled)
            {
                return;
            }
            NavigationService?.Navigate(new RegistrationPage());
        }
    }
}