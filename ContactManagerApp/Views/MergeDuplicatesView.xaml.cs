using ContactManagerApp.Models;
using ContactManagerApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ContactManagerApp.Views
{
    public partial class MergeDuplicatesView : Page
    {
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        public ObservableCollection<DuplicateGroup> DuplicateGroups { get; set; }
        private static readonly HashSet<string> _canceledGroupIds = new HashSet<string>(); // Статичне для збереження між переходами
        private readonly Dictionary<string, int> _groupContactCounts; // Для зберігання кількості контактів у групі
        private readonly Dictionary<string, string> _groupDataHashes; // Для зберігання хешів даних контактів у групі

        public string HeaderText => $"{LocalizationManager.GetString("MergeDuplicatesHeader")} ({DuplicateGroups.Count(g => !g.IsMerged)})";
        public string MergeAllButtonText => LocalizationManager.GetString("MergeAllButton");
        public string NoDuplicatesMessage => LocalizationManager.GetString("NoDuplicatesMessage");
        public string ViewButtonText => LocalizationManager.GetString("ViewButton");
        public string MergeButtonText => LocalizationManager.GetString("MergeButton");
        public string CancelButtonText => LocalizationManager.GetString("Cancel");
        public bool HasNoDuplicates => !DuplicateGroups.Any(g => !g.IsMerged);

        public ICommand ViewContactCommand { get; }
        public ICommand MergeCommand { get; }
        public ICommand MergeAllCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ViewMergedContactCommand { get; }

        public MergeDuplicatesView(NavigationService navigationService, ContactService contactService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _contactService = contactService;

            DuplicateGroups = new ObservableCollection<DuplicateGroup>();
            _groupContactCounts = new Dictionary<string, int>(); // Ініціалізуємо словник для кількості контактів
            _groupDataHashes = new Dictionary<string, string>(); // Ініціалізуємо словник для хешів даних
            LoadDuplicates();

            ViewContactCommand = new RelayCommand<Contact>(ViewContact);
            MergeCommand = new RelayCommand<DuplicateGroup>(MergeContacts);
            MergeAllCommand = new RelayCommand<object>(MergeAllContacts);
            CancelCommand = new RelayCommand<DuplicateGroup>(Cancel);
            ViewMergedContactCommand = new RelayCommand<DuplicateGroup>(ViewMergedContact);

            DataContext = this;

            _contactService.ContactsChanged += (s, e) =>
            {
                LoadDuplicates();
                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(HasNoDuplicates));
            };

            LocalizationManager.LanguageChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(MergeAllButtonText));
                OnPropertyChanged(nameof(NoDuplicatesMessage));
                OnPropertyChanged(nameof(ViewButtonText));
                OnPropertyChanged(nameof(MergeButtonText));
                OnPropertyChanged(nameof(CancelButtonText));
            };
        }

        public class DuplicateGroup : INotifyPropertyChanged
        {
            private bool _isMerged;
            private List<Contact> _contacts;

            public event PropertyChangedEventHandler PropertyChanged;

            public string GroupId => GenerateGroupId(); // Унікальний ідентифікатор групи

            public List<Contact> Contacts
            {
                get => _contacts;
                set
                {
                    _contacts = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ContactCount));
                    OnPropertyChanged(nameof(DataHash)); // Сповіщаємо про зміну хешу даних
                }
            }

            public int ContactCount => Contacts?.Count ?? 0; // Кількість контактів у групі

            public bool IsMerged
            {
                get => _isMerged;
                set
                {
                    _isMerged = value;
                    OnPropertyChanged();
                }
            }

            public Contact MergedContact { get; set; }

            // Хеш даних контактів (імена, телефони, email), щоб відстежувати зміни
            public string DataHash
            {
                get
                {
                    if (Contacts == null || !Contacts.Any()) return string.Empty;

                    // Сортуємо контакти за Id для стабільності хешу
                    var sortedContacts = Contacts.OrderBy(c => c.Id).ToList();
                    var data = sortedContacts
                        .Select(c => $"{c.Name}|{c.FirstPhone ?? ""}|{c.FirstEmail ?? ""}")
                        .OrderBy(s => s)
                        .ToList();
                    return string.Join("_", data);
                }
            }

            private string GenerateGroupId()
            {
                // Сортуємо ID контактів і об'єднуємо їх у рядок для створення унікального ідентифікатора
                var sortedContactIds = Contacts?.Select(c => c.Id).OrderBy(id => id).ToList();
                return sortedContactIds != null ? string.Join("_", sortedContactIds) : string.Empty;
            }

            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void LoadDuplicates()
        {
            var duplicates = _contactService.FindDuplicates();
            var newGroups = new List<DuplicateGroup>();

            foreach (var group in duplicates)
            {
                foreach (var contact in group)
                {
                    contact.Initialize();
                    CheckPhotoExistence(contact); // Перевірка фото для кожного контакту
                }

                var duplicateGroup = new DuplicateGroup { Contacts = group, IsMerged = false };
                var groupId = duplicateGroup.GroupId;

                // Перевіряємо, чи група була скасована раніше
                if (!_canceledGroupIds.Contains(groupId))
                {
                    newGroups.Add(duplicateGroup);
                }
                else
                {
                    // Якщо група була скасована, перевіряємо, чи змінилася кількість контактів
                    bool hasCountChanged = false;
                    if (_groupContactCounts.TryGetValue(groupId, out int previousCount))
                    {
                        if (duplicateGroup.ContactCount != previousCount)
                        {
                            hasCountChanged = true;
                        }
                    }

                    // Перевіряємо, чи змінилися дані контактів
                    bool hasDataChanged = false;
                    if (_groupDataHashes.TryGetValue(groupId, out string previousHash))
                    {
                        if (duplicateGroup.DataHash != previousHash)
                        {
                            hasDataChanged = true;
                        }
                    }

                    // Якщо кількість контактів або їхні дані змінилися, скидаємо статус скасування
                    if (hasCountChanged || hasDataChanged)
                    {
                        _canceledGroupIds.Remove(groupId);
                        newGroups.Add(duplicateGroup);
                    }
                }

                // Оновлюємо або додаємо кількість контактів і хеш даних для групи
                _groupContactCounts[groupId] = duplicateGroup.ContactCount;
                _groupDataHashes[groupId] = duplicateGroup.DataHash;
            }

            // Оновлюємо колекцію груп
            DuplicateGroups.Clear();
            foreach (var group in newGroups)
            {
                DuplicateGroups.Add(group);
            }

            OnPropertyChanged(nameof(HasNoDuplicates));
            OnPropertyChanged(nameof(HeaderText));
        }

        private void ViewContact(Contact contact)
        {
            if (contact != null)
            {
                _navigationService.Navigate(new ContactDetailsView(contact, _navigationService, _contactService));
            }
        }

        private void ViewMergedContact(DuplicateGroup group)
        {
            if (group != null && group.MergedContact != null)
            {
                _navigationService.Navigate(new ContactDetailsView(group.MergedContact, _navigationService, _contactService));
            }
        }

        private void MergeContacts(DuplicateGroup group)
        {
            if (group == null || group.Contacts.Count < 2) return;

            _contactService.MergeContacts(group.Contacts);
            group.MergedContact = group.Contacts[0];
            group.Contacts = new List<Contact> { group.MergedContact };
            group.IsMerged = true;

            // Видаляємо групу зі списку скасованих, якщо вона там була
            _canceledGroupIds.Remove(group.GroupId);

            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(HasNoDuplicates));
        }

        private void MergeAllContacts(object parameter)
        {
            foreach (var group in DuplicateGroups.ToList())
            {
                if (!group.IsMerged)
                {
                    MergeContacts(group);
                }
            }
        }

        private void Cancel(DuplicateGroup group)
        {
            if (group == null) return;

            // Додаємо ID групи до списку скасованих
            _canceledGroupIds.Add(group.GroupId);
            DuplicateGroups.Remove(group);

            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(HasNoDuplicates));
        }

        private void ListViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            bool isActionElement = false;

            while (originalSource != null)
            {
                if (originalSource is Button)
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

        private void CheckPhotoExistence(Contact contact)
        {
            if (!string.IsNullOrEmpty(contact.Photo) && !contact.IsPhotoDefault)
            {
                string fullPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", contact.Photo);
                if (!File.Exists(fullPath))
                {
                    contact.Photo = null;
                    contact.IsPhotoDefault = true;
                    contact.OnPropertyChanged(nameof(contact.Photo));
                    contact.OnPropertyChanged(nameof(contact.Initials));
                    contact.OnPropertyChanged(nameof(contact.IsPhotoDefault));
                    _contactService.UpdateContact(contact);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}