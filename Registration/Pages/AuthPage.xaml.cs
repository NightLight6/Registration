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
    /// <summary>
    /// Логика взаимодействия для страницы AuthPage.xaml.
    /// Класс обеспечивает аутентификацию пользователей, двухфакторную проверку и восстановление пароля.
    /// </summary>
    public partial class AuthPage : Page
    {
        /// <summary>
        /// Свойство для хранения данных текущего авторизованного пользователя в рамках сессии.
        /// </summary>
        public static Users CurrentUser { get; set; }

        private int _failedAttempts = 0;
        private DispatcherTimer _blockTimer;
        private string _captchaText = "";
        private string _currentRecoveryEmail = "";

        private Users _userForTwoFactor = null;
        private string _roleForTwoFactor = "Клиент";

        // Сервис для отправки уведомлений на электронную почту
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

        /// <summary>
        /// Сбрасывает видимость панелей до начального состояния авторизации.
        /// </summary>
        private void HideAllExtraPanels()
        {
            spForgotPasswordEmail.Visibility = Visibility.Collapsed;
            spRecoveryCode.Visibility = Visibility.Collapsed;
            spNewPassword.Visibility = Visibility.Collapsed;
            spTwoFactor.Visibility = Visibility.Collapsed;
            spMainAuth.Visibility = Visibility.Visible;
            // Показ капчи только если были ошибки ввода
            brCaptcha.Visibility = _failedAttempts > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Обработчик события нажатия кнопки "Войти".
        /// Выполняет проверку логина/пароля и капчи.
        /// </summary>
        private async void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Запрет на ввод, если действует блокировка таймером
                if (_blockTimer != null && _blockTimer.IsEnabled) return;

                string login = tbLogin.Text.Trim();
                string password = tbPassword.Password;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Заполните все поля", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Логика проверки капчи
                if (brCaptcha.Visibility == Visibility.Visible)
                {
                    if (string.IsNullOrWhiteSpace(tbCaptcha.Text) || tbCaptcha.Text.Trim().ToLower() != _captchaText.ToLower())
                    {
                        MessageBox.Show("Неверный код капчи!");
                        ShowCaptcha();
                        return;
                    }
                }

                // Хеширование пароля перед проверкой в БД
                string passwordHash = PasswordHasher.ComputeSha256Hash(password);

                using (var db = new BeermageEntities1())
                {
                    var user = db.Users.Include(u => u.Roles).FirstOrDefault(u => u.Login == login && u.PasswordHash == passwordHash);

                    if (user != null)
                    {
                        string roleName = user.Roles?.RoleName ?? "Клиент";

                        // Инициация двухфакторной авторизации, если она включена в профиле
                        if (user.IsTwoFactorEnabled == true)
                        {
                            _userForTwoFactor = user;
                            _roleForTwoFactor = roleName;

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
                            FinalizeLogin(user, roleName);
                        }
                    }
                    else
                    {
                        // Счетчик неудачных попыток и вызов блокировки
                        _failedAttempts++;
                        if (_failedAttempts >= 3)
                        {
                            BlockUI();
                        }
                        else
                        {
                            ShowCaptcha();
                        }
                        MessageBox.Show("Неверный логин или пароль.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}");
            }
        }

        /// <summary>
        /// Подтверждение кода 2FA и завершение входа.
        /// </summary>
        private void BtnConfirmTwoFactor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = tbTwoFactorCode.Text.Trim();
                if (CodeStorage.ValidateCode(_userForTwoFactor.UserID.ToString(), input))
                {
                    FinalizeLogin(_userForTwoFactor, _roleForTwoFactor);
                }
                else
                {
                    MessageBox.Show("Неверный код или срок действия истек.");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// Фиксация данных пользователя в системе и переход на главную страницу роли.
        /// </summary>
        /// <param name="user">Объект найденного пользователя</param>
        /// <param name="roleName">Наименование роли</param>
        private void FinalizeLogin(Users user, string roleName)
        {
            CurrentUser = user;
            MessageBox.Show($"Добро пожаловать, {user.Name}! Роль: {roleName}");
            NavigateToRolePage(user, roleName);
        }

        /// <summary>
        /// Генерация и отображение новой капчи.
        /// </summary>
        private void ShowCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tblCaptcha.Text = _captchaText;
            brCaptcha.Visibility = Visibility.Visible;
            tbCaptcha.Clear();
        }

        /// <summary>
        /// Временная блокировка элементов управления при переборе паролей.
        /// </summary>
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

        /// <summary>
        /// Разблокировка интерфейса после завершения работы таймера.
        /// </summary>
        private void UnBlockUI()
        {
            _blockTimer?.Stop();
            tbBlockTimer.Visibility = Visibility.Collapsed;
            tbLogin.IsEnabled = tbPassword.IsEnabled = btnEnter.IsEnabled = true;
            _failedAttempts = 0;
        }

        /// <summary>
        /// Логика перенаправления пользователя в зависимости от уровня прав.
        /// </summary>
        private void NavigateToRolePage(Users user, string roleName)
        {
            if (roleName == "Администратор" || roleName == "Менеджер")
                NavigationService.Navigate(new UserListPage());
            else
                NavigationService.Navigate(new ClientPage(user, roleName));
        }

        /// <summary>
        /// Обработчик кнопки запроса кода для смены забытого пароля.
        /// </summary>
        private async void BtnSendRecoveryCode_Click(object sender, RoutedEventArgs e)
        {
            string email = tbRecoveryEmail.Text.Trim();
            using (var ctx = new BeermageEntities1())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Email == email);
                if (user == null) { MessageBox.Show("Пользователь не найден"); return; }

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

        /// <summary>
        /// Обновление пароля в базе данных на новый хеш.
        /// </summary>
        private void BtnSaveNewPassword_Click(object sender, RoutedEventArgs e)
        {
            if (pbNewPassword.Password != pbConfirmPassword.Password)
            {
                MessageBox.Show("Пароли не совпадают!");
                return;
            }

            using (var ctx = new BeermageEntities1())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Email == _currentRecoveryEmail);
                if (user != null)
                {
                    user.PasswordHash = PasswordHasher.ComputeSha256Hash(pbNewPassword.Password);
                    ctx.SaveChanges();
                    MessageBox.Show("Пароль успешно изменен!");
                    NavigationService.Navigate(new AuthPage());
                }
            }
        }

        // Вспомогательные методы навигации
        private void BackToAuth_Click(object sender, RoutedEventArgs e) => HideAllExtraPanels();
        private void btnEnterGuest_Click(object sender, RoutedEventArgs e) => NavigateToRolePage(null, "Клиент");
        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new RegistrationPage());
    }
}