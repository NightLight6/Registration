using Registration.Helpers;
using Registration.Model;
using Registration.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.Windows.Threading;

namespace Registration.Pages
{
    public partial class AuthPage : Page
    {
        public static Users CurrentUser { get; set; }

        private int _failedAttempts = 0;
        private DispatcherTimer _blockTimer;
        private string _captchaText = "";
        private string _currentRecoveryEmail = "";
        private Users _userForTwoFactor = null;

        private readonly EmailService _emailService = new EmailService(
            "NightooLight@yandex.ru",
            "fiufifhygbfyhcvu"
        );

        public AuthPage()
        {
            InitializeComponent();
            tbBlockTimer.Visibility = Visibility.Collapsed;
            HideAllExtraPanels();
        }

        private void HideAllExtraPanels()
        {
            spForgotPasswordEmail.Visibility = Visibility.Collapsed;
            spRecoveryCode.Visibility = Visibility.Collapsed;
            spNewPassword.Visibility = Visibility.Collapsed;
            spTwoFactor.Visibility = Visibility.Collapsed;
            spMainAuth.Visibility = Visibility.Visible;
        }

        private async void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_blockTimer != null && _blockTimer.IsEnabled) return;

                string login = tbLogin.Text.Trim();
                string password = tbPassword.Password;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите логин и пароль.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (brCaptcha.Visibility == Visibility.Visible)
                {
                    if (tbCaptcha.Text.Trim().ToLower() != _captchaText.ToLower())
                    {
                        MessageBox.Show("Неверная капча!");
                        ShowCaptcha();
                        return;
                    }
                }

                string passwordHash = PasswordHasher.ComputeSha256Hash(password);

                using (var db = new BeermageEntities1())
                {
                    var user = db.Users.Include(u => u.Roles).FirstOrDefault(u => u.Login == login && u.PasswordHash == passwordHash);

                    if (user != null)
                    {
                        if (user.IsTwoFactorEnabled == true)
                        {

                            _userForTwoFactor = user;

                            string code = new Random().Next(1000, 9999).ToString();

                            CodeStorage.StoreCode(user.UserID.ToString(), code, TimeSpan.FromMinutes(5));

                            spMainAuth.Visibility = Visibility.Collapsed;
                            spTwoFactor.Visibility = Visibility.Visible;
                            tbTwoFactorEmail.Text = user.Email;

                            await _emailService.SendTwoFactorCodeAsync(user.Email, code);

                            MessageBox.Show($"Код подтверждения отправлен на {user.Email}", "2FA");
                        }
                        else
                        {
                            FinalizeLogin(user);
                        }
                    }
                    else
                    {
                        _failedAttempts++;
                        if (_failedAttempts >= 3) BlockUI();
                        else ShowCaptcha();
                        MessageBox.Show("Неверный логин или пароль.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}");
            }
        }

        private void BtnConfirmTwoFactor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = tbTwoFactorCode.Text.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("Введите код подтверждения");
                    return;
                }

                if (_userForTwoFactor == null)
                {
                    MessageBox.Show("Ошибка сессии. Пожалуйста, введите логин и пароль заново.");
                    BackToAuth_Click(null, null);
                    return;
                }

                string key = _userForTwoFactor.UserID.ToString();

                if (CodeStorage.ValidateCode(key, input))
                {
                    FinalizeLogin(_userForTwoFactor);
                }
                else
                {
                    MessageBox.Show("Неверный код или срок его действия истек (5 мин)");
                    tbTwoFactorCode.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}");
            }
        }

        private void FinalizeLogin(Users user)
        {
            CurrentUser = user;

            string roleName = "Клиент";
            if (user.Roles != null)
            {
                roleName = user.Roles.RoleName;
            }

            MessageBox.Show($"Добро пожаловать, {user.Name}!");
            NavigateToRolePage(user, roleName);
        }


        private void ShowCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tblCaptcha.Text = _captchaText;
            tblCaptcha.Visibility = Visibility.Visible;
            tbCaptcha.Visibility = Visibility.Visible;
            brCaptcha.Visibility = Visibility.Visible;
        }

        private void BlockUI()
        {
            tbLogin.IsEnabled = tbPassword.IsEnabled = btnEnter.IsEnabled = false;
            int secondsLeft = 10;
            tbBlockTimer.Visibility = Visibility.Visible;
            _blockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _blockTimer.Tick += (s, e) => {
                secondsLeft--;
                tbBlockTimer.Text = $"Заблокировано на {secondsLeft} сек.";
                if (secondsLeft <= 0) UnBlockUI();
            };
            _blockTimer.Start();
        }

        private void UnBlockUI()
        {
            _blockTimer?.Stop();
            tbBlockTimer.Visibility = Visibility.Collapsed;
            tbLogin.IsEnabled = tbPassword.IsEnabled = btnEnter.IsEnabled = true;
            _failedAttempts = 0;
            brCaptcha.Visibility = Visibility.Collapsed;
        }

        private void NavigateToRolePage(Users user, string roleName)
        {
            if (roleName == "Администратор" || roleName == "Менеджер")
                NavigationService.Navigate(new UserListPage());
            else
                NavigationService.Navigate(new ClientPage(user, roleName));
        }

        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            spMainAuth.Visibility = Visibility.Collapsed;
            spForgotPasswordEmail.Visibility = Visibility.Visible;
        }

        private async void BtnSendRecoveryCode_Click(object sender, RoutedEventArgs e)
        {
            string email = tbRecoveryEmail.Text.Trim();
            using (var ctx = new BeermageEntities1())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Email == email);
                if (user == null) { MessageBox.Show("Email не найден"); return; }

                string code = new Random().Next(1000, 9999).ToString();
                CodeStorage.StoreCode(email, code, TimeSpan.FromMinutes(5));
                if (await _emailService.SendPasswordResetEmailAsync(email, code))
                {
                    _currentRecoveryEmail = email;
                    spForgotPasswordEmail.Visibility = Visibility.Collapsed;
                    spRecoveryCode.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnConfirmRecoveryCode_Click(object sender, RoutedEventArgs e)
        {
            if (CodeStorage.ValidateCode(_currentRecoveryEmail, tbRecoveryCode.Text.Trim()))
            {
                spRecoveryCode.Visibility = Visibility.Collapsed;
                spNewPassword.Visibility = Visibility.Visible;
            }
        }

        private void BtnSaveNewPassword_Click(object sender, RoutedEventArgs e)
        {
            if (pbNewPassword.Password != pbConfirmPassword.Password) return;
            using (var ctx = new BeermageEntities1())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Email == _currentRecoveryEmail);
                if (user != null)
                {
                    user.PasswordHash = PasswordHasher.ComputeSha256Hash(pbNewPassword.Password);
                    ctx.SaveChanges();
                    MessageBox.Show("Успешно!");
                    NavigationService.Navigate(new AuthPage());
                }
            }
        }

        private void BackToAuth_Click(object sender, RoutedEventArgs e) => HideAllExtraPanels();
        private void btnEnterGuest_Click(object sender, RoutedEventArgs e) => NavigateToRolePage(null, "Клиент");
        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new RegistrationPage());
    }
}