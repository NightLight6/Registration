using Registration.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Registration.Services;
using System.Windows.Navigation;

namespace Registration.Pages
{
    public partial class ProductListPage : Page
    {
        public ProductListPage()
        {
            InitializeComponent();
            this.Loaded += (s, e) => LoadProducts();
        }

        private void LoadProducts()
        {
            using (var db = new BeermageEntities1())
            {
                var products = db.Products
                    .Include("ProductCategories")
                    .Include("BeverageTypes")
                    .ToList();
                LvProducts.ItemsSource = products;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductEditPage(null));
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as Products;
            if (product != null)
            {
                NavigationService.Navigate(new ProductEditPage(product));
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as Products;
            if (product != null)
            {
                var result = MessageBox.Show($"Удалить товар '{product.Name}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    using (var db = new BeermageEntities1())
                    {
                        var prodToDelete = db.Products.FirstOrDefault(p => p.ProductID == product.ProductID);
                        if (prodToDelete != null)
                        {
                            db.Products.Remove(prodToDelete);
                            db.SaveChanges();
                            LoadProducts();
                        }
                    }
                }
            }
        }
    }
}