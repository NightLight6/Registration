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
            using (var context = new BeermagEntities2())
            {
                cmbRole.ItemsSource = context.Roles.ToList();
                cmbRole.SelectedIndex = 0;
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
            string otchestvo = txtOtchestvo.Text.Trim();
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            var selectedRole = cmbRole.SelectedItem as Roles;

            if (string.IsNullOrEmpty(surname) || string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(otchestvo) || string.IsNullOrEmpty(login) ||
                string.IsNullOrEmpty(password) || selectedRole == null)
            {
                lblMessage.Text = "Все поля обязательны для заполнения.";
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                using (var context = new BeermagEntities2())
                {
                    if (context.Users.Any(u => u.Login == login))
                    {
                        lblMessage.Text = "Пользователь с таким логином уже существует.";
                        lblMessage.Foreground = System.Windows.Media.Brushes.Orange;
                        return;
                    }

                    string passwordHash = PasswordHasher.ComputeSha256Hash(password);

                    var newUser = new Users
                    {
                        Login = login,
                        Surname = surname,
                        Name = name,
                        Otchestvo = otchestvo,
                        RoleID = selectedRole.RoleID,
                        PasswordHash = passwordHash
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    lblMessage.Text = "Пользователь успешно зарегистрирован!";
                    lblMessage.Foreground = System.Windows.Media.Brushes.Green;

                    // Очистка полей
                    txtSurname.Clear();
                    txtName.Clear();
                    txtOtchestvo.Clear();
                    txtLogin.Clear();
                    txtPassword.Clear();
                    cmbRole.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Ошибка: {ex.Message}";
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
            NavigationService?.Navigate(new AuthPage());
        }
        private void BtnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthPage());
        }
    }
}