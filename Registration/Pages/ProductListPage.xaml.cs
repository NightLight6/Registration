using Registration.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Registration.Services;
using System.Data.Entity;
using System.Windows.Navigation;
using System.Windows.Documents;

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
            try
            {
                using (var db = new BeermageEntities1())
                {
                    var products = db.Products
                        .Include(p => p.ProductCategories)
                        .ToList();

                    if (products.Count == 0)
                    {
                        MessageBox.Show("В базе данных нет товаров!");
                        return;
                    }

                    LvProducts.ItemsSource = products;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}\n{ex.InnerException?.Message}");
            }
        }

        private void PrintListButton_Click(object sender, RoutedEventArgs e)
        {
            FlowDocument doc = flowDocumentReader.Document;

            if (doc == null)
            {
                MessageBox.Show("Документ для печати не найден.");
                return;
            }

            PrintDialog printDialog = new PrintDialog();

            MessageBox.Show("В окне печати выберите «Microsoft Print to PDF» для сохранения в файл.",
                           "Совет", MessageBoxButton.OK, MessageBoxImage.Information);

            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idpSource = doc;

                printDialog.PrintDocument(
                    idpSource.DocumentPaginator,
                    "Список товаров — Beermage");

                MessageBox.Show("Документ успешно отправлен на печать!",
                               "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProductEditPage(null));
        }

        private void Border_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var product = border?.DataContext as Products;

            if (product == null) return;

            var contextMenu = new ContextMenu();

            var editItem = new MenuItem
            {
                Header = "Редактировать",
                FontWeight = FontWeights.SemiBold
            };
            editItem.Click += (s, args) => BtnEdit_Click_FromMenu(product);

            var deleteItem = new MenuItem
            {
                Header = "Удалить",
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("ErrorColor")
            };
            deleteItem.Click += (s, args) => BtnDelete_Click_FromMenu(product);

            contextMenu.Items.Add(editItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteItem);

            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void BtnEdit_Click_FromMenu(Products product)
        {
            if (product != null)
            {
                NavigationService.Navigate(new ProductEditPage(product));
            }
        }

        private void BtnDelete_Click_FromMenu(Products product)
        {
            if (product != null)
            {
                var result = MessageBox.Show(
                    $"Удалить товар «{product.Name}»?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var db = new BeermageEntities1())
                    {
                        var prodToDelete = db.Products
                            .FirstOrDefault(p => p.ProductID == product.ProductID);
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