using ContactManagerApp.Models;
using ContactManagerApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ContactManagerApp.Views
{
    public partial class EditContactView : Page
    {
        private readonly Contact _originalContact;
        private readonly NavigationService _navigationService;
        private readonly ContactService _contactService;
        public Contact EditedContact { get; set; }
        public string OriginalContactName => _originalContact.Name;
        public ObservableCollection<PhoneNumber> Phones { get; set; }
        public ObservableCollection<Email> Emails { get; set; }
        public List<string> PhoneTypes { get; set; }
        public List<string> EmailTypes { get; set; }
        public List<string> Relationships { get; set; }
        public ICommand ChoosePhotoCommand { get; }
        public ICommand RemovePhotoCommand { get; }
        private readonly string _photosDirectory;
        public ICommand CancelContextMenuCommand { get; }

        public EditContactView(Contact contact, NavigationService navigationService, ContactService contactService)
        {
            InitializeComponent();
            _originalContact = contact;
            _navigationService = navigationService;
            _contactService = contactService;

            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            _photosDirectory = Path.Combine(projectRoot, "Data", "Photos");
            if (!Directory.Exists(_photosDirectory))
            {
                Directory.CreateDirectory(_photosDirectory);
            }

            EditedContact = new Contact
            {
                Id = contact.Id,
                Name = contact.Name,
                Phones = new ObservableCollection<PhoneNumber>(contact.Phones),
                Emails = new ObservableCollection<Email>(contact.Emails),
                IsFavourite = contact.IsFavourite,
                Address = contact.Address,
                Photo = contact.Photo,
                RelationshipKey = contact.RelationshipKey,
                IsDeleted = contact.IsDeleted,
                Notes = contact.Notes,
                CreatedDate = contact.CreatedDate,
                UpdatedDate = contact.UpdatedDate
            };

            EditedContact.Initialize();
            
            InitializeComboBoxLists();
            Phones = EditedContact.Phones;
            Emails = EditedContact.Emails;

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
                    if (!string.IsNullOrEmpty(EditedContact.Photo) && !EditedContact.IsPhotoDefault)
                    {
                        string oldPhotoPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", EditedContact.Photo);
                        if (File.Exists(oldPhotoPath))
                        {
                            File.Delete(oldPhotoPath);
                        }
                    }

                    string extension = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = $"contact_{EditedContact.Id}_{DateTime.Now.Ticks}{extension}";
                    string destinationPath = Path.Combine(_photosDirectory, newFileName);

                    File.Copy(openFileDialog.FileName, destinationPath, true);
                    EditedContact.Photo = Path.Combine("Photos", newFileName).Replace("\\", "/");
                    EditedContact.OnPropertyChanged(nameof(EditedContact.Photo));
                    EditedContact.OnPropertyChanged(nameof(EditedContact.Initials));

                    if (PhotoActionIcon.ContextMenu != null)
                    {
                        PhotoActionIcon.ContextMenu.IsOpen = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uploading photo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PhotoActionIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(EditedContact.Photo) && !EditedContact.IsPhotoDefault)
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
            if (!string.IsNullOrEmpty(EditedContact.Photo) && !EditedContact.IsPhotoDefault)
            {
                string fullPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", EditedContact.Photo);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting photo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                EditedContact.Photo = null;
                EditedContact.OnPropertyChanged(nameof(EditedContact.Photo));
                EditedContact.OnPropertyChanged(nameof(EditedContact.Initials));
                EditedContact.Initialize();
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
            _originalContact.Name = EditedContact.Name;
            _originalContact.Phones.Clear();
            foreach (var phone in EditedContact.Phones)
            {
                _originalContact.Phones.Add(phone);
            }
            _originalContact.Emails.Clear();
            foreach (var email in EditedContact.Emails)
            {
                _originalContact.Emails.Add(email);
            }
            _originalContact.IsFavourite = EditedContact.IsFavourite;
            _originalContact.Address = EditedContact.Address;
            _originalContact.Photo = EditedContact.Photo;
            _originalContact.RelationshipKey = EditedContact.RelationshipKey;
            _originalContact.Notes = EditedContact.Notes;
            _originalContact.UpdatedDate = DateTime.Now;

            _contactService.UpdateContact(_originalContact);
            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(new ContactListView(_navigationService, _contactService));
        }

        private void ToggleFavourite_Click(object sender, MouseButtonEventArgs e)
        {
            EditedContact.IsFavourite = !EditedContact.IsFavourite;
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
            EditedContact.OnPropertyChanged(nameof(Contact.Relationship));
        }

        ~EditContactView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }
    }

    public class BooleanToForegroundConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isFavourite && isFavourite)
            {
                return "#FFD700";
            }
            return "#666666";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathToImageSourceConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string photoPath && !string.IsNullOrEmpty(photoPath))
            {
                try
                {
                    string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
                    string fullPath;

                    if (photoPath.StartsWith("Assets/Photo/"))
                    {
                        fullPath = Path.Combine(projectRoot, photoPath);
                    }
                    else
                    {
                        fullPath = Path.Combine(projectRoot, "Data", photoPath);
                    }
                    
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        return bitmap;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}