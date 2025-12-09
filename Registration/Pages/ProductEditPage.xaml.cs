using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Registration.Model;

namespace Registration.Pages
{
    public partial class ProductEditPage : Page
    {
        private Products _product;
        private string _selectedImagePath;

        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(ProductEditPage),
                new PropertyMetadata("/Images/no_product.png"));

        public ProductEditPage(Products product)
        {
            InitializeComponent();
            _product = product;
            LoadCategories();
            LoadBeverageTypes();
            LoadData();
            Title = _product == null ? "Добавление товара" : "Редактирование товара";
        }

        private void LoadCategories()
        {
            using (var db = new BeermageEntities1())
            {
                var categories = db.ProductCategories.ToList();
                CmbCategory.ItemsSource = categories;
            }
        }

        private void LoadBeverageTypes()
        {
            using (var db = new BeermageEntities1())
            {
                var types = db.BeverageTypes.ToList();
                CmbBeverageType.ItemsSource = types;
            }
        }

        private void LoadData()
        {
            if (_product != null)
            {
                TxtName.Text = _product.Name;
                TxtDescription.Text = _product.Description ?? "";
                TxtPrice.Text = _product.Price.ToString("F2");
                TxtCostPrice.Text = _product.CostPrice?.ToString("F2") ?? "";
                ChkIsAvailable.IsChecked = _product.IsAvailable;
                CmbCategory.SelectedValue = _product.CategoryID;
                CmbBeverageType.SelectedValue = _product.BeverageTypeID;

                ImagePath = _product.PhotoPath ?? "/Images/no_product.png";
            }
            else
            {
                ImagePath = "/Images/no_product.png";
                ChkIsAvailable.IsChecked = true;
            }
        }

        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                ImagePath = _selectedImagePath;

                imgPhoto.Visibility = Visibility.Visible;
                lblPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Название товара обязательно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCategory = CmbCategory.SelectedItem as ProductCategories;
            if (selectedCategory == null)
            {
                MessageBox.Show("Выберите категорию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedBeverageType = CmbBeverageType.SelectedItem as BeverageTypes;
            if (selectedBeverageType == null)
            {
                MessageBox.Show("Выберите тип напитка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Цена должна быть положительным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? costPrice = null;
            if (!string.IsNullOrWhiteSpace(TxtCostPrice.Text))
            {
                if (!decimal.TryParse(TxtCostPrice.Text, out decimal parsedCost) || parsedCost < 0)
                {
                    MessageBox.Show("Себестоимость должна быть неотрицательным числом или пустой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                costPrice = parsedCost;
            }

            try
            {
                using (var db = new BeermageEntities1())
                {
                    Products productToSave;

                    if (_product == null)
                    {
                        productToSave = new Products();
                        db.Products.Add(productToSave);
                    }
                    else
                    {
                        productToSave = db.Products.FirstOrDefault(p => p.ProductID == _product.ProductID);
                        if (productToSave == null)
                        {
                            MessageBox.Show("Товар не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    productToSave.Name = TxtName.Text.Trim();
                    productToSave.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                    productToSave.Price = price;
                    productToSave.CostPrice = costPrice;
                    productToSave.IsAvailable = ChkIsAvailable.IsChecked ?? false;
                    productToSave.CategoryID = selectedCategory.CategoryID;
                    productToSave.BeverageTypeID = selectedBeverageType.BeverageTypeID;

                    if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
                    {
                        string fileName = $"product_{(productToSave.ProductID == 0 ? DateTime.Now.Ticks : productToSave.ProductID)}_{Path.GetFileName(_selectedImagePath)}";
                        string assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                        Directory.CreateDirectory(assetsFolder);
                        string destinationPath = Path.Combine(assetsFolder, fileName);

                        File.Copy(_selectedImagePath, destinationPath, true);
                        productToSave.PhotoPath = $"Images/{fileName}";

                        ImagePath = productToSave.PhotoPath;
                    }
                    else if (_product?.PhotoPath != null)
                    {
                        productToSave.PhotoPath = _product.PhotoPath;
                    }
                    else
                    {
                        productToSave.PhotoPath = null;
                        ImagePath = "/Images/no_product.png";
                    }

                    db.SaveChanges();
                }

                MessageBox.Show("Товар успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null)
            {
                TxtName.Clear();
                TxtDescription.Clear();
                TxtPrice.Clear();
                TxtCostPrice.Clear();
                ChkIsAvailable.IsChecked = true;
                CmbCategory.SelectedIndex = -1;
                CmbBeverageType.SelectedIndex = -1;
                ImagePath = "/Images/no_product.png";
            }
            else
            {
                LoadData();
            }
        }
    }
}