using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ContactManagerApp.Views
{
    public partial class AuthView : Window
    {
        public AuthView()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("AuthView initialized");

            // Ініціалізація початкових позицій
            var loginTransform = (TranslateTransform)LoginTile.RenderTransform;
            var registrationTransform = (TranslateTransform)RegistrationTile.RenderTransform;
            loginTransform.X = 0;
            registrationTransform.X = ActualWidth; // Використовуємо ширину вікна

            // Підписуємося на події переключення з LoginView і RegistrationView
            LoginTile.NavigateToRegistration += () => SwitchToRegistration();
            RegistrationTile.NavigateToLogin += () => SwitchToLogin();

            // Відстежуємо зміну розміру вікна
            SizeChanged += (s, e) =>
            {
                registrationTransform.X = ActualWidth; // Оновлюємо позицію при зміні розміру
                System.Diagnostics.Debug.WriteLine($"Window size changed: ActualWidth = {ActualWidth}");
            };
        }

        private void SwitchToRegistration()
        {
            var loginTransform = (TranslateTransform)LoginTile.RenderTransform;
            var registrationTransform = (TranslateTransform)RegistrationTile.RenderTransform;

            // Переконуємося, що обидва елементи видимі перед анімацією
            LoginTile.Visibility = Visibility.Visible;
            RegistrationTile.Visibility = Visibility.Visible;

            // Скидаємо позицію RegistrationTile перед анімацією, щоб вона точно починалася справа
            registrationTransform.X = ActualWidth;

            // Анімація для LoginTile (вліво)
            var loginAnimation = new DoubleAnimation
            {
                From = 0,
                To = -ActualWidth, // Використовуємо ширину вікна
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            loginAnimation.Completed += (s, e) =>
            {
                LoginTile.Visibility = Visibility.Collapsed;
                // Скидаємо позицію LoginTile назад до X = 0 для наступного входу
                loginTransform.X = 0;
                System.Diagnostics.Debug.WriteLine("LoginTile hidden and reset to X = 0 after animation");
            };
            loginTransform.BeginAnimation(TranslateTransform.XProperty, loginAnimation);

            // Анімація для RegistrationTile (справа наліво)
            var registrationAnimation = new DoubleAnimation
            {
                From = ActualWidth,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            registrationTransform.BeginAnimation(TranslateTransform.XProperty, registrationAnimation);

            System.Diagnostics.Debug.WriteLine("Switching to RegistrationView with animation");
        }

        private void SwitchToLogin()
        {
            var loginTransform = (TranslateTransform)LoginTile.RenderTransform;
            var registrationTransform = (TranslateTransform)RegistrationTile.RenderTransform;

            // Переконуємося, що обидва елементи видимі перед анімацією
            LoginTile.Visibility = Visibility.Visible;
            RegistrationTile.Visibility = Visibility.Visible;

            // Скидаємо позицію LoginTile перед анімацією, щоб вона починалася зліва
            loginTransform.X = -ActualWidth;

            // Анімація для RegistrationTile (зліва направо)
            var registrationAnimation = new DoubleAnimation
            {
                From = 0,
                To = ActualWidth,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            registrationAnimation.Completed += (s, e) =>
            {
                RegistrationTile.Visibility = Visibility.Collapsed;
                // Скидаємо позицію RegistrationTile назад до X = ActualWidth після анімації
                registrationTransform.X = ActualWidth;
                System.Diagnostics.Debug.WriteLine($"RegistrationTile hidden and reset to X = {ActualWidth} after animation");
            };
            registrationTransform.BeginAnimation(TranslateTransform.XProperty, registrationAnimation);

            // Анімація для LoginTile (з’являється зліва)
            var loginAnimation = new DoubleAnimation
            {
                From = -ActualWidth,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            loginTransform.BeginAnimation(TranslateTransform.XProperty, loginAnimation);

            System.Diagnostics.Debug.WriteLine("Switching to LoginView with animation");
        }
    }
}