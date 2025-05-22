using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ContactManagerApp.Views
{
    public partial class AuthView : Window
    {
        private TranslateTransform loginTransform;
        private TranslateTransform registrationTransform;

        public AuthView()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("AuthView initialized");

            // Ініціалізація трансформацій після завантаження вікна
            Loaded += (s, e) =>
            {
                loginTransform = LoginTile.RenderTransform as TranslateTransform ?? new TranslateTransform { X = 0 };
                registrationTransform = RegistrationTile.RenderTransform as TranslateTransform ?? new TranslateTransform { X = ActualWidth };

                LoginTile.RenderTransform = loginTransform;
                RegistrationTile.RenderTransform = registrationTransform;

                // Встановлюємо початкові позиції
                loginTransform.X = 0;
                registrationTransform.X = ActualWidth;

                System.Diagnostics.Debug.WriteLine($"Initial positions set: LoginTransform.X = {loginTransform.X}, RegistrationTransform.X = {registrationTransform.X}");
            };

            // Підписуємося на події переключення
            LoginTile.NavigateToRegistration += () => SwitchToRegistration();
            RegistrationTile.NavigateToLogin += () => SwitchToLogin();

            // Відстежуємо зміну розміру вікна
            SizeChanged += (s, e) =>
            {
                if (registrationTransform != null)
                {
                    registrationTransform.X = ActualWidth; // Оновлюємо позицію при зміні розміру
                    System.Diagnostics.Debug.WriteLine($"Window size changed: ActualWidth = {ActualWidth}, RegistrationTransform.X updated to {registrationTransform.X}");
                }
            };
        }

        private void SwitchToRegistration()
        {
            if (loginTransform == null || registrationTransform == null)
            {
                System.Diagnostics.Debug.WriteLine("Transforms are not initialized yet. Aborting animation.");
                return;
            }

            // Переконуємося, що обидва елементи видимі перед анімацією
            LoginTile.Visibility = Visibility.Visible;
            RegistrationTile.Visibility = Visibility.Visible;
            LoginTile.Opacity = 1;
            RegistrationTile.Opacity = 1;

            // Скидаємо позицію RegistrationTile перед анімацією
            registrationTransform.X = ActualWidth;

            // Анімація для LoginTile (вліво)
            var loginAnimation = new DoubleAnimation
            {
                From = 0,
                To = -ActualWidth,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            loginAnimation.Completed += (s, e) =>
            {
                LoginTile.Visibility = Visibility.Collapsed;
                LoginTile.Opacity = 0;
                loginTransform.X = 0; // Скидаємо позицію
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
            registrationAnimation.Completed += (s, e) =>
            {
                RegistrationTile.Opacity = 1; // Переконуємося, що видно після анімації
                System.Diagnostics.Debug.WriteLine("RegistrationTile animation completed");
            };
            registrationTransform.BeginAnimation(TranslateTransform.XProperty, registrationAnimation);

            System.Diagnostics.Debug.WriteLine("Switching to RegistrationView with animation");
        }

        private void SwitchToLogin()
        {
            if (loginTransform == null || registrationTransform == null)
            {
                System.Diagnostics.Debug.WriteLine("Transforms are not initialized yet. Aborting animation.");
                return;
            }

            // Переконуємося, що обидва елементи видимі перед анімацією
            LoginTile.Visibility = Visibility.Visible;
            RegistrationTile.Visibility = Visibility.Visible;
            LoginTile.Opacity = 1;
            RegistrationTile.Opacity = 1;

            // Скидаємо позицію LoginTile перед анімацією
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
                RegistrationTile.Opacity = 0;
                registrationTransform.X = ActualWidth; // Скидаємо позицію
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
            loginAnimation.Completed += (s, e) =>
            {
                LoginTile.Opacity = 1; // Переконуємося, що видно після анімації
                System.Diagnostics.Debug.WriteLine("LoginTile animation completed");
            };
            loginTransform.BeginAnimation(TranslateTransform.XProperty, loginAnimation);

            System.Diagnostics.Debug.WriteLine("Switching to LoginView with animation");
        }
    }
}