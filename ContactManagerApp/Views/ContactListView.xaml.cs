using ContactManagerApp.Models;
using ContactManagerApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ContactManagerApp.Views
{
    public partial class ContactListView : Page
    {
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<Contact> SelectedContacts { get; set; }
        public ObservableCollection<ComboBoxItemModel> SelectionActions { get; set; }
        private readonly CollectionViewSource _contactsViewSource;
        private string _searchQuery;

        public ICommand EditContactCommand { get; }
        public ICommand DeleteContactCommand { get; }
        public ICommand ToggleFavouriteCommand { get; }
        public ICommand DeleteSelectedContactsCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        public static readonly DependencyProperty FilteredContactsCountProperty =
            DependencyProperty.Register(
                nameof(FilteredContactsCount),
                typeof(int),
                typeof(ContactListView),
                new PropertyMetadata(0));

        public int FilteredContactsCount
        {
            get => (int)GetValue(FilteredContactsCountProperty);
            set => SetValue(FilteredContactsCountProperty, value);
        }

        public ContactListView(NavigationService navigationService, ContactService contactService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _contactService = contactService;

            Contacts = new ObservableCollection<Contact>(_contactService.GetAllContacts());
            foreach (var contact in Contacts)
            {
                contact.Initialize();
            }
            SelectedContacts = new ObservableCollection<Contact>();

            _contactsViewSource = new CollectionViewSource { Source = Contacts };
            _contactsViewSource.Filter += ContactListViewSource_Filter;
            ContactsListView.ItemsSource = _contactsViewSource.View;

            EditContactCommand = new RelayCommand<Contact>(EditContact);
            DeleteContactCommand = new RelayCommand<Contact>(DeleteContact);
            ToggleFavouriteCommand = new RelayCommand<Contact>(ToggleFavourite);
            DeleteSelectedContactsCommand = new RelayCommand<object>(DeleteSelectedContacts);
            ClearSelectionCommand = new RelayCommand<object>(ClearSelection);

            SelectionActions = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayText = LocalizationManager.GetString("SelectAll"), Action = SelectAll },
                new ComboBoxItemModel { DisplayText = LocalizationManager.GetString("ClearSelection"), Action = ClearSelection }
            };

            DataContext = this;
            SelectionComboBox.ItemsSource = SelectionActions;

            _contactService.ContactsChanged += OnContactsChanged;

            LocalizationManager.LanguageChanged += OnLanguageChanged;
            SelectedContacts.CollectionChanged += SelectedContacts_CollectionChanged;

            foreach (var contact in Contacts)
            {
                contact.PropertyChanged += Contact_PropertyChanged;
                SubscribeToPhoneAndEmailChanges(contact);
            }

            ClearSelection(null);

            _navigationService.GetFrame().Navigated += (s, e) =>
            {
                if (e.Content == this)
                {
                    _contactsViewSource.View.Refresh();
                    UpdateFilteredContactsCount();
                }
                else
                {
                    ClearSelection(null);
                }
            };

            ((INotifyCollectionChanged)_contactsViewSource.View).CollectionChanged += (s, e) =>
            {
                UpdateFilteredContactsCount();
            };

            UpdateFilteredContactsCount();
        }

        private void OnContactsChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Contacts.Clear();
                foreach (var contact in _contactService.GetAllContacts())
                {
                    contact.Initialize();
                    contact.PropertyChanged -= Contact_PropertyChanged;
                    contact.PropertyChanged += Contact_PropertyChanged;
                    SubscribeToPhoneAndEmailChanges(contact);
                    Contacts.Add(contact);
                }
                _contactsViewSource.View.Refresh();
                UpdateFilteredContactsCount();
            });
        }

        private void ContactListViewSource_Filter(object sender, FilterEventArgs e)
        {
            var contact = e.Item as Contact;
            if (contact == null)
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = !contact.IsDeleted;

            if (e.Accepted && !string.IsNullOrWhiteSpace(_searchQuery))
            {
                var query = _searchQuery.ToLower();
                bool matchesName = !string.IsNullOrEmpty(contact.Name) && contact.Name.ToLower().Contains(query);
                bool matchesPhone = contact.Phones.Any(p => !string.IsNullOrEmpty(p.Number) && p.Number.ToLower().Contains(query));
                bool matchesEmail = contact.Emails.Any(em => !string.IsNullOrEmpty(em.Address) && em.Address.ToLower().Contains(query));
                e.Accepted = matchesName || matchesPhone || matchesEmail;
            }
        }

        public void UpdateSearchFilter(string searchQuery)
        {
            _searchQuery = searchQuery;
            _contactsViewSource.View.Refresh();
            UpdateFilteredContactsCount();
        }

        private void SubscribeToPhoneAndEmailChanges(Contact contact)
        {
            foreach (var phone in contact.Phones)
            {
                phone.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PhoneNumber.Number))
                    {
                        contact.OnPropertyChanged(nameof(Contact.FirstPhone));
                    }
                };
            }
            foreach (var email in contact.Emails)
            {
                email.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Email.Address))
                    {
                        contact.OnPropertyChanged(nameof(Contact.FirstEmail));
                    }
                };
            }
        }

        private void Contact_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var contact = (Contact)sender;
            if (e.PropertyName == nameof(Contact.IsSelected))
            {
                if (contact.IsSelected && !SelectedContacts.Contains(contact))
                {
                    SelectedContacts.Add(contact);
                }
                else if (!contact.IsSelected && SelectedContacts.Contains(contact))
                {
                    SelectedContacts.Remove(contact);
                }
            }
            else if (e.PropertyName == nameof(Contact.IsDeleted))
            {
                _contactsViewSource.View.Refresh();
                UpdateFilteredContactsCount();
            }
        }

        private void SelectedContacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Contact contact in e.NewItems)
                {
                    contact.PropertyChanged -= Contact_PropertyChanged;
                    contact.PropertyChanged += Contact_PropertyChanged;
                }
            }
        }

        private void EditContact(Contact contact)
        {
            if (contact != null)
            {
                _navigationService.Navigate(new EditContactView(contact, _navigationService, _contactService));
            }
        }

        private void DeleteContact(Contact contact)
        {
            if (contact != null && !contact.IsDeleted)
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("DeleteContactTitle"),
                    Message = LocalizationManager.GetString("DeleteContactMessage"),
                    ConfirmButtonText = LocalizationManager.GetString("MoveToBasket"),
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

                dialog.DialogResult += (s, result) =>
                {
                    if (result)
                    {
                        _contactService.MarkAsDeleted(contact);
                        _contactsViewSource.View.Refresh();
                        UpdateFilteredContactsCount();
                    }
                };

                window.ShowDialog();
            }
        }

        private void ToggleFavourite(Contact contact)
        {
            if (contact != null)
            {
                contact.IsFavourite = !contact.IsFavourite;
                _contactService.UpdateContact(contact);
            }
        }

        private void DeleteSelectedContacts(object parameter)
        {
            var contactsToDelete = SelectedContacts.ToList();
            if (contactsToDelete.Any())
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("DeleteContactTitle"),
                    Message = LocalizationManager.GetString("DeleteContactMessage"),
                    ConfirmButtonText = LocalizationManager.GetString("MoveToBasket"),
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

                dialog.DialogResult += (s, result) =>
                {
                    if (result)
                    {
                        foreach (var contact in contactsToDelete)
                        {
                            if (!contact.IsDeleted)
                            {
                                _contactService.MarkAsDeleted(contact);
                            }
                        }
                        SelectedContacts.Clear();
                        _contactsViewSource.View.Refresh();
                        UpdateFilteredContactsCount();
                    }
                };

                window.ShowDialog();
            }
        }

        private void SelectAll(object parameter)
        {
            foreach (var contact in _contactsViewSource.View.Cast<Contact>())
            {
                contact.IsSelected = true;
            }
        }

        private void ClearSelection(object parameter)
        {
            foreach (var contact in SelectedContacts.ToList())
            {
                contact.IsSelected = false;
            }
            SelectedContacts.Clear();
        }

        private void SelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || comboBox.SelectedItem == null) return;

            var selectedAction = comboBox.SelectedItem as ComboBoxItemModel;
            if (selectedAction?.Action != null)
            {
                selectedAction.Action(null);
            }

            comboBox.SelectedIndex = -1;
        }

        private void ListViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            bool isActionElement = false;

            while (originalSource != null)
            {
                if (originalSource is CheckBox)
                {
                    isActionElement = true;
                    break;
                }
                if (originalSource is TextBlock textBlock && textBlock.Style == (Style)FindResource("ActionIconStyle"))
                {
                    isActionElement = true;
                    break;
                }
                if (originalSource is Button button && button.Style == (Style)FindResource("DeleteButtonStyle"))
                {
                    isActionElement = true;
                    break;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (isActionElement)
            {
                e.Handled = true;
                return;
            }

            var listViewItem = sender as ListViewItem;
            var contact = listViewItem?.DataContext as Contact;
            if (contact != null)
            {
                _navigationService.Navigate(new ContactDetailsView(contact, _navigationService, _contactService));
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            DataContext = null;

            SelectionActions.Clear();
            SelectionActions.Add(new ComboBoxItemModel { DisplayText = LocalizationManager.GetString("SelectAll"), Action = SelectAll });
            SelectionActions.Add(new ComboBoxItemModel { DisplayText = LocalizationManager.GetString("ClearSelection"), Action = ClearSelection });

            DataContext = this;
            UpdateBindings(this);
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

        private void UpdateFilteredContactsCount()
        {
            FilteredContactsCount = _contactsViewSource.View.Cast<object>().Count();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        ~ContactListView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            _contactService.ContactsChanged -= OnContactsChanged;
        }
    }

    public class ComboBoxItemModel
    {
        public string DisplayText { get; set; }
        public Action<object> Action { get; set; }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            bool canExecute = _canExecute == null || _canExecute((T)parameter);
            return canExecute;
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}