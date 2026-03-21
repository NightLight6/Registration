using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using Registration.Model;
using Microsoft.Win32;
using System.Windows.Navigation;

namespace Registration.Pages
{
    public partial class EmploymentContractPage : Page
    {
        private Users _employee;
        private readonly CompanyData _companyData;

        public EmploymentContractPage(Users employee)
        {
            InitializeComponent();
            _employee = employee;

            _companyData = new CompanyData
            {
                Name = "ООО \"BEERMAGE\"",
                Director = "Иванов Иван Иванович",
                INN = "5401234567",
                KPP = "540101001",
                Address = "г. Новосибирск, ул. Примерная, д. 1",
                City = "Новосибирск"
            };

            string fullName = $"{_employee.Surname} {_employee.Name} {_employee.Otchestvo ?? ""}".Trim();
            EmployeeNameTextBlock.Text = fullName;
            EmployeePositionTextBlock.Text = _employee.Roles?.RoleName ?? "Не указана";
            DepartmentNameTextBlock.Text = "Отдел продаж";
            EmployeeHeaderBlock.Text = $"Сотрудник: {fullName}";

            ContractDateTextBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
            StartDateTextBox.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
        }

        private void CreateContractButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContractNumberTextBox.Text) ||
                string.IsNullOrWhiteSpace(CityTextBox.Text) ||
                string.IsNullOrWhiteSpace(ContractDateTextBox.Text) ||
                string.IsNullOrWhiteSpace(SalaryTextBox.Text) ||
                string.IsNullOrWhiteSpace(StartDateTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmployerNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(DirectorNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля (отмечены *)",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                FlowDocument contractDocument = CreateEmploymentContract();

                if (SaveContractDocument(contractDocument))
                {
                    MessageBox.Show("Трудовой договор успешно сформирован!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (NavigationService?.CanGoBack == true)
                    {
                        NavigationService.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании договора: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateEmploymentContract()
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
            doc.FontSize = 14;
            doc.PageWidth = 816; // A4 при 96 DPI
            doc.PageHeight = 1056;
            doc.PagePadding = new Thickness(50);
            doc.ColumnWidth = 716;
            doc.TextAlignment = TextAlignment.Justify;

            Paragraph titlePara = new Paragraph();
            titlePara.TextAlignment = TextAlignment.Center;
            titlePara.FontSize = 16;
            titlePara.FontWeight = FontWeights.Bold;
            titlePara.Margin = new Thickness(0, 0, 0, 30);
            titlePara.Inlines.Add(new Bold(new Run("ТРУДОВОЙ ДОГОВОР № " + ContractNumberTextBox.Text)));
            doc.Blocks.Add(titlePara);

            Paragraph cityDatePara = new Paragraph();
            cityDatePara.TextAlignment = TextAlignment.Right;
            cityDatePara.Margin = new Thickness(0, 0, 0, 20);
            cityDatePara.Inlines.Add(new Run($"г. {CityTextBox.Text}"));
            cityDatePara.Inlines.Add(new Run(" "));
            cityDatePara.Inlines.Add(new Run($"«{GetDay(ContractDateTextBox.Text)}» {GetMonth(ContractDateTextBox.Text)} 20{GetYear(ContractDateTextBox.Text)} г."));
            doc.Blocks.Add(cityDatePara);

            string fullName = $"{_employee.Surname} {_employee.Name} {_employee.Otchestvo ?? ""}".Trim();
            Paragraph preamblePara = new Paragraph();
            preamblePara.TextAlignment = TextAlignment.Justify;
            preamblePara.Margin = new Thickness(0, 0, 0, 20);
            preamblePara.Inlines.Add(new Run($"{EmployerNameTextBox.Text}, именуемое в дальнейшем «Работодатель», в лице генерального директора {DirectorNameTextBox.Text}, действующего на основании Устава, с одной стороны, и гражданин(ка) {fullName}, именуемый(ая) в дальнейшем «Работник», с другой стороны, заключили настоящий Трудовой договор о нижеследующем:"));
            doc.Blocks.Add(preamblePara);

            AddSection(doc, "1. ПРЕДМЕТ ТРУДОВОГО ДОГОВОРА", new[]
            {
                $"Работник принимается на работу в {EmployerNameTextBox.Text} на должность: {EmployeePositionTextBlock.Text}",
                $"Место работы: {_companyData.Address}",
                "Настоящий Трудовой договор является договором по основной работе.",
                "Настоящий Трудовой договор заключен на неопределенный срок.",
                $"Дата начала работы: «{GetDay(StartDateTextBox.Text)}» {GetMonth(StartDateTextBox.Text)} 20{GetYear(StartDateTextBox.Text)} года.",
                $"Продолжительность испытания при приеме на работу: {TestPeriodTextBox.Text} мес."
            });

            AddSection(doc, "2. ПРАВА И ОБЯЗАННОСТИ РАБОТНИКА", new[]
            {
                "Работник имеет право на: своевременную выплату заработной платы, предоставление работы, отдых, охрану труда и другие права, предусмотренные ТК РФ.",
                "Работник обязан: добросовестно исполнять трудовые обязанности, соблюдать трудовую дисциплину, бережно относиться к имуществу работодателя."
            });

            Paragraph salaryPara = new Paragraph();
            salaryPara.TextAlignment = TextAlignment.Justify;
            salaryPara.Margin = new Thickness(0, 0, 0, 20);
            salaryPara.Inlines.Add(new Bold(new Run("3. ОПЛАТА ТРУДА")));
            salaryPara.Inlines.Add(new LineBreak());
            salaryPara.Inlines.Add(new LineBreak());
            salaryPara.Inlines.Add(new Run($"За выполнение обязанностей, предусмотренных настоящим Трудовым договором, Работнику устанавливается должностной оклад в размере {SalaryTextBox.Text} ( {NumberToWords(decimal.Parse(SalaryTextBox.Text))} ) рублей ежемесячно."));
            doc.Blocks.Add(salaryPara);

            AddSection(doc, "4. РЕЖИМ РАБОЧЕГО ВРЕМЕНИ И ВРЕМЕНИ ОТДЫХА", new[]
            {
                "Работнику устанавливается пятидневная рабочая неделя продолжительностью 40 часов.",
                "Работнику устанавливается ежегодный основной оплачиваемый отпуск продолжительностью 28 календарных дней."
            });

            AddSignatures(doc, fullName);

            return doc;
        }

        private void AddSection(FlowDocument doc, string title, string[] items)
        {
            Paragraph titlePara = new Paragraph();
            titlePara.FontWeight = FontWeights.Bold;
            titlePara.Margin = new Thickness(0, 20, 0, 10);
            titlePara.Inlines.Add(new Run(title));
            doc.Blocks.Add(titlePara);

            int num = 1;
            foreach (var item in items)
            {
                Paragraph para = new Paragraph();
                para.TextAlignment = TextAlignment.Justify;
                para.Margin = new Thickness(20, 0, 0, 5);
                para.Inlines.Add(new Run($"{num}. {item}"));
                doc.Blocks.Add(para);
                num++;
            }
        }

        private void AddSignatures(FlowDocument doc, string employeeFullName)
        {
            Paragraph signaturesPara = new Paragraph();
            signaturesPara.Margin = new Thickness(0, 40, 0, 0);

            Grid signatureGrid = new Grid();
            signatureGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            signatureGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel employerPanel = new StackPanel();
            employerPanel.Children.Add(new TextBlock { Text = "РАБОТОДАТЕЛЬ:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            employerPanel.Children.Add(new TextBlock { Text = _companyData.Name, Margin = new Thickness(0, 0, 0, 5) });
            employerPanel.Children.Add(new TextBlock { Text = $"ИНН/КПП: {_companyData.INN}/{_companyData.KPP}", Margin = new Thickness(0, 0, 0, 20) });
            employerPanel.Children.Add(new TextBlock { Text = "Генеральный директор", Margin = new Thickness(0, 0, 0, 5) });
            employerPanel.Children.Add(new TextBlock { Text = "_________________ / " + _companyData.Director });

            StackPanel employeePanel = new StackPanel();
            employeePanel.Children.Add(new TextBlock { Text = "РАБОТНИК:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            employeePanel.Children.Add(new TextBlock { Text = employeeFullName, Margin = new Thickness(0, 0, 0, 5) });
            employeePanel.Children.Add(new TextBlock { Text = "Паспорт: серия ______ № ______", Margin = new Thickness(0, 0, 0, 20) });
            employeePanel.Children.Add(new TextBlock { Text = "Подпись:", Margin = new Thickness(0, 0, 0, 5) });
            employeePanel.Children.Add(new TextBlock { Text = "_________________" });

            Grid.SetColumn(employerPanel, 0);
            Grid.SetColumn(employeePanel, 1);

            signatureGrid.Children.Add(employerPanel);
            signatureGrid.Children.Add(employeePanel);

            BlockUIContainer signatureContainer = new BlockUIContainer(signatureGrid);
            doc.Blocks.Add(signatureContainer);
        }

        private bool SaveContractDocument(FlowDocument document)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "RTF Files|*.rtf|Word Documents|*.docx|All Files|*.*",
                FileName = $"Трудовой_договор_{_employee.Surname}_{_employee.Name}_{ContractNumberTextBox.Text}.rtf",
                Title = "Сохранение трудового договора"
            };

            if (saveDialog.ShowDialog() == true)
            {
                string filePath = saveDialog.FileName;
                string extension = Path.GetExtension(filePath).ToLower();

                TextRange textRange = new TextRange(document.ContentStart, document.ContentEnd);
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    if (extension == ".rtf")
                    {
                        textRange.Save(fs, DataFormats.Rtf);
                    }
                    else if (extension == ".docx")
                    {
                        textRange.Save(fs, DataFormats.Rtf);
                        MessageBox.Show("Примечание: Файл сохранён в формате RTF и может быть открыт в Microsoft Word.",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                return true;
            }
            return false;
        }

        private string GetDay(string dateString)
        {
            if (DateTime.TryParseExact(dateString, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                return date.Day.ToString();
            return "___";
        }

        private string GetMonth(string dateString)
        {
            string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня",
                              "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            if (DateTime.TryParseExact(dateString, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                return months[date.Month - 1];
            return "__________";
        }

        private string GetYear(string dateString)
        {
            if (DateTime.TryParseExact(dateString, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                return date.Year.ToString().Substring(2);
            return "____";
        }

        private string NumberToWords(decimal number)
        {
            return number.ToString("# ##0").Replace(" ", "");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
        }
    }

    public class CompanyData
    {
        public string Name { get; set; }
        public string Director { get; set; }
        public string INN { get; set; }
        public string KPP { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
    }
}