using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;

namespace ContactManagerApp.Models
{
    public class User : INotifyPropertyChanged
    {
        private string _username;
        private string _password;
        private string _salt;
        private string _role;
        private bool _isActive;
        private string _userId;
        private string _language = "uk";
        private string _theme = "Light";
        private string _token;
        private DateTime? _tokenExpiration;
        private string _contactFolderCode;
        private string _name;
        private string _phoneNumber;
        private string _email;
        private string _photoPath;
        private bool _isPhotoDefault;

        public event PropertyChangedEventHandler PropertyChanged;

        public User()
        {
            UserId = Guid.NewGuid().ToString();
            ContactFolderCode = Guid.NewGuid().ToString("N");
            UpdatePhotoIfNameIsEmptyOrDigits();
        }

        // Метод для створення копії об’єкта
        public User Clone()
        {
            return new User
            {
                Username = this.Username,
                Password = this.Password,
                Salt = this.Salt,
                Role = this.Role,
                IsActive = this.IsActive,
                UserId = this.UserId,
                Language = this.Language,
                Theme = this.Theme,
                Token = this.Token,
                TokenExpiration = this.TokenExpiration,
                ContactFolderCode = this.ContactFolderCode,
                Name = this.Name,
                PhoneNumber = this.PhoneNumber,
                Email = this.Email,
                PhotoPath = this.PhotoPath,
                _isPhotoDefault = this.IsPhotoDefault
            };
        }

        // Метод для відновлення стану з копії
        public void RestoreFrom(User other)
        {
            this.Username = other.Username;
            this.Password = other.Password;
            this.Salt = other.Salt;
            this.Role = other.Role;
            this.IsActive = other.IsActive;
            this.UserId = other.UserId;
            this.Language = other.Language;
            this.Theme = other.Theme;
            this.Token = other.Token;
            this.TokenExpiration = other.TokenExpiration;
            this.ContactFolderCode = other.ContactFolderCode;
            this.Name = other.Name;
            this.PhoneNumber = other.PhoneNumber;
            this.Email = other.Email;
            this.PhotoPath = other.PhotoPath;
            this._isPhotoDefault = other.IsPhotoDefault;

            // Сповіщаємо про зміну всіх властивостей
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(PhoneNumber));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(PhotoPath));
            OnPropertyChanged(nameof(IsPhotoDefault));
            OnPropertyChanged(nameof(Initials));
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string Salt
        {
            get => _salt;
            set
            {
                _salt = value;
                OnPropertyChanged();
            }
        }

        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged();
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                OnPropertyChanged();
            }
        }

        public string Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                OnPropertyChanged();
            }
        }

        public string Token
        {
            get => _token;
            set
            {
                _token = value;
                OnPropertyChanged();
            }
        }

        public DateTime? TokenExpiration
        {
            get => _tokenExpiration;
            set
            {
                _tokenExpiration = value;
                OnPropertyChanged();
            }
        }

        public string ContactFolderCode
        {
            get => _contactFolderCode;
            set
            {
                _contactFolderCode = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Initials));
                UpdatePhotoIfNameIsEmptyOrDigits();
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                _phoneNumber = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string PhotoPath
        {
            get => _photoPath;
            set
            {
                _photoPath = value;
                string normalizedPhotoPath = _photoPath?.Replace("\\", "/");
                string defaultPhotoPath = Path.Combine("Assets", "Photo", "default_photo.png").Replace("\\", "/");
                _isPhotoDefault = normalizedPhotoPath == defaultPhotoPath;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Initials));
                OnPropertyChanged(nameof(IsPhotoDefault));
            }
        }

        public bool IsPhotoDefault
        {
            get => _isPhotoDefault;
            set
            {
                _isPhotoDefault = value;
                OnPropertyChanged();
            }
        }

        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return "?";
                }

                var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return "?";
                }

                var firstPart = parts[0];
                var firstLetter = firstPart.FirstOrDefault(char.IsLetter);
                if (firstLetter == '\0')
                {
                    return "?";
                }

                return firstLetter.ToString().ToUpper();
            }
        }

        private void UpdatePhotoIfNameIsEmptyOrDigits()
        {
            bool isEmpty = string.IsNullOrEmpty(Name);
            bool isOnlyDigits = !isEmpty && Name.All(c => char.IsDigit(c) || char.IsWhiteSpace(c));
            bool isOnlySpecialChars = !isEmpty && Name.All(c => char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c));
            string defaultPhotoPathRelative = Path.Combine("Assets", "Photo", "default_photo.png").Replace("\\", "/");

            if ((isEmpty || isOnlyDigits || isOnlySpecialChars) && string.IsNullOrEmpty(PhotoPath))
            {
                string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
                string defaultPhotoPath = Path.Combine(projectRoot, "Assets", "Photo", "default_photo.png");

                if (File.Exists(defaultPhotoPath))
                {
                    PhotoPath = defaultPhotoPathRelative;
                }
                else
                {
                    PhotoPath = null;
                }
            }
            else if (!(isEmpty || isOnlyDigits || isOnlySpecialChars) && IsPhotoDefault)
            {
                PhotoPath = null;
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}