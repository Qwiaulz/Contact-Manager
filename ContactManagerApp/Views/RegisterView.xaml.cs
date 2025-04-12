using System.Windows;
using ContactManagerApp.Services;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace ContactManagerApp.Views
{
    public partial class RegistrationView : Window
    {
        public RegistrationView()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Перевірка на порожні поля
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Будь ласка, заповніть усі поля.", "Помилка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Перевірка на мінімум 3 символи для логіну
            if (username.Length < 3)
            {
                MessageBox.Show("Логін повинен містити мінімум 3 символи.", "Помилка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Перевірка на мінімум 8 символів для пароля
            if (password.Length < 8)
            {
                MessageBox.Show("Пароль повинен містити мінімум 8 символів.", "Помилка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Перевірка на наявність пробілів у логіні та паролі
            if (username.Contains(" ") || password.Contains(" "))
            {
                MessageBox.Show("Логін і пароль не можуть містити пробілів.", "Помилка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Перевірка на наявність користувача
            if (AuthService.UsersExist(username))
            {
                MessageBox.Show("Користувач з таким іменем вже існує.", "Помилка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Реєстрація користувача
            if (AuthService.Register(username, password))
            {
                MessageBox.Show("Реєстрація успішна!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                LoginView loginView = new LoginView();
                loginView.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Помилка реєстрації. Спробуйте ще раз.", "Помилка", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            LoginView loginView = new LoginView();
            loginView.Show();
            this.Close();
        }
    }
}