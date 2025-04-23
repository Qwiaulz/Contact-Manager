using ContactManagerApp.Models;
using ContactManagerApp.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContactManagerApp.Views
{
    public partial class ContactDetailsView : Page
    {
        private readonly Contact _contact;
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        private readonly string _photosDirectory;
        public ICommand ChoosePhotoCommand { get; }

        public Contact SelectedContact => _contact;

        public ContactDetailsView(Contact contact, NavigationService navigationService, ContactService contactService)
        {
            InitializeComponent();
            _contact = contact;
            _navigationService = navigationService;
            _contactService = contactService;

            // Ініціалізація директорії для фото
            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            _photosDirectory = Path.Combine(projectRoot, "Data", "Photos");
            if (!Directory.Exists(_photosDirectory))
            {
                Directory.CreateDirectory(_photosDirectory);
            }

            ChoosePhotoCommand = new RelayCommand<object>(ChoosePhoto);

            DataContext = this;

            LocalizationManager.LanguageChanged += (s, e) =>
            {
                // Оновлюємо UI при зміні мови
                this.DataContext = null;
                this.DataContext = this;
            };
        }

        private void ChoosePhoto(object parameter)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Видаляємо старе фото, якщо воно не дефолтне
                    if (!string.IsNullOrEmpty(_contact.Photo) && !_contact.IsPhotoDefault)
                    {
                        string oldPhotoPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", _contact.Photo);
                        if (File.Exists(oldPhotoPath))
                        {
                            File.Delete(oldPhotoPath);
                        }
                    }

                    // Копіюємо нове фото
                    string extension = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = $"contact_{_contact.Id}_{DateTime.Now.Ticks}{extension}";
                    string destinationPath = Path.Combine(_photosDirectory, newFileName);

                    File.Copy(openFileDialog.FileName, destinationPath, true);
                    _contact.Photo = Path.Combine("Photos", newFileName).Replace("\\", "/");
                    _contact.UpdatedDate = DateTime.Now;

                    // Оновлюємо контакт у сервісі
                    _contactService.UpdateContact(_contact);
                    _contactService.NotifyContactsChanged();

                    // Оновлюємо UI
                    _contact.OnPropertyChanged(nameof(_contact.Photo));
                    _contact.OnPropertyChanged(nameof(_contact.Initials));
                    _contact.OnPropertyChanged(nameof(_contact.IsPhotoDefault));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uploading photo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddPhotoCircle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChoosePhotoCommand.Execute(null);
            e.Handled = true;
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.GoBack();
        }

        private void EditContact_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new EditContactView(_contact, _navigationService, _contactService));
        }

        private void DeleteContact_Click(object sender, RoutedEventArgs e)
        {
            if (_contact == null) return;

            if (!_contact.IsDeleted)
            {
                // М'яке видалення: переносимо до кошика
                var result = MessageBox.Show(
                    LocalizationManager.GetString("ConfirmDeleteMessage"),
                    LocalizationManager.GetString("ConfirmDeleteTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _contactService.MarkAsDeleted(_contact);
                    _contactService.NotifyContactsChanged();
                    _navigationService.GoBack();
                }
            }
            else
            {
                // Остаточне видалення
                var result = MessageBox.Show(
                    LocalizationManager.GetString("ConfirmPermanentDeleteMessage") ?? "This contact will be permanently deleted and cannot be restored. Are you sure?",
                    LocalizationManager.GetString("ConfirmDeleteTitle") ?? "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _contactService.DeleteContact(_contact);
                    _contactService.NotifyContactsChanged();
                    _navigationService.GoBack();
                }
            }
        }

        private void ToggleFavourite_Click(object sender, MouseButtonEventArgs e)
        {
            _contact.IsFavourite = !_contact.IsFavourite;
            _contactService.UpdateContact(_contact);
            _contactService.NotifyContactsChanged();
        }
    }
}