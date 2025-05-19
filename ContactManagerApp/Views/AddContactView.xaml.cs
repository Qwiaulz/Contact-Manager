using ContactManagerApp.Models;
using ContactManagerApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContactManagerApp.Views
{
    public partial class AddContactView : Page
    {
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        public Contact NewContact { get; set; }
        public ObservableCollection<PhoneNumber> Phones { get; set; }
        public ObservableCollection<Email> Emails { get; set; }
        public List<string> PhoneTypes { get; set; }
        public List<string> EmailTypes { get; set; }
        public List<string> Relationships { get; set; }
        public ICommand ChoosePhotoCommand { get; }
        public ICommand RemovePhotoCommand { get; }
        private readonly string _photosDirectory;
        public ICommand CancelContextMenuCommand { get; }

        public AddContactView(NavigationService navigationService, ContactService contactService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _contactService = contactService;

            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            _photosDirectory = Path.Combine(projectRoot, "Data", "Photos");
            if (!Directory.Exists(_photosDirectory))
            {
                Directory.CreateDirectory(_photosDirectory);
            }

            NewContact = new Contact
            {
                Phones = new ObservableCollection<PhoneNumber>(),
                Emails = new ObservableCollection<Email>(),
                IsFavourite = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            NewContact.Initialize();

            InitializeComboBoxLists();
            Phones = NewContact.Phones;
            Emails = NewContact.Emails;

            ChoosePhotoCommand = new RelayCommand<object>(ChoosePhoto);
            RemovePhotoCommand = new RelayCommand<object>(RemovePhoto);
            CancelContextMenuCommand = new RelayCommand<object>(CancelContextMenu);

            DataContext = this;

            if (PhotoActionIcon != null && PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.DataContext = this;
            }

            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void InitializeComboBoxLists()
        {
            PhoneTypes = new List<string>
            {
                LocalizationManager.GetString("PhoneTypeMobile"),
                LocalizationManager.GetString("PhoneTypeWork"),
                LocalizationManager.GetString("PhoneTypeHome"),
                LocalizationManager.GetString("PhoneTypeOther")
            };
            EmailTypes = new List<string>
            {
                LocalizationManager.GetString("EmailTypePersonal"),
                LocalizationManager.GetString("EmailTypeWork"),
                LocalizationManager.GetString("EmailTypeOther")
            };
            Relationships = new List<string>
            {
                LocalizationManager.GetString("RelationshipFriend"),
                LocalizationManager.GetString("RelationshipColleague"),
                LocalizationManager.GetString("RelationshipFamily"),
                LocalizationManager.GetString("RelationshipOther")
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
                    if (!string.IsNullOrEmpty(NewContact.Photo) && !NewContact.IsPhotoDefault)
                    {
                        string oldPhotoPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", NewContact.Photo);
                        if (File.Exists(oldPhotoPath))
                        {
                            File.Delete(oldPhotoPath);
                        }
                    }

                    string extension = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = $"contact_temp_{DateTime.Now.Ticks}{extension}"; // Тимчасове ім'я, ID ще немає
                    string destinationPath = Path.Combine(_photosDirectory, newFileName);

                    File.Copy(openFileDialog.FileName, destinationPath, true);
                    NewContact.Photo = Path.Combine("Photos", newFileName).Replace("\\", "/");
                    NewContact.OnPropertyChanged(nameof(NewContact.Photo));
                    NewContact.OnPropertyChanged(nameof(NewContact.Initials));

                    if (PhotoActionIcon.ContextMenu != null)
                    {
                        PhotoActionIcon.ContextMenu.IsOpen = false;
                    }
                }
                catch (Exception ex)
                {
                    var dialog = new CustomConfirmationDialog
                    {
                        Title = LocalizationManager.GetString("Error"),
                        Message = LocalizationManager.GetString("PhotoUploadError", ex.Message),
                        ConfirmButtonText = LocalizationManager.GetString("OK"),
                        CancelButtonText = ""
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

                    dialog.DialogResult += (s, result) => { };
                    window.ShowDialog();
                }
            }
        }

        private void PhotoActionIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(NewContact.Photo) && !NewContact.IsPhotoDefault)
            {
                if (PhotoActionIcon != null && PhotoActionIcon.ContextMenu != null)
                {
                    PhotoActionIcon.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
            }
            else
            {
                ChoosePhotoCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void RemovePhoto(object parameter)
        {
            if (!string.IsNullOrEmpty(NewContact.Photo) && !NewContact.IsPhotoDefault)
            {
                string fullPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", NewContact.Photo);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        var dialog = new CustomConfirmationDialog
                        {
                            Title = LocalizationManager.GetString("Error"),
                            Message = LocalizationManager.GetString("PhotoDeleteError", ex.Message),
                            ConfirmButtonText = LocalizationManager.GetString("OK"),
                            CancelButtonText = ""
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

                        dialog.DialogResult += (s, result) => { };
                        window.ShowDialog();
                        return;
                    }
                }
                NewContact.Photo = null;
                NewContact.OnPropertyChanged(nameof(NewContact.Photo));
                NewContact.OnPropertyChanged(nameof(NewContact.Initials));
                NewContact.Initialize();
            }

            if (PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.IsOpen = false;
            }
        }

        private void CancelContextMenu(object parameter)
        {
            if (PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.IsOpen = false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Після збереження оновлюємо ім'я файлу фото з правильним ID
            if (!string.IsNullOrEmpty(NewContact.Photo) && !NewContact.IsPhotoDefault)
            {
                string oldPhotoPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", NewContact.Photo);
                if (File.Exists(oldPhotoPath))
                {
                    string extension = Path.GetExtension(NewContact.Photo);
                    string newFileName = $"contact_{NewContact.Id}_{DateTime.Now.Ticks}{extension}";
                    string newPhotoPath = Path.Combine(_photosDirectory, newFileName);
                    File.Move(oldPhotoPath, newPhotoPath);
                    NewContact.Photo = Path.Combine("Photos", newFileName).Replace("\\", "/");
                }
            }

            _contactService.AddContact(NewContact);
            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Видаляємо тимчасове фото, якщо воно було завантажене
            if (!string.IsNullOrEmpty(NewContact.Photo) && !NewContact.IsPhotoDefault)
            {
                string fullPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", NewContact.Photo);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        var dialog = new CustomConfirmationDialog
                        {
                            Title = LocalizationManager.GetString("Error"),
                            Message = LocalizationManager.GetString("TempPhotoDeleteError", ex.Message),
                            ConfirmButtonText = LocalizationManager.GetString("OK"),
                            CancelButtonText = ""
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

                        dialog.DialogResult += (s, result) => { };
                        window.ShowDialog();
                    }
                }
            }
            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));
        }

        private void ToggleFavourite_Click(object sender, MouseButtonEventArgs e)
        {
            NewContact.IsFavourite = !NewContact.IsFavourite;
            if (sender is TextBlock star)
            {
                var bindingExpression = star.GetBindingExpression(TextBlock.TagProperty);
                bindingExpression?.UpdateTarget();
            }
        }

        private void AddPhoneNumber_Click(object sender, RoutedEventArgs e)
        {
            Phones.Add(new PhoneNumber { TypeKey = "PhoneTypeMobile", Number = "" });
        }

        private void RemovePhoneNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PhoneNumber phone)
            {
                Phones.Remove(phone);
            }
        }

        private void AddEmail_Click(object sender, RoutedEventArgs e)
        {
            Emails.Add(new Email { TypeKey = "EmailTypePersonal", Address = "" });
        }

        private void RemoveEmail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Email email)
            {
                Emails.Remove(email);
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            InitializeComboBoxLists();
            var phoneTypes = PhoneTypes.ToList();
            PhoneTypes.Clear();
            foreach (var type in phoneTypes) PhoneTypes.Add(type);
            var emailTypes = EmailTypes.ToList();
            EmailTypes.Clear();
            foreach (var type in emailTypes) EmailTypes.Add(type);
            var relationships = Relationships.ToList();
            Relationships.Clear();
            foreach (var rel in relationships) Relationships.Add(rel);
            DataContext = null;
            DataContext = this;
            foreach (var phone in Phones)
            {
                phone.OnPropertyChanged(nameof(PhoneNumber.Type));
            }
            foreach (var email in Emails)
            {
                email.OnPropertyChanged(nameof(Email.Type));
            }
            NewContact.OnPropertyChanged(nameof(Contact.Relationship));
        }

        ~AddContactView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}