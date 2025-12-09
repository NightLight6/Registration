using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Registration.Model;

namespace Registration.Pages
{
    public partial class UserEditPage : Page
    {
        private BeermageEntities1 _context = new BeermageEntities1();
        private Users _user;
        private string _selectedPhotoPath = null;

        public UserEditPage(Users user)
        {
            InitializeComponent();
            _user = user;
            LoadRoles();
            LoadFormData();
            if (_user != null)
            {
                txtLogin.Text = _user.Login;
                txtName.Text = _user.Name;
                txtSurname.Text = _user.Surname;
                txtOtchestvo.Text = _user.Otchestvo ?? "";
                txtEmail.Text = _user.Email ?? "";
                txtPhone.Text = _user.Phone ?? "";
                txtPosition.Text = _user.Position ?? "";
                cmbStatus.SelectedItem = cmbStatus.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == (_user.Status ?? "Активен"));

                if (_user.RoleID != 0)
                {
                    var roleItem = cmbRoles.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(r => (int)r.Tag == _user.RoleID);
                    if (roleItem != null)
                        cmbRoles.SelectedItem = roleItem;
                }
            }
            else
            {
                cmbStatus.SelectedIndex = 0;
            }
        }

        private void LoadRoles()
        {
            cmbRoles.Items.Clear();
            foreach (var role in _context.Roles)
            {
                cmbRoles.Items.Add(new ComboBoxItem { Content = role.RoleName, Tag = role.RoleID });
            }
        }

        private void LoadFormData()
        {
            if (_user != null)
            {
                txtLogin.Text = _user.Login;
                txtName.Text = _user.Name;
                txtSurname.Text = _user.Surname;
                txtOtchestvo.Text = _user.Otchestvo;
                txtEmail.Text = _user.Email;
                txtPhone.Text = _user.Phone;
                txtPosition.Text = _user.Position;
                cmbStatus.SelectedItem = cmbStatus.Items.Cast<ComboBoxItem>();
            }
            else
            {
                cmbStatus.SelectedIndex = 0;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtSurname.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Заполните обязательные поля (*).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbRoles.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string pwd = pbPassword.Password;
            string confirm = pbConfirm.Password;
            Users userToSave;

            if (_user == null)
            {
                userToSave = new Users();
                _context.Users.Add(userToSave);
            }
            else
            {
                userToSave = _context.Users.FirstOrDefault(u => u.UserID == _user.UserID);
                if (userToSave == null)
                {
                    MessageBox.Show("Пользователь не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            userToSave.Login = txtLogin.Text;
            userToSave.Name = txtName.Text;
            userToSave.Surname = txtSurname.Text;
            userToSave.Otchestvo = string.IsNullOrWhiteSpace(txtOtchestvo.Text) ? null : txtOtchestvo.Text;
            userToSave.Email = txtEmail.Text;
            userToSave.Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text;
            userToSave.Position = string.IsNullOrWhiteSpace(txtPosition.Text) ? null : txtPosition.Text;
            userToSave.Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Активен";
            userToSave.RoleID = (int)((cmbRoles.SelectedItem as ComboBoxItem)?.Tag as int?);

            if (_user == null)
            {
                userToSave.PasswordHash = pbPassword.Password;
            }

            if (!string.IsNullOrEmpty(_selectedPhotoPath))
            {
                string fileName = $"user_{(userToSave.UserID == 0 ? DateTime.Now.Ticks : userToSave.UserID)}_{System.IO.Path.GetFileName(_selectedPhotoPath)}";
                string destinationPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserPhotos", fileName);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destinationPath));
                System.IO.File.Copy(_selectedPhotoPath, destinationPath, true);
                userToSave.PhotoPath = $"UserPhotos/{fileName}";
            }
            else if (_user != null && string.IsNullOrEmpty(_selectedPhotoPath))
            {
            }

            try
            {
                _context.SaveChanges();
                MessageBox.Show("Сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null)
            {
                foreach (var ctrl in this.FindVisualChildren<TextBox>()) ctrl.Clear();
                pbPassword.Clear(); pbConfirm.Clear();
                cmbRoles.SelectedIndex = -1;
                cmbStatus.SelectedIndex = 0;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
        private void btnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*";

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
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }
    }
}