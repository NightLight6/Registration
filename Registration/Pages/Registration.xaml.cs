using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel.DataAnnotations;
using Registration.Model;
using Registration.Services;
using Registration.Helpers;
using Registration.UserViewModelValidators;

namespace Registration.Pages
{
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                using (var context = new BeermageEntities1())
                {
                    cmbRole.ItemsSource = context.Roles.ToList();
                    if (cmbRole.Items.Count > 0)
                    {
                        cmbRole.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Ошибка загрузки ролей: {ex.Message}";
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = cmbRole.SelectedItem as Roles;
            if (selected != null)
            {
                lblMessage.Text = $"Выбрана роль: {selected.RoleName}";
                lblMessage.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
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

            try
            {
                var validator = new UserViewModelValidator(); // ← убедитесь, что имя класса и using правильные
                var errors = validator.Validate(viewModel);

                if (errors.Count > 0)
                {
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

            try
            {
                using (var context = new BeermageEntities1())
                {
                    if (context.Users.Any(u => u.Login == viewModel.Login))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

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
                        PasswordHash = PasswordHasher.ComputeSha256Hash(password)
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService?.Navigate(new AuthPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthPage());
        }
    }
}