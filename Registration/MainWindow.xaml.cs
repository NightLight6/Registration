using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Registration.Pages;

namespace Registration
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new AuthPage());
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            btnBack.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;

            if (e.Content is AuthPage || e.Content is RegistrationPage)
            {
                spUserControls.Visibility = Visibility.Collapsed;
                btnBack.Visibility = Visibility.Collapsed;
            }
            else
            {
                spUserControls.Visibility = Visibility.Visible;

                if (AuthPage.CurrentUser != null)
                {
                    tblUserName.Text = AuthPage.CurrentUser.Name;
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }

        private void btnProfile_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите выйти?", "Выход", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                AuthPage.CurrentUser = null;
                MainFrame.Navigate(new AuthPage());

                while (MainFrame.CanGoBack)
                {
                    MainFrame.RemoveBackEntry();
                }
            }
        }
    }
}