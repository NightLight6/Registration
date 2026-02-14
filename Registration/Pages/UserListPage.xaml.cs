using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Data.Entity;
using Registration.Model;

namespace Registration.Pages
{
    public partial class UserListPage : Page
    {
        private List<Users> _allUsers = new List<Users>();

        public UserListPage()
        {
            InitializeComponent();
            this.Loaded += UserListPage_Loaded;
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
                    context.Configuration.ProxyCreationEnabled = false;
                    context.Configuration.LazyLoadingEnabled = false;

                    var users = context.Users
                                       .Include(u => u.Roles)
                                       .AsNoTracking()
                                       .ToList();

                    _allUsers = users;

                    if (lvUsers != null)
                    {
                        lvUsers.ItemsSource = users;
                    }

                    var roles = context.Roles.AsNoTracking().ToList();

                    cmbRolesFilter.SelectionChanged -= cmbRolesFilter_SelectionChanged;

                    cmbRolesFilter.Items.Clear();
                    cmbRolesFilter.Items.Add(new ComboBoxItem { Content = "Все роли", Tag = "-1" });

                    foreach (var role in roles)
                    {
                        cmbRolesFilter.Items.Add(new ComboBoxItem
                        {
                            Content = role.RoleName,
                            Tag = role.RoleID.ToString()
                        });
                    }
                    cmbRolesFilter.SelectedIndex = 0;

                    cmbRolesFilter.SelectionChanged += cmbRolesFilter_SelectionChanged;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void FilterUsers()
        {
            if (lvUsers == null || cmbRolesFilter == null || txtSearch == null)
                return;

            string searchText = txtSearch.Text?.Trim().ToLower() ?? "";
            var selectedItem = cmbRolesFilter.SelectedItem as ComboBoxItem;
            string selectedTag = selectedItem?.Tag?.ToString() ?? "-1";

            var filtered = _allUsers.Where(u =>
                (string.IsNullOrEmpty(searchText) ||
                 (u.Login != null && u.Login.ToLower().Contains(searchText)) ||
                 (u.Name != null && u.Name.ToLower().Contains(searchText)) ||
                 (u.Surname != null && u.Surname.ToLower().Contains(searchText)) ||
                 (u.Email != null && u.Email.ToLower().Contains(searchText))) &&
                (selectedTag == "-1" || u.RoleID.ToString() == selectedTag)
            ).ToList();

            lvUsers.ItemsSource = filtered;
        }

        private void txtSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) => FilterUsers();

        private void cmbRolesFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterUsers();

        private void btnAdd_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new UserEditPage(null));

        private void lvUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvUsers.SelectedItem is Users user)
                NavigationService.Navigate(new UserEditPage(user));
        }

        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (lvUsers.SelectedItem is Users selectedUser)
                NavigationService.Navigate(new UserEditPage(selectedUser));
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(lvUsers.SelectedItem is Users selectedUser)) return;

            if (MessageBox.Show($"Удалить {selectedUser.Name}?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new BeermageEntities1())
                    {
                        var user = context.Users.FirstOrDefault(u => u.UserID == selectedUser.UserID);
                        if (user != null)
                        {
                            context.Users.Remove(user);
                            context.SaveChanges();
                            LoadData();
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }
    }
}