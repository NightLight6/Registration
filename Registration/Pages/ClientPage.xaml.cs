using Registration.Model;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Data.Entity;
using Registration.Services;
using System.Windows.Navigation;

namespace Registration.Pages
{
    public partial class ClientPage : Page
    {
        public ClientPage(Users user, string roleName)
        {
            InitializeComponent();
            if (roleName == "Менеджер" || roleName == "Администратор" || roleName == "Продавец")
            {
                NavigationService.Navigate(new ProductListPage());
            }
            else
            {
                LoadProducts();
            }
        }

        private void LoadProducts()
        {
            using (var db = new BeermageEntities1())
            {
                var products = db.Products
                    .Include("ProductCategories")
                    .ToList();
                LvProducts.ItemsSource = products;
            }
        }
    }
}