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

            this.Loaded += UserEditPage_Loaded;
        }

        private void UserEditPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoles();
            LoadFormData();
        }

        private void LoadRoles()
        {
            try
            {
                using (var context = new BeermageEntities1())
                {
                    context.Configuration.ProxyCreationEnabled = false;
                    var roles = context.Roles.AsNoTracking().ToList();

                    cmbRoles.Items.Clear();
                    foreach (var role in roles)
                    {
                        cmbRoles.Items.Add(new ComboBoxItem
                        {
                            Content = role.RoleName,
                            Tag = role.RoleID
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}");
            }
        }

        private void LoadFormData()
        {
            if (_user == null)
            {
                cmbStatus.SelectedIndex = 0;
                imgPhoto.Visibility = Visibility.Collapsed;
                lblPlaceholder.Visibility = Visibility.Visible;
                return;
            }

            txtLogin.Text = _user.Login ?? "";
            txtSurname.Text = _user.Surname ?? "";
            txtName.Text = _user.Name ?? "";
            txtOtchestvo.Text = _user.Otchestvo ?? "";
            txtEmail.Text = _user.Email ?? "";
            txtPhone.Text = _user.Phone ?? "";
            txtPosition.Text = _user.Position ?? "";

            foreach (ComboBoxItem item in cmbStatus.Items)
            {
                if (item.Content.ToString() == (_user.Status ?? "Активен"))
                {
                    cmbStatus.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in cmbRoles.Items)
            {
                if (item.Tag != null && (int)item.Tag == _user.RoleID)
                {
                    cmbRoles.SelectedItem = item;
                    break;
                }
            }

            LoadUserPhoto();

        }

        private void LoadUserPhoto()
        {
            if (!string.IsNullOrEmpty(_user.PhotoPath))
            {
                try
                {
                    string photoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _user.PhotoPath);
                    if (File.Exists(photoPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(photoPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        imgPhoto.Source = bitmap;
                        imgPhoto.Visibility = Visibility.Visible;
                        lblPlaceholder.Visibility = Visibility.Collapsed;
                    }
                }
                catch
                {
                    ShowPlaceholder();
                }
            }
            else
            {
                ShowPlaceholder();
            }
        }

        private void ShowPlaceholder()
        {
            imgPhoto.Visibility = Visibility.Collapsed;
            lblPlaceholder.Visibility = Visibility.Visible;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoleItem = cmbRoles.SelectedItem as ComboBoxItem;
            if (selectedRoleItem == null)
            {
                MessageBox.Show("Выберите роль.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string passwordHash = _user?.PasswordHash;
            if (!string.IsNullOrWhiteSpace(pbPassword.Password))
            {
                if (pbPassword.Password != pbConfirm.Password)
                {
                    MessageBox.Show("Пароли не совпадают!");
                    return;
                }
                passwordHash = PasswordHasher.ComputeSha256Hash(pbPassword.Password);
            }
            else if (_user == null)
            {
                MessageBox.Show("Пароль обязателен для нового пользователя!");
                return;
            }

            var viewModel = new UserViewModel
            {
                Login = txtLogin.Text.Trim(),
                Surname = txtSurname.Text.Trim(),
                Name = txtName.Text.Trim(),
                Otchestvo = txtOtchestvo.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Position = txtPosition.Text.Trim(),
                RoleID = (int)selectedRoleItem.Tag,
                Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                PasswordHash = passwordHash
            };

            SaveToDatabase(viewModel);
        }

        private void SaveToDatabase(UserViewModel viewModel)
        {
            try
            {
                using (var context = new BeermageEntities1())
                {
                    Users userToSave = _user == null
                        ? new Users()
                        : context.Users.FirstOrDefault(u => u.UserID == _user.UserID);

                    if (userToSave == null) return;

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
                        string fileName = $"user_{Guid.NewGuid()}{Path.GetExtension(_selectedPhotoPath)}";
                        string photosDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserPhotos");
                        Directory.CreateDirectory(photosDir);
                        string dest = Path.Combine(photosDir, fileName);
                        File.Copy(_selectedPhotoPath, dest, true);
                        userToSave.PhotoPath = $"UserPhotos/{fileName}";
                    }

                    if (_user == null) context.Users.Add(userToSave);
                    context.SaveChanges();

                    MessageBox.Show("Данные сохранены успешно!");
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void btnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var op = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.png;*.jpeg" };
            if (op.ShowDialog() == true)
            {
                _selectedPhotoPath = op.FileName;
                imgPhoto.Source = new BitmapImage(new Uri(_selectedPhotoPath));
                imgPhoto.Visibility = Visibility.Visible;
                lblPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();

        private void btnClear_Click(object sender, RoutedEventArgs e) => LoadFormData();
    }
}