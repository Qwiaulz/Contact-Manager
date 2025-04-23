    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using System.Linq;
    using System.IO;
    using ContactManagerApp.Services;

    namespace ContactManagerApp.Models
    {
        public class Contact : INotifyPropertyChanged
        {
            private bool _isSelected;
            private string _relationshipKey;
            private string _name;
            private string _photo;
            private bool _isInitializing;
            private bool _isPhotoDefault;
            private DateTime? _deletionDate;
            private bool _isFavourite;

            public event PropertyChangedEventHandler PropertyChanged;

            public Contact()
            {
                _isInitializing = true;
                Phones = new ObservableCollection<PhoneNumber>();
                Emails = new ObservableCollection<Email>();
                // Підписка на зміни колекцій
                Phones.CollectionChanged += (s, e) => OnPropertyChanged(nameof(FirstPhone));
                Emails.CollectionChanged += (s, e) => OnPropertyChanged(nameof(FirstEmail));
            }
            
            public int Id { get; set; }
            public string UserId { get; set; }

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Initials));
                    UpdatePhotoIfNameIsEmptyOrDigits();
                    _isInitializing = false;
                }
            }

            public ObservableCollection<PhoneNumber> Phones { get; set; }
            public ObservableCollection<Email> Emails { get; set; }
            public bool IsFavourite
            {
                get => _isFavourite;
                set
                {
                    _isFavourite = value;
                    OnPropertyChanged(); // Додаємо сповіщення
                }
            }
            public string Address { get; set; }

            public string Photo
            {
                get => _photo;
                set
                {
                    _photo = value;
                    string normalizedPhotoPath = _photo?.Replace("\\", "/");
                    string defaultPhotoPath = Path.Combine("Assets", "Photo", "default_photo.png").Replace("\\", "/");
                    _isPhotoDefault = normalizedPhotoPath == defaultPhotoPath;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Initials));
                    OnPropertyChanged(nameof(IsPhotoDefault));
                }
            }

            [JsonIgnore]
            public bool IsPhotoDefault
            {
                get => _isPhotoDefault;
                set
                {
                    _isPhotoDefault = value;
                    OnPropertyChanged();
                }
            }
            
            public DateTime? DeletionDate
            {
                get => _deletionDate;
                set
                {
                    _deletionDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DeletionDateFormatted));
                }
            }

            public bool IsDeleted { get; set; }
            public string Notes { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }

            public string RelationshipKey
            {
                get => _relationshipKey;
                set
                {
                    _relationshipKey = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Relationship));
                }
            }

            [JsonIgnore]
            public string Relationship
            {
                get => !string.IsNullOrEmpty(_relationshipKey) ? LocalizationManager.GetString(_relationshipKey) : string.Empty;
                set
                {
                    var relationshipKeys = new List<string>
                    {
                        "RelationshipFriend",
                        "RelationshipColleague",
                        "RelationshipFamily",
                        "RelationshipOther"
                    };

                    foreach (var key in relationshipKeys)
                    {
                        if (LocalizationManager.GetString(key) == value)
                        {
                            _relationshipKey = key;
                            break;
                        }
                    }

                    OnPropertyChanged(nameof(RelationshipKey));
                    OnPropertyChanged();
                }
            }

            [JsonIgnore]
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }

            [JsonIgnore]
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

                    var result = firstLetter.ToString().ToUpper();
                    return result;
                }
            }

            // Нова властивість для першого телефону
            [JsonIgnore]
            public string FirstPhone
            {
                get
                {
                    var firstPhone = Phones.FirstOrDefault(p => !string.IsNullOrEmpty(p.Number));
                    return firstPhone?.Number ?? string.Empty;
                }
            }

            // Нова властивість для першого email
            [JsonIgnore]
            public string FirstEmail
            {
                get
                {
                    var firstEmail = Emails.FirstOrDefault(e => !string.IsNullOrEmpty(e.Address));
                    return firstEmail?.Address ?? string.Empty;
                }
            }
            
            [JsonIgnore]
            public string DeletionDateFormatted
            {
                get
                {
                    if (!DeletionDate.HasValue)
                        return string.Empty;

                    var deletionDate = DeletionDate.Value;
                    var now = DateTime.Now;
                    var today = now.Date;
                    var yesterday = today.AddDays(-1);
                    var separator = LocalizationManager.GetString("Separator");

                    string timePart = deletionDate.ToString("HH:mm");
                    string datePart;

                    if (deletionDate.Date == today)
                    {
                        datePart = LocalizationManager.GetString("Today");
                    }
                    else if (deletionDate.Date == yesterday)
                    {
                        datePart = LocalizationManager.GetString("Yesterday");
                    }
                    else
                    {
                        datePart = deletionDate.ToString("dd.MM.yyyy");
                    }

                    return $"{datePart} {separator} {timePart}";
                }
            }

            private void UpdatePhotoIfNameIsEmptyOrDigits()
            {
                bool isEmpty = string.IsNullOrEmpty(Name);
                bool isOnlyDigits = !isEmpty && Name.All(c => char.IsDigit(c) || char.IsWhiteSpace(c));
                string defaultPhotoPathRelative = Path.Combine("Assets", "Photo", "default_photo.png").Replace("\\", "/");

                if ((isEmpty || isOnlyDigits) && string.IsNullOrEmpty(Photo))
                {
                    string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
                    string defaultPhotoPath = Path.Combine(projectRoot, "Assets", "Photo", "default_photo.png");

                    if (File.Exists(defaultPhotoPath))
                    {
                        Photo = defaultPhotoPathRelative;
                    }
                    else
                    {
                        Photo = null;
                    }
                }
                else if (!(isEmpty || isOnlyDigits) && IsPhotoDefault)
                {
                    Photo = null;
                }
            }

            public void Initialize()
            {
                UpdatePhotoIfNameIsEmptyOrDigits();
                _isInitializing = false;
            }

            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class PhoneNumber : INotifyPropertyChanged
        {
            private string _typeKey;
            private string _number;

            public event PropertyChangedEventHandler PropertyChanged;

            public string TypeKey
            {
                get => _typeKey;
                set
                {
                    _typeKey = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Type));
                }
            }

            [JsonIgnore]
            public string Type
            {
                get => !string.IsNullOrEmpty(_typeKey) ? LocalizationManager.GetString(_typeKey) : string.Empty;
                set
                {
                    var typeKeys = new List<string>
                    {
                        "PhoneTypeMobile",
                        "PhoneTypeWork",
                        "PhoneTypeHome",
                        "PhoneTypeOther"
                    };

                    foreach (var key in typeKeys)
                    {
                        if (LocalizationManager.GetString(key) == value)
                        {
                            _typeKey = key;
                            break;
                        }
                    }

                    OnPropertyChanged(nameof(TypeKey));
                    OnPropertyChanged();
                }
            }

            public string Number
            {
                get => _number;
                set
                {
                    _number = value;
                    OnPropertyChanged();
                    // Сповіщаємо про зміну FirstPhone у Contact
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(Number)));
                    }
                }
            }

            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class Email : INotifyPropertyChanged
        {
            private string _typeKey;
            private string _address;

            public event PropertyChangedEventHandler PropertyChanged;

            public string TypeKey
            {
                get => _typeKey;
                set
                {
                    _typeKey = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Type));
                }
            }

            [JsonIgnore]
            public string Type
            {
                get => !string.IsNullOrEmpty(_typeKey) ? LocalizationManager.GetString(_typeKey) : string.Empty;
                set
                {
                    var typeKeys = new List<string>
                    {
                        "EmailTypePersonal",
                        "EmailTypeWork",
                        "EmailTypeOther"
                    };

                    foreach (var key in typeKeys)
                    {
                        if (LocalizationManager.GetString(key) == value)
                        {
                            _typeKey = key;
                            break;
                        }
                    }

                    OnPropertyChanged(nameof(TypeKey));
                    OnPropertyChanged();
                }
            }

            public string Address
            {
                get => _address;
                set
                {
                    _address = value;
                    OnPropertyChanged();
                    // Сповіщаємо про зміну FirstEmail у Contact
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(Address)));
                    }
                }
            }
            
            

            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }