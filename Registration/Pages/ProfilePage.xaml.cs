using Registration.Model;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Registration.Helpers;
using Registration.Services;

namespace Registration.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadUserData();
        }

        private void LoadUserData()
        {
            var user = AuthPage.CurrentUser;
            if (user == null) return;

            tblFullName.Text = user.Name;
            tblLogin.Text = user.Login;
            tblEmail.Text = user.Email;
            tblRole.Text = user.Roles?.RoleName ?? "Клиент";

            cbTwoFactorEnable.Checked -= CbTwoFactor_Changed;
            cbTwoFactorEnable.Unchecked -= CbTwoFactor_Changed;

            cbTwoFactorEnable.IsChecked = user.IsTwoFactorEnabled;

            cbTwoFactorEnable.Checked += CbTwoFactor_Changed;
            cbTwoFactorEnable.Unchecked += CbTwoFactor_Changed;
        }

        private void CbTwoFactor_Changed(object sender, RoutedEventArgs e)
        {
            using (var db = new BeermageEntities1())
            {
                var user = db.Users.Find(AuthPage.CurrentUser.UserID);
                if (user != null)
                {
                    user.IsTwoFactorEnabled = cbTwoFactorEnable.IsChecked ?? false;
                    db.SaveChanges();
                    AuthPage.CurrentUser.IsTwoFactorEnabled = user.IsTwoFactorEnabled;

                    string status = (bool)user.IsTwoFactorEnabled ? "включена" : "выключена";
                    MessageBox.Show($"Двухфакторная аутентификация {status}!", "Безопасность");
                }
            }
        }

        private void BtnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pbNewPass.Password) || pbNewPass.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов!");
                return;
            }

            if (pbNewPass.Password != pbConfirmPass.Password)
            {
                MessageBox.Show("Пароли не совпадают!");
                return;
            }

            using (var db = new BeermageEntities1())
            {
                var user = db.Users.Find(AuthPage.CurrentUser.UserID);
                user.PasswordHash = PasswordHasher.ComputeSha256Hash(pbNewPass.Password);
                db.SaveChanges();
                MessageBox.Show("Пароль успешно обновлен!");
                pbNewPass.Clear();
                pbConfirmPass.Clear();
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            AuthPage.CurrentUser = null;
            NavigationService.Navigate(new AuthPage());
        }
    }
}