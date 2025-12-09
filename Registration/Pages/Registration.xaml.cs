using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Registration.Helpers;
using Registration.Model;

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
            string surname = txtSurname.Text.Trim();
            string name = txtName.Text.Trim();
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(surname) || string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                lblMessage.Text = "Обязательные поля: Фамилия, Имя, Логин, Пароль.";
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            var selectedRole = cmbRole.SelectedItem as Roles;
            if (selectedRole == null)
            {
                lblMessage.Text = "Выберите роль.";
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                using (var context = new BeermageEntities1())
                {
                    if (context.Users.Any(u => u.Login == login))
                    {
                        lblMessage.Text = "Пользователь с таким логином уже существует.";
                        lblMessage.Foreground = System.Windows.Media.Brushes.Orange;
                        return;
                    }

                    string passwordHash = PasswordHasher.ComputeSha256Hash(password);
                    var selectedItem = cmbStatus.SelectedItem as ComboBoxItem;
                    string status = selectedItem?.Content?.ToString() ?? "Активен";

                    var newUser = new Users
                    {
                        Login = login,
                        Surname = surname,
                        Name = name,
                        Otchestvo = string.IsNullOrWhiteSpace(txtOtchestvo.Text) ? null : txtOtchestvo.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                        Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                        Position = string.IsNullOrWhiteSpace(txtPosition.Text) ? null : txtPosition.Text.Trim(),
                        RoleID = selectedRole.RoleID,
                        PasswordHash = passwordHash,
                        Status = status
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    lblMessage.Text = "Пользователь успешно зарегистрирован!";
                    lblMessage.Foreground = System.Windows.Media.Brushes.Green;

                    txtSurname.Clear(); txtName.Clear(); txtOtchestvo.Clear();
                    txtLogin.Clear(); txtPassword.Clear(); txtEmail.Clear();
                    txtPhone.Clear(); txtPosition.Clear();
                    cmbRole.SelectedIndex = 0;
                    cmbStatus.SelectedIndex = 0;

                    NavigationService?.Navigate(new AuthPage());
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Ошибка: {ex.Message}";
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthPage());
        }
    }
}