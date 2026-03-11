using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Data.Entity;
using Registration.Model;
using System.Windows.Documents;

namespace Registration.Pages
{
    /// <summary>
    /// Логика взаимодействия для UserListPage.xaml.
    /// Представляет собой страницу со списком всех пользователей, функциями поиска, фильтрации и управления записями.
    /// </summary>
    public partial class UserListPage : Page
    {
        /// <summary>
        /// Локальный кэш списка пользователей для обеспечения быстрой фильтрации без повторных запросов к БД.
        /// </summary>
        private List<Users> _allUsers = new List<Users>();

        public UserListPage()
        {
            InitializeComponent();
            this.Loaded += UserListPage_Loaded;
        }

        /// <summary>
        /// Обработчик события загрузки страницы. Инициирует получение данных из БД.
        /// </summary>
        private void UserListPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        /// <summary>
        /// Извлекает данные пользователей и ролей из базы данных, настраивает контекст и заполняет элементы управления.
        /// </summary>
        private void LoadData()
        {
            try
            {
                using (var context = new BeermageEntities1())
                {
                    // Оптимизация Entity Framework для ускорения загрузки (без отслеживания изменений и прокси)
                    context.Configuration.ProxyCreationEnabled = false;
                    context.Configuration.LazyLoadingEnabled = false;

                    // Загрузка пользователей вместе с их ролями
                    var users = context.Users
                                       .Include(u => u.Roles)
                                       .AsNoTracking()
                                       .ToList();

                    _allUsers = users;

                    if (lvUsers != null)
                    {
                        lvUsers.ItemsSource = users;
                    }

                    // Настройка фильтра ролей
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
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Выполняет фильтрацию списка пользователей по введенному тексту (логин, имя, почта) и выбранной роли.
        /// </summary>
        private void FilterUsers()
        {
            if (lvUsers == null || cmbRolesFilter == null || txtSearch == null)
                return;

            string searchText = txtSearch.Text?.Trim().ToLower() ?? "";
            var selectedItem = cmbRolesFilter.SelectedItem as ComboBoxItem;
            string selectedTag = selectedItem?.Tag?.ToString() ?? "-1";

            // Применение условий фильтрации к коллекции в памяти
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

        // Обработчики событий ввода и выбора для мгновенного обновления списка
        private void txtSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) => FilterUsers();
        private void cmbRolesFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterUsers();

        /// <summary>
        /// Переход на страницу добавления нового пользователя.
        /// </summary>
        private void btnAdd_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new UserEditPage(null));

        /// <summary>
        /// Обработчик двойного клика по строке списка для редактирования выбранного пользователя.
        /// </summary>
        private void lvUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvUsers.SelectedItem is Users user)
                NavigationService.Navigate(new UserEditPage(user));
        }

        /// <summary>
        /// Команда контекстного меню для изменения данных пользователя.
        /// </summary>
        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (lvUsers.SelectedItem is Users selectedUser)
                NavigationService.Navigate(new UserEditPage(selectedUser));
        }

        /// <summary>
        /// Удаляет выбранного пользователя из базы данных после подтверждения.
        /// </summary>
        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(lvUsers.SelectedItem is Users selectedUser)) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {selectedUser.Name} {selectedUser.Surname}?",
                                         "Подтверждение удаления",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
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
                            LoadData(); // Перезагрузка актуального списка
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void PrintListButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FlowDocument doc = flowDocumentReader.Document;

                if (doc == null)
                {
                  MessageBox.Show("Документ не найден.");
            return;
                }

                PrintDialog printDialog = new PrintDialog();

                doc.PagePadding = new Thickness(50);
                doc.ColumnGap = 0;
                doc.ColumnWidth = printDialog.PrintableAreaWidth;

                 if (printDialog.ShowDialog() == true)
                 {
                    IDocumentPaginatorSource idpSource = doc;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, "Список сотрудников");
                 }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке к печати: {ex.Message}");
            }
        }
        private void lvUsers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}