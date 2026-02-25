using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Registration.Model;
using Registration.Services;
using Registration.Helpers;
using Registration.UserViewModelValidators;

namespace Registration.Pages
{
    /// <summary>
    /// Логика взаимодействия для страницы регистрации RegistrationPage.xaml.
    /// Класс обеспечивает создание новых учетных записей пользователей с предварительной валидацией данных.
    /// </summary>
    public partial class RegistrationPage : Page
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса RegistrationPage.
        /// </summary>
        public RegistrationPage()
        {
            InitializeComponent();
            LoadRoles(); // Загрузка списка доступных ролей из базы данных
        }

        /// <summary>
        /// Загружает список ролей из базы данных и привязывает их к выпадающему списку cmbRole.
        /// </summary>
        private void LoadRoles()
        {
            try
            {
                using (var context = new BeermageEntities1())
                {
                    // Получаем все роли из таблицы Roles
                    cmbRole.ItemsSource = context.Roles.ToList();
                    if (cmbRole.Items.Count > 0)
                    {
                        cmbRole.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Информирование пользователя в случае ошибки подключения к БД
                lblMessage.Text = $"Ошибка загрузки ролей: {ex.Message}";
                lblMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                lblMessage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Обработчик изменения выбора роли в выпадающем списке.
        /// </summary>
        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = cmbRole.SelectedItem as Roles;
            if (selected != null)
            {
                lblMessage.Text = $"Выбрана роль: {selected.RoleName}";
                lblMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                lblMessage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку регистрации. 
        /// Выполняет сбор данных, валидацию, хеширование пароля и сохранение в БД.
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. Предварительная проверка выбора роли и ввода пароля
            var selectedRole = cmbRole.SelectedItem as Roles;
            if (selectedRole == null)
            {
                MessageBox.Show("Выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string password = txtPassword.Password.Trim();
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пароль обязателен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Создание ViewModel для передачи данных в валидатор
            var viewModel = new UserViewModel
            {
                Login = txtLogin.Text.Trim(),
                Surname = txtSurname.Text.Trim(),
                Name = txtName.Text.Trim(),
                Otchestvo = string.IsNullOrWhiteSpace(txtOtchestvo.Text) ? null : txtOtchestvo.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                Position = string.IsNullOrWhiteSpace(txtPosition.Text) ? null : txtPosition.Text.Trim(),
                RoleID = selectedRole.RoleID,
                Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Активен"
            };

            // 3. Вызов бизнес-логики валидации (UserViewModelValidator)
            try
            {
                var validator = new UserViewModelValidator();
                var errors = validator.Validate(viewModel);

                if (errors.Count > 0)
                {
                    // Сбор всех ошибок валидации в одну строку для вывода
                    string errorMessage = string.Join("\n", errors.Select(er =>
                        $"{(er.MemberNames.Any() ? $"{string.Join(", ", er.MemberNames)}: " : "")}{er.ErrorMessage}"));

                    MessageBox.Show(errorMessage, "Ошибки валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка валидации: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. Сохранение данных в базу данных
            try
            {
                using (var context = new BeermageEntities1())
                {
                    // Проверка уникальности логина
                    if (context.Users.Any(u => u.Login == viewModel.Login))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Создание сущности БД и хеширование пароля
                    var newUser = new Users
                    {
                        Login = viewModel.Login,
                        Surname = viewModel.Surname,
                        Name = viewModel.Name,
                        Otchestvo = viewModel.Otchestvo,
                        Email = viewModel.Email,
                        Phone = viewModel.Phone,
                        Position = viewModel.Position,
                        RoleID = viewModel.RoleID,
                        Status = viewModel.Status,
                        // Пароль сохраняется исключительно в виде SHA-256 хеша
                        PasswordHash = PasswordHasher.ComputeSha256Hash(password)
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges(); // Коммит транзакции в БД

                    MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переход на страницу входа после успешного завершения
                    NavigationService?.Navigate(new AuthPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Переход на страницу авторизации для уже зарегистрированных пользователей.
        /// </summary>
        private void BtnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthPage());
        }
    }
}