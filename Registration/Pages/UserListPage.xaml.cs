using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Registration.Services;
using Registration.Model;

namespace Registration.Pages
{
    public partial class UserListPage : Page
    {
        private BeermageEntities1 _context = new BeermageEntities1();
        private IQueryable<Users> _allUsers = Enumerable.Empty<Users>().AsQueryable();

        public UserListPage()
        {
            InitializeComponent();
            this.Loaded += (s, e) => LoadData();
        }
        private void UserListPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        private void LoadData()
        {
            try
            {
                using (var context = new BeermageEntities1())
                {
                    var users = context.Users.Include("Roles").ToList();

                    _allUsers = users.AsQueryable();
                    lvUsers.ItemsSource = users;

                    cmbRolesFilter.Items.Clear();
                    cmbRolesFilter.Items.Add(new ComboBoxItem { Content = "Все роли", Tag = "-1" });
                    foreach (var role in context.Roles)
                    {
                        cmbRolesFilter.Items.Add(new ComboBoxItem { Content = role.RoleName, Tag = role.RoleID.ToString() });
                    }
                    cmbRolesFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                lvUsers.ItemsSource = new List<Users>();
            }
        }

        private void FilterUsers()
        {
            string searchText = txtSearch.Text?.ToLower() ?? "";
            string selectedTag = (cmbRolesFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            var filtered = _allUsers.Where(u =>
                (string.IsNullOrEmpty(searchText) ||
                 u.Login.ToLower().Contains(searchText) ||
                 u.Name.ToLower().Contains(searchText) ||
                 u.Surname.ToLower().Contains(searchText) ||
                 (u.Email != null && u.Email.ToLower().Contains(searchText))) &&
                (selectedTag == "-1" || u.RoleID.ToString() == selectedTag)
            );

            if (lvUsers != null)
            {
                lvUsers.ItemsSource = filtered.ToList();
            }
        }

        private void txtSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            FilterUsers();
        }

        private void cmbRoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterUsers();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new UserEditPage(null));
        }

        private void lvUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvUsers.SelectedItem is Users user)
            {
                NavigationService.Navigate(new UserEditPage(user));
            }
        }
        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var selectedUser = lvUsers.SelectedItem as Users;
            if (selectedUser == null) return;

            NavigationService.Navigate(new UserEditPage(selectedUser));
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var selectedUser = lvUsers.SelectedItem as Users;
            if (selectedUser == null) return;

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить сотрудника {selectedUser.Name} {selectedUser.Surname}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new BeermageEntities1())
                    {
                        var userToDelete = context.Users.FirstOrDefault(u => u.UserID == selectedUser.UserID);
                        if (userToDelete != null)
                        {
                            context.Users.Remove(userToDelete);
                            context.SaveChanges();

                            LoadData();
                            MessageBox.Show("Сотрудник успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void lvUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cmbRolesFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}