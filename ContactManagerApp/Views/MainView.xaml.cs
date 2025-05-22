using ContactManagerApp.Services;
using ContactManagerApp.Views;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using NavigationService = ContactManagerApp.Services.NavigationService;

namespace ContactManagerApp.Views
{
    public partial class MainView : Window
    {
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        private bool _isSidebarCollapsed;
        private const double FullSidebarWidth = 280;
        private const double CollapsedSidebarWidth = 0;
        private const double MinWidthForFullSidebar = 800;
        private string _searchQuery;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                _isSidebarCollapsed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSidebarCollapsed)));
                UpdateSidebarState();
            }
        }

        public MainView(ContactService contactService)
        {
            InitializeComponent();
            _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));

            if (App.CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("Error: App.CurrentUser is null in MainView");
                Close();
                var authView = new AuthView();
                authView.Show();
                return;
            }
            
            _navigationService = new NavigationService(MainFrame);

            // Додаємо обробник для події Navigating
            _navigationService.GetFrame().Navigating += MainFrame_Navigating;

            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));

            ContactsButton.Focus();

            UpdateSidebarState();

            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void MainFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            // Перевіряємо, чи поточна сторінка – SettingsView
            if (MainFrame.Content is SettingsView settingsView)
            {
                // Перевіряємо, чи є незбережені зміни
                if (settingsView.HasUnsavedChanges)
                {
                    // Перевіряємо валідність даних
                    if (!settingsView.ValidateData())
                    {
                        // Якщо дані невалідні, скидаємо зміни і дозволяємо навігацію
                        settingsView.DiscardChanges();
                        return; // Навігація продовжиться, але зміни не збережуться
                    }

                    // Якщо дані валідні, показуємо діалог підтвердження
                    var dialog = new CustomConfirmationDialog
                    {
                        Title = LocalizationManager.GetString("UnsavedChangesTitle"),
                        Message = LocalizationManager.GetString("UnsavedChangesMessage"),
                        ConfirmButtonText = LocalizationManager.GetString("Save"),
                        CancelButtonText = LocalizationManager.GetString("Cancel")
                    };

                    var window = new Window
                    {
                        AllowsTransparency = true,
                        Content = dialog,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        WindowStyle = WindowStyle.None,
                        ResizeMode = ResizeMode.NoResize,
                        Background = null
                    };

                    bool? dialogResult = null;
                    dialog.DialogResult += (s, result) =>
                    {
                        dialogResult = result;
                        if (result)
                        {
                            // Зберігаємо зміни без показу діалогу успіху
                            settingsView.SaveProfileChanges(showSuccessDialog: false);
                        }
                        else
                        {
                            // Скасовуємо зміни
                            settingsView.DiscardChanges();
                        }
                    };

                    window.ShowDialog();

                    // Якщо користувач натиснув "Cancel", скасовуємо навігацію
                    if (dialogResult == false)
                    {
                        e.Cancel = true;
                    }
                }
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            DataContext = null;
            DataContext = this;

            UpdateBindings(Sidebar);

            if (MainFrame.Content is Page currentPage)
            {
                currentPage.DataContext = null;
                currentPage.DataContext = currentPage;
                UpdateBindings(currentPage);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text?.Trim().ToLower();

            if (MainFrame.Content is ContactListView contactListView)
            {
                contactListView.UpdateSearchFilter(_searchQuery);
            }
        }

        private void UpdateBindings(DependencyObject element)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is DependencyObject depObj)
                {
                    if (depObj is TextBlock textBlock)
                    {
                        var bindingExpression = textBlock.GetBindingExpression(TextBlock.TextProperty);
                        bindingExpression?.UpdateTarget();
                    }
                    else if (depObj is ContentControl contentControl && contentControl.Content is TextBlock contentTextBlock)
                    {
                        var bindingExpression = contentTextBlock.GetBindingExpression(TextBlock.TextProperty);
                        bindingExpression?.UpdateTarget();
                    }

                    UpdateBindings(depObj);
                }
            }
        }

        private void UpdateSidebarState()
        {
            var expandAnimation = (Storyboard)FindResource("ExpandSidebarAnimation");
            var collapseAnimation = (Storyboard)FindResource("CollapseSidebarAnimation");

            if (IsSidebarCollapsed)
            {
                collapseAnimation.Begin();
            }
            else
            {
                expandAnimation.Begin();
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth < MinWidthForFullSidebar)
            {
                IsSidebarCollapsed = true;
            }
            else
            {
                IsSidebarCollapsed = false;
            }
        }

        private void AddContactButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new AddContactView(_navigationService, _contactService));
        }

        private void ContactsButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));
            ContactsButton.Focus();
        }

        private void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            FavouriteButton.Focus();
            _navigationService.Navigate(new FavouriteContactListView(_navigationService, _contactService));
        }

        private void FindDublicate_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new MergeDuplicatesView(_navigationService, _contactService));
            MergeFixButton.Focus();
        }

        private void TrashButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new TrashContactListView(_navigationService, _contactService));
            TrashButton.Focus();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new SettingsView(_navigationService));
            SettingsButton.Focus();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomConfirmationDialog
            {
                Title = LocalizationManager.GetString("LogoutConfirmationTitle"),
                Message = LocalizationManager.GetString("LogoutConfirmationMessage"),
                ConfirmButtonText = LocalizationManager.GetString("Yes"),
                CancelButtonText = LocalizationManager.GetString("No")
            };

            var window = new Window
            {
                AllowsTransparency = true,
                Content = dialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = null
            };

            dialog.DialogResult += (s, result) =>
            {
                if (result)
                {
                    System.Diagnostics.Debug.WriteLine($"Logging out: UserId={(App.CurrentUser != null ? App.CurrentUser.UserId : "null")}");
                    App.SettingStorage.SetSetting("AuthToken", null);
                    App.SettingStorage.SetSetting("CurrentUserId", null);
                    App.SettingStorage.SetCurrentUserIdLastUsed(null);
                    App.SettingStorage.SetSessionExpiredShown(false);
                    App.SettingStorage.SetAppLastClosed(null);
                    App.CurrentUser = null;
                    AuthView authView = new AuthView();
                    authView.Show();
                    Close();
                }
            };

            window.ShowDialog();
        }

        private void LogoButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));
            ContactsButton.Focus();
            System.Diagnostics.Debug.WriteLine("Navigated to ContactListView via LogoButton");
        }

        ~MainView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            _navigationService.GetFrame().Navigating -= MainFrame_Navigating;
        }
    }
}