using Registration.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.Windows.Navigation;

namespace Registration.Pages
{
    public partial class ClientPage : Page
    {
        private Users _currentUser;
        private string _roleName;

        public ClientPage(Users user, string roleName)
        {
            InitializeComponent();
            _currentUser = user;
            _roleName = roleName;
            this.Loaded += ClientPage_Loaded;
        }

        private void ClientPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_roleName == "Менеджер" || _roleName == "Администратор" || _roleName == "Продавец")
            {
                NavigationService?.Navigate(new ProductListPage());
            }
            else
            {
                LoadProducts();
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (var db = new BeermageEntities1())
                {
                    var products = db.Products
                        .Include(p => p.ProductCategories)
                        .ToList();

                    LvProducts.ItemsSource = products;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message + "\n" + ex.InnerException?.Message);
            }
        }
    }
}