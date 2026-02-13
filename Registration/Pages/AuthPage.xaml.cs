using Registration.Helpers;
using Registration.Model;
using Registration.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        }

        private void ShowMainAuth()
        {
            tbLogin.Visibility = Visibility.Visible;
            tbPassword.Visibility = Visibility.Visible;
            btnEnter.Visibility = Visibility.Visible;
            btnEnterGuest.Visibility = Visibility.Visible;
            btnGoToRegister.Visibility = Visibility.Visible;
            BtnForgotPassword.Visibility = Visibility.Visible;
            tblCaptcha.Visibility = Visibility.Collapsed;
            tbCaptcha.Visibility = Visibility.Collapsed;
        }

        private void HideMainAuth()
        {
            tbLogin.Visibility = Visibility.Collapsed;
            tbPassword.Visibility = Visibility.Collapsed;
            btnEnter.Visibility = Visibility.Collapsed;
            btnEnterGuest.Visibility = Visibility.Collapsed;
            btnGoToRegister.Visibility = Visibility.Collapsed;
            BtnForgotPassword.Visibility = Visibility.Collapsed;
        }

        private async void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            ClearCaptcha();

            if (_blockTimer != null && _blockTimer.IsEnabled)
            {
                MessageBox.Show("Система временно заблокирована. Подождите.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeHelper.IsWorkTime())
            {
                MessageBox.Show("Работа с системой возможна только с 10:00 до 19:00.", "Вне рабочего времени", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string login = tbLogin.Text.Trim();
            string password = tbPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string passwordHash = PasswordHasher.ComputeSha256Hash(password);

            using (var context = new BeermageEntities1())
            {
                var user = context.Users.FirstOrDefault(u => u.Login == login && u.PasswordHash == passwordHash);

                if (user != null)
                {
                    _failedAttempts = 0;

                    bool isTwoFactorEnabled = user.IsTwoFactorEnabled ?? false;
                    if (isTwoFactorEnabled)
                    {
                        string code = new Random().Next(1000, 9999).ToString();
                        CodeStorage.StoreCode(user.UserID.ToString(), code, TimeSpan.FromMinutes(5));

                        try
                        {
                            bool sent = await _emailService.SendTwoFactorCodeAsync(
                                user.Email,
                                code
                            ).ConfigureAwait(false);

                            if (sent)
                            {
                                _userForTwoFactor = user;
                                HideMainAuth();
                                spTwoFactor.Visibility = Visibility.Visible;
                                MessageBox.Show("Код отправлен на вашу почту. Проверьте email.", "2FA", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Не удалось отправить код 2FA. Проверьте настройки SMTP или интернет-соединение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при отправке 2FA: {ex.Message}", "Ошибка SMTP", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    var role = context.Roles.FirstOrDefault(r => r.RoleID == user.RoleID);
                    string roleName = role?.RoleName ?? "Клиент";
                    CurrentUser = user;

                    MessageBox.Show($"Добро пожаловать, {roleName}!", "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigateToRolePage(user, roleName);
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
                        MessageBox.Show("Неверный логин или пароль.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
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
            btnGoToRegister.IsEnabled = false;
            BtnForgotPassword.IsEnabled = false;

            int secondsLeft = 10;
            tbBlockTimer.Text = $"Заблокировано на {secondsLeft} секунд...";
            tbBlockTimer.Visibility = Visibility.Visible;

            _blockTimer = new DispatcherTimer();
            _blockTimer.Interval = TimeSpan.FromSeconds(1);
            _blockTimer.Tick += (s, ev) =>
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
            btnGoToRegister.IsEnabled = true;
            BtnForgotPassword.IsEnabled = true;

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
                return;

            NavigateToRolePage(null, "Клиент");
        }

        private void ClearCaptcha()
        {
            tbCaptcha.Clear();
        }

        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            if (_blockTimer != null && _blockTimer.IsEnabled)
                return;

            NavigationService?.Navigate(new RegistrationPage());
        }

        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            HideMainAuth();
            spForgotPasswordEmail.Visibility = Visibility.Visible;
        }

        private async void BtnSendRecoveryCode_Click(object sender, RoutedEventArgs e)
        {
            string email = tbRecoveryEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Введите email.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var ctx = new BeermageEntities1())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
                    MessageBox.Show("Пользователь с таким email не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string code = new Random().Next(1000, 9999).ToString();
                CodeStorage.StoreCode(email, code, TimeSpan.FromMinutes(5));

                try
                {
                    bool sent = await _emailService.SendPasswordResetEmailAsync(
                        email,
                        code
                    ).ConfigureAwait(false);

                    if (sent)
                    {
                        _currentRecoveryEmail = email;
                        spForgotPasswordEmail.Visibility = Visibility.Collapsed;
                        spRecoveryCode.Visibility = Visibility.Visible;
                        MessageBox.Show("Код отправлен на вашу почту.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось отправить код восстановления. Проверьте настройки SMTP.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отправки: {ex.Message}", "Ошибка SMTP", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnConfirmRecoveryCode_Click(object sender, RoutedEventArgs e)
        {
            string input = tbRecoveryCode.Text.Trim();
            if (CodeStorage.ValidateCode(_currentRecoveryEmail, input))
            {
                spRecoveryCode.Visibility = Visibility.Collapsed;
                spNewPassword.Visibility = Visibility.Visible;
                MessageBox.Show("Код подтверждён. Введите новый пароль.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Неверный или просроченный код восстановления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                tbRecoveryCode.Clear();
            }
        }

        private void BtnSaveNewPassword_Click(object sender, RoutedEventArgs e)
        {
            string pass1 = pbNewPassword.Password;
            string pass2 = pbConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(pass1) || string.IsNullOrWhiteSpace(pass2))
            {
                MessageBox.Show("Пароли не могут быть пустыми.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass1 != pass2)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (pass1.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var ctx = new BeermageEntities1())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Email == _currentRecoveryEmail);
                if (user != null)
                {
                    user.PasswordHash = PasswordHasher.ComputeSha256Hash(pass1);
                    ctx.SaveChanges();
                    MessageBox.Show("Пароль успешно изменён! Теперь вы можете войти с новым паролем.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    ResetRecoveryUI();
                    NavigationService?.Navigate(new AuthPage());
                }
                else
                {
                    MessageBox.Show("Пользователь не найден. Попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetRecoveryUI()
        {
            HideAllExtraPanels();
            ShowMainAuth();
            tbRecoveryEmail.Clear();
            tbRecoveryCode.Clear();
            pbNewPassword.Clear();
            pbConfirmPassword.Clear();
            _currentRecoveryEmail = "";
        }

        private void BtnConfirmTwoFactor_Click(object sender, RoutedEventArgs e)
        {
            string input = tbTwoFactorCode.Text.Trim();
            if (_userForTwoFactor != null &&
                CodeStorage.ValidateCode(_userForTwoFactor.UserID.ToString(), input))
            {
                CurrentUser = _userForTwoFactor;
                using (var context = new BeermageEntities1())
                {
                    var role = context.Roles.FirstOrDefault(r => r.RoleID == _userForTwoFactor.RoleID);
                    string roleName = role?.RoleName ?? "Клиент";

                    MessageBox.Show($"Вы вошли как {roleName}.", "Успешная авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigateToRolePage(_userForTwoFactor, roleName);
                }
            }
            else
            {
                MessageBox.Show("Неверный код 2FA. Проверьте почту или запросите новый код.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                tbTwoFactorCode.Clear();
            }
        }

        private void NavigateToRolePage(Users user, string roleName)
        {
            Page nextPage;
            switch (roleName)
            {
                case "Администратор":
                case "Менеджер":
                case "Сотрудник":
                    nextPage = new UserListPage();
                    break;
                case "Продавец":
                    nextPage = new ProductListPage();
                    break;
                case "Клиент":
                    nextPage = new ClientPage(user, roleName);
                    break;
                default:
                    nextPage = new ClientPage(user, roleName);
                    break;
            }

            NavigationService?.Navigate(nextPage);
        }

        private void tbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
        }

        private void pbNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
        }

        private void pbConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
        }

        private void tbPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
            }
        }

        private void tbPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
            }
        }

        private void BackToAuth_Click(object sender, RoutedEventArgs e)
        {
            ResetRecoveryUI();
            ShowMainAuth();
        }

        private void BackToEmail_Click(object sender, RoutedEventArgs e)
        {
            spRecoveryCode.Visibility = Visibility.Collapsed;
            spForgotPasswordEmail.Visibility = Visibility.Visible;
            tbRecoveryCode.Clear();
        }

        private void BackToCode_Click(object sender, RoutedEventArgs e)
        {
            spNewPassword.Visibility = Visibility.Collapsed;
            spRecoveryCode.Visibility = Visibility.Visible;
            pbNewPassword.Clear();
            pbConfirmPassword.Clear();
        }
    }
}