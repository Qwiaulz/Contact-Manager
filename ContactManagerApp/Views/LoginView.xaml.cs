using System.Windows;
using ContactManagerApp.Services;
using ContactManager.Views;

namespace ContactManagerApp.Views

{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Перевірка, чи не порожні поля
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Логін та пароль не можуть бути порожніми.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка формату логіна (наприклад, мінімум 3 символи)
            if (username.Length < 3)
            {
                MessageBox.Show("Логін повинен містити хоча б 3 символи.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var user = AuthService.Login(username, password);
            if (user != null)
            {
                MessageBox.Show("Вхід успішний!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Отримуємо userId (можна використовувати username або якийсь інший унікальний ідентифікатор)
                string userId = user.UserId;  // Припустимо, що у об'єкта user є властивість UserId

                // Створюємо MainWindow і передаємо userId
                MainView mainWindow = new MainView();
                mainWindow.Show();
                this.Close();  // Закриваємо вікно логіна
            }
            else
            {
                // Підрозділяємо повідомлення в залежності від того, чи не знайдений користувач або пароль неправильний
                if (!AuthService.UsersExist(username))
                {
                    MessageBox.Show("Користувача з таким логіном не знайдено.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Невірний пароль.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationView registrationView = new RegistrationView(); // відкриваємо вікно реєстрації
            registrationView.Show();
            this.Close();  // закриваємо вікно логіна
        }
    }
}
