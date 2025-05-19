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
    public partial class TrashContactListView : Page
    {
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<Contact> SelectedContacts { get; set; }
        public ObservableCollection<Contact> TrashContacts { get; set; }
        public ObservableCollection<ComboBoxItemModel> SelectionActions { get; set; }
        private readonly CollectionViewSource _trashContactsViewSource;
        private string _searchQuery;

        public ICommand RestoreContactCommand { get; }
        public ICommand RestoreSelectedContactsCommand { get; }
        public ICommand PermanentlyDeleteSelectedContactsCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        // Визначаємо DependencyProperty для FilteredTrashContactsCount
        public static readonly DependencyProperty FilteredTrashContactsCountProperty =
            DependencyProperty.Register(
                nameof(FilteredTrashContactsCount),
                typeof(int),
                typeof(TrashContactListView),
                new PropertyMetadata(0));

        public int FilteredTrashContactsCount
        {
            get => (int)GetValue(FilteredTrashContactsCountProperty);
            set => SetValue(FilteredTrashContactsCountProperty, value);
        }

        public TrashContactListView(NavigationService navigationService, ContactService contactService)
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
            TrashContacts = new ObservableCollection<Contact>();

            _trashContactsViewSource = new CollectionViewSource { Source = Contacts };
            _trashContactsViewSource.Filter += TrashContactsViewSource_Filter;
            TrashContactsListView.ItemsSource = _trashContactsViewSource.View;

            RestoreContactCommand = new RelayCommand<Contact>(RestoreContact);
            RestoreSelectedContactsCommand = new RelayCommand<object>(RestoreSelectedContacts);
            PermanentlyDeleteSelectedContactsCommand = new RelayCommand<object>(PermanentlyDeleteSelectedContacts);
            ClearSelectionCommand = new RelayCommand<object>(ClearSelection);

            SelectionActions = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayText = LocalizationManager.GetString("SelectAll"), Action = SelectAll },
                new ComboBoxItemModel { DisplayText = LocalizationManager.GetString("ClearSelection"), Action = ClearSelection }
            };

            DataContext = this;
            SelectionComboBox.ItemsSource = SelectionActions;

            _contactService.ContactsChanged += (s, e) =>
            {
                Contacts.Clear();
                foreach (var contact in _contactService.GetAllContacts())
                {
                    Contacts.Add(contact);
                    contact.Initialize();
                }
                _trashContactsViewSource.View.Refresh();
                UpdateFilteredTrashContactsCount();

                DataContext = null;
                DataContext = this;
            };

            LocalizationManager.LanguageChanged += OnLanguageChanged;
            SelectedContacts.CollectionChanged += SelectedContacts_CollectionChanged;

            foreach (var contact in Contacts)
            {
                contact.PropertyChanged += Contact_PropertyChanged;
            }

            ClearSelection(null);

            _navigationService.GetFrame().Navigated += (s, e) =>
            {
                if (e.Content != this)
                {
                    ClearSelection(null);
                }
            };

            ((INotifyCollectionChanged)_trashContactsViewSource.View).CollectionChanged += (s, e) =>
            {
                UpdateFilteredTrashContactsCount();
            };

            // Початкове оновлення лічильника
            UpdateFilteredTrashContactsCount();
        }

        private void TrashContactsViewSource_Filter(object sender, FilterEventArgs e)
        {
            var contact = e.Item as Contact;
            if (contact == null)
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = contact.IsDeleted;

            if (e.Accepted && !string.IsNullOrWhiteSpace(_searchQuery))
            {
                var query = _searchQuery.ToLower();
                bool matchesName = !string.IsNullOrEmpty(contact.Name) && contact.Name.ToLower().Contains(query);
                e.Accepted = matchesName;
            }
        }

        public void UpdateSearchFilter(string searchQuery)
        {
            _searchQuery = searchQuery;
            _trashContactsViewSource.View.Refresh();
            UpdateFilteredTrashContactsCount();
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
                _trashContactsViewSource.View.Refresh();
                UpdateFilteredTrashContactsCount();
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

        private void RestoreContact(Contact contact)
        {
            if (contact != null)
            {
                contact.IsDeleted = false;
                contact.DeletionDate = null;
                _contactService.UpdateContact(contact);
                _trashContactsViewSource.View.Refresh();
                UpdateFilteredTrashContactsCount();
            }
        }

        private void RestoreSelectedContacts(object parameter)
        {
            foreach (var contact in SelectedContacts.ToList())
            {
                contact.IsDeleted = false;
                contact.DeletionDate = null;
                _contactService.UpdateContact(contact);
            }
            SelectedContacts.Clear();
            _trashContactsViewSource.View.Refresh();
            UpdateFilteredTrashContactsCount();
        }

        private void PermanentlyDeleteSelectedContacts(object parameter)
        {
            var contactsToDelete = SelectedContacts.ToList();
            if (contactsToDelete.Any())
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("PermanentlyDeleteTitle"),
                    Message = LocalizationManager.GetString("PermanentlyDeleteMessage"),
                    ConfirmButtonText = LocalizationManager.GetString("DeletePermanently"),
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
                            _contactService.DeleteContact(contact);
                        }
                        SelectedContacts.Clear();
                        _trashContactsViewSource.View.Refresh();
                        UpdateFilteredTrashContactsCount();
                    }
                };

                window.ShowDialog();
            }
        }

        private void SelectAll(object parameter)
        {
            foreach (var contact in _trashContactsViewSource.View.Cast<Contact>())
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
                if (originalSource is Button button && button.Style == (Style)FindResource("RestoreButtonStyle"))
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

        private void UpdateFilteredTrashContactsCount()
        {
            FilteredTrashContactsCount = _trashContactsViewSource.View.Cast<object>().Count();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        ~TrashContactListView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            _contactService.ContactsChanged -= (s, e) => { };
        }
    }
}