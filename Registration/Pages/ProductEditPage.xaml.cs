using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Registration.Model;

namespace Registration.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProductEditPage.xaml.
    /// Страница предназначена для создания новых и редактирования существующих товаров.
    /// </summary>
    public partial class ProductEditPage : Page
    {
        private Products _product;
        private string _selectedImagePath;

        /// <summary>
        /// Свойство зависимости для хранения и отображения пути к изображению товара.
        /// </summary>
        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(ProductEditPage),
                new PropertyMetadata("/Images/no_product.png"));

        /// <summary>
        /// Конструктор страницы редактирования товара.
        /// </summary>
        /// <param name="product">Объект товара для редактирования или null для создания нового.</param>
        public ProductEditPage(Products product)
        {
            InitializeComponent();
            _product = product;
            LoadCategories();
            LoadBeverageTypes();
            LoadData();

            // Установка заголовка в зависимости от режима (добавление/редактирование)
            Title = _product == null ? "Добавление товара" : "Редактирование товара";
        }

        /// <summary>
        /// Загружает список категорий из базы данных для выпадающего списка.
        /// </summary>
        private void LoadCategories()
        {
            using (var db = new BeermageEntities1())
            {
                var categories = db.ProductCategories.ToList();
                CmbCategory.ItemsSource = categories;
            }
        }

        /// <summary>
        /// Загружает типы напитков из базы данных для выпадающего списка.
        /// </summary>
        private void LoadBeverageTypes()
        {
            using (var db = new BeermageEntities1())
            {
                var types = db.BeverageTypes.ToList();
                CmbBeverageType.ItemsSource = types;
            }
        }

        /// <summary>
        /// Заполняет поля формы данными редактируемого товара.
        /// </summary>
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

                // Если фото отсутствует, используем стандартную заглушку
                ImagePath = _product.PhotoPath ?? "/Images/no_product.png";
            }
            else
            {
                ImagePath = "/Images/no_product.png";
                ChkIsAvailable.IsChecked = true;
            }
        }

        /// <summary>
        /// Открывает диалоговое окно для выбора изображения товара на диске.
        /// </summary>
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

        /// <summary>
        /// Выполняет валидацию введенных данных и сохраняет товар в базу данных.
        /// Включает логику копирования файла изображения в директорию приложения.
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // --- Проверки валидности данных ---
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

                    // Определение: создание нового товара или поиск существующего для обновления
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

                    // Присвоение значений из элементов интерфейса
                    productToSave.Name = TxtName.Text.Trim();
                    productToSave.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                    productToSave.Price = price;
                    productToSave.CostPrice = costPrice;
                    productToSave.IsAvailable = ChkIsAvailable.IsChecked ?? false;
                    productToSave.CategoryID = selectedCategory.CategoryID;
                    productToSave.BeverageTypeID = selectedBeverageType.BeverageTypeID;

                    // Логика обработки изображения
                    if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
                    {
                        // Формирование уникального имени файла
                        string fileName = $"product_{(productToSave.ProductID == 0 ? DateTime.Now.Ticks : productToSave.ProductID)}_{Path.GetFileName(_selectedImagePath)}";
                        string assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

                        // Создание папки, если она не существует
                        Directory.CreateDirectory(assetsFolder);
                        string destinationPath = Path.Combine(assetsFolder, fileName);

                        // Копирование файла в ресурсы приложения
                        File.Copy(_selectedImagePath, destinationPath, true);
                        productToSave.PhotoPath = $"Images/{fileName}";

                        ImagePath = productToSave.PhotoPath;
                    }
                    else if (_product?.PhotoPath != null)
                    {
                        productToSave.PhotoPath = _product.PhotoPath;
                    }

                    db.SaveChanges(); // Коммит изменений в БД
                }

                MessageBox.Show("Товар успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сбрасывает введенные данные или возвращает форму к исходному состоянию товара.
        /// </summary>
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
                LoadData(); // Перезагрузка исходных данных из объекта
            }
        }
    }
}