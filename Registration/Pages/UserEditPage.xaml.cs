using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Registration.Helpers;
using Registration.Model;
using Registration.Services;
using Registration.UserViewModelValidators;

namespace Registration.Pages
{
    public partial class UserEditPage : Page
    {
        private Users _user;
        private string _selectedPhotoPath = null;

        public UserEditPage(Users user)
        {
            InitializeComponent();
            _user = user;
            LoadRoles();
            LoadFormData();
        }

        private void LoadRoles()
        {
            cmbRoles.Items.Clear();
            using (var context = new BeermageEntities1())
            {
                foreach (var role in context.Roles)
                {
                    cmbRoles.Items.Add(new ComboBoxItem { Content = role.RoleName, Tag = role.RoleID });
                }
            }
        }

        private void LoadFormData()
        {
            if (_user != null)
            {
                txtLogin.Text = _user.Login ?? "";
                txtSurname.Text = _user.Surname ?? "";
                txtName.Text = _user.Name ?? "";
                txtOtchestvo.Text = _user.Otchestvo ?? "";
                txtEmail.Text = _user.Email ?? "";
                txtPhone.Text = _user.Phone ?? "";
                txtPosition.Text = _user.Position ?? "";

                var statusItem = cmbStatus.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == (_user.Status ?? "Активен"));
                cmbStatus.SelectedItem = statusItem ?? cmbStatus.Items[0];

                var roleItem = cmbRoles.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(r => (int)r.Tag == _user.RoleID);
                if (roleItem != null)
                    cmbRoles.SelectedItem = roleItem;
            }
            else
            {
                cmbStatus.SelectedIndex = 0;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoleItem = cmbRoles.SelectedItem as ComboBoxItem;
            if (selectedRoleItem == null)
            {
                MessageBox.Show("Выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string passwordHash = _user?.PasswordHash;

            if (_user == null)
            {
                string pwd = pbPassword.Password;
                string confirm = pbConfirm.Password;

                if (string.IsNullOrWhiteSpace(pwd))
                {
                    MessageBox.Show("Пароль обязателен при создании пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (pwd != confirm)
                {
                    MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                passwordHash = PasswordHasher.ComputeSha256Hash(pwd);
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
                RoleID = (int)selectedRoleItem.Tag,
                Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Активен",
                PasswordHash = passwordHash
            };

            try
            {
                var validator = new UserViewModelValidator();
                var errors = validator.Validate(viewModel);

                if (errors.Count > 0)
                {
                    string errorMsg = string.Join("\n", errors.Select(er =>
                        $"{(er.MemberNames.Any() ? $"{string.Join(", ", er.MemberNames)}: " : "")}{er.ErrorMessage}"));

                    MessageBox.Show($"Ошибки ввода:\n{errorMsg}",
                                    "Ошибка валидации",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при валидации: {ex.Message}",
                                "Критическая ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new BeermageEntities1())
                {
                    Users userToSave;
                    if (_user == null)
                    {
                        userToSave = new Users();
                        context.Users.Add(userToSave);
                    }
                    else
                    {
                        userToSave = context.Users.FirstOrDefault(u => u.UserID == _user.UserID);
                        if (userToSave == null)
                        {
                            MessageBox.Show("Пользователь не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    userToSave.Login = viewModel.Login;
                    userToSave.Surname = viewModel.Surname;
                    userToSave.Name = viewModel.Name;
                    userToSave.Otchestvo = viewModel.Otchestvo;
                    userToSave.Email = viewModel.Email;
                    userToSave.Phone = viewModel.Phone;
                    userToSave.Position = viewModel.Position;
                    userToSave.RoleID = viewModel.RoleID;
                    userToSave.Status = viewModel.Status;
                    userToSave.PasswordHash = viewModel.PasswordHash;

                    if (!string.IsNullOrEmpty(_selectedPhotoPath))
                    {
                        string fileName = $"user_{(userToSave.UserID == 0 ? DateTime.Now.Ticks : userToSave.UserID)}_{Path.GetFileName(_selectedPhotoPath)}";
                        string photosDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserPhotos");
                        Directory.CreateDirectory(photosDir);
                        string destinationPath = Path.Combine(photosDir, fileName);
                        File.Copy(_selectedPhotoPath, destinationPath, true);
                        userToSave.PhotoPath = $"UserPhotos/{fileName}";
                    }

                    context.SaveChanges();
                    MessageBox.Show("Сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null)
            {
                foreach (var ctrl in this.FindVisualChildren<TextBox>()) ctrl.Clear();
                pbPassword.Clear();
                pbConfirm.Clear();
                cmbRoles.SelectedIndex = -1;
                cmbStatus.SelectedIndex = 0;
            }
            else
            {
                LoadFormData();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void btnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedPhotoPath = openFileDialog.FileName;
                imgPhoto.Source = new BitmapImage(new Uri(_selectedPhotoPath));
                imgPhoto.Visibility = Visibility.Visible;
                lblPlaceholder.Visibility = Visibility.Collapsed;
            }
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }
    }
}