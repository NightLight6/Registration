using Registration.Model;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Registration.Helpers;
using Registration.Services;
using System;

namespace Registration.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProfilePage.xaml.
    /// Предоставляет интерфейс личного кабинета для управления данными профиля и безопасностью.
    /// </summary>
    public partial class ProfilePage : Page
    {
        /// <summary>
        /// Конструктор страницы профиля.
        /// Инициализирует компоненты и загружает данные текущего пользователя.
        /// </summary>
        public ProfilePage()
        {
            InitializeComponent();
            LoadUserData();
        }

        /// <summary>
        /// Заполняет элементы интерфейса данными из текущей сессии пользователя.
        /// </summary>
        private void LoadUserData()
        {
            // Получаем данные пользователя, сохраненные при авторизации
            var user = AuthPage.CurrentUser;
            if (user == null) return;

            // Отображение базовой информации
            tblFullName.Text = user.Name;
            tblLogin.Text = user.Login;
            tblEmail.Text = user.Email;
            tblRole.Text = user.Roles?.RoleName ?? "Клиент";

            // Временно отписываемся от событий, чтобы установка значения не вызывала срабатывание логики сохранения в БД
            cbTwoFactorEnable.Checked -= CbTwoFactor_Changed;
            cbTwoFactorEnable.Unchecked -= CbTwoFactor_Changed;

            cbTwoFactorEnable.IsChecked = user.IsTwoFactorEnabled;

            // Подписываемся обратно после инициализации
            cbTwoFactorEnable.Checked += CbTwoFactor_Changed;
            cbTwoFactorEnable.Unchecked += CbTwoFactor_Changed;
        }

        /// <summary>
        /// Обработчик изменения состояния чекбокса двухфакторной аутентификации.
        /// Обновляет настройки безопасности в базе данных.
        /// </summary>
        private void CbTwoFactor_Changed(object sender, RoutedEventArgs e)
        {
            using (var db = new BeermageEntities1())
            {
                // Поиск пользователя в контексте БД по ID
                var user = db.Users.Find(AuthPage.CurrentUser.UserID);
                if (user != null)
                {
                    // Сохранение нового состояния 2FA
                    user.IsTwoFactorEnabled = cbTwoFactorEnable.IsChecked ?? false;
                    db.SaveChanges();

                    // Обновление данных в текущей сессии
                    AuthPage.CurrentUser.IsTwoFactorEnabled = user.IsTwoFactorEnabled;

                    string status = (bool)user.IsTwoFactorEnabled ? "включена" : "выключена";
                    MessageBox.Show($"Двухфакторная аутентификация {status}!", "Безопасность");
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки обновления пароля.
        /// Выполняет валидацию введенных данных и сохраняет новый хеш пароля.
        /// </summary>
        private void BtnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            // Проверка минимальной длины пароля (требование безопасности)
            if (string.IsNullOrWhiteSpace(pbNewPass.Password) || pbNewPass.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов!");
                return;
            }

            // Проверка подтверждения пароля
            if (pbNewPass.Password != pbConfirmPass.Password)
            {
                MessageBox.Show("Пароли не совпадают!");
                return;
            }

            using (var db = new BeermageEntities1())
            {
                var user = db.Users.Find(AuthPage.CurrentUser.UserID);
                if (user != null)
                {
                    // Хеширование нового пароля перед сохранением
                    user.PasswordHash = PasswordHasher.ComputeSha256Hash(pbNewPass.Password);
                    db.SaveChanges();

                    MessageBox.Show("Пароль успешно обновлен!");

                    // Очистка полей ввода после успешного сохранения
                    pbNewPass.Clear();
                    pbConfirmPass.Clear();
                }
            }
        }

        /// <summary>
        /// Выполняет выход из учетной записи и перенаправляет на страницу авторизации.
        /// </summary>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Сброс текущего пользователя (завершение сессии)
            AuthPage.CurrentUser = null;
            // Переход на страницу входа
            NavigationService.Navigate(new AuthPage());
        }
    }
}