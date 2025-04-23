using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ContactManagerApp.Services
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static readonly LocalizationManager _instance = new LocalizationManager();
        public static LocalizationManager Instance => _instance;

        private string _currentLanguage = "uk";
        private readonly Dictionary<string, Dictionary<string, string>> _translations;

        // Ключі, які завжди повертають англійські переклади (для LoginView і RegistrationView)
        private static readonly HashSet<string> _englishOnlyKeys = new HashSet<string>
        {
            "Welcome",
            "Username",
            "Password",
            "RememberMe",
            "LoginButton",
            "NoAccountPrompt",
            "SignUpButton",
            "EmptyFieldsError",
            "UserIdEmptyError",
            "LoginSuccess",
            "InvalidCredentialsError",
            "CreateAccount",
            "RegisterButton",
            "AlreadyHaveAccount",
            "SignInButton",
            "AllFieldsRequired",
            "PasswordMinLength",
            "UserExistsError",
            "RegistrationSuccess",
            "LoginAfterRegistrationError",
            "RegistrationFailed",
            "Error"
        };

        public event PropertyChangedEventHandler PropertyChanged;
        public static event EventHandler LanguageChanged;

        public static string CurrentLanguage
        {
            get => _instance._currentLanguage;
            set
            {
                if (_instance._currentLanguage != value)
                {
                    _instance._currentLanguage = value;
                    _instance.OnPropertyChanged(nameof(CurrentLanguage));
                    LanguageChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        private LocalizationManager()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["uk"] = new Dictionary<string, string>
                {
                    ["CreateContact"] = "Створити контакт",
                    ["Contacts"] = "Контакти",
                    ["Selected"] = "Вибрані",
                    ["Frequent"] = "Часті",
                    ["Password2"] = "Password",
                    ["FindDuplicate"] = "Пошук дублікатів",
                    ["Basket"] = "Кошик",
                    ["Settings"] = "Налаштування",
                    ["Organization"] = "Упорядкування",
                    ["FavouriteContacts"] = "Вибрані контакти",
                    ["LoginHeader"] = "Вхід в систему",
                    ["Login"] = "Логін",
                    ["Password"] = "Пароль",
                    ["LoginButton"] = "Увійти",
                    ["RegisterHeader"] = "Реєстрація",
                    ["RegisterButton"] = "Зареєструватися",
                    ["UserExistsError"] = "Користувач з таким логіном уже існує",
                    ["InvalidCredentialsError"] = "Неправильний логін або пароль",
                    ["Language"] = "Мова",
                    ["DeleteAccountSuccess"] = "Delete account success",
                    ["Theme"] = "Тема",
                    ["LightTheme"] = "Світла тема",
                    ["DarkTheme"] = "Темна тема",
                    ["HighContrastTheme"] = "Контрастна тема",
                    ["English"] = "Англійська",
                    ["Ukrainian"] = "Українська",
                    ["Edit"] = "Редагувати",
                    ["AddContact"] = "Додати контакт",
                    ["Save"] = "Зберегти",
                    ["Cancel"] = "Скасувати",
                    ["ChangePhoto"] = "Змінити зображення",
                    ["RemovePhoto"] = "Вилучити зображення",
                    ["Name"] = "Ім’я",
                    ["Surname"] = "Прізвище",
                    ["Company"] = "Компанія",
                    ["Address"] = "Адреса",
                    ["Photo"] = "Фото",
                    ["IsFavourite"] = "Вибране",
                    ["Day"] = "День",
                    ["Month"] = "Місяць",
                    ["Website"] = "Веб-сайт",
                    ["Relationship"] = "Зв’язок",
                    ["CustomField"] = "Спеціальне поле",
                    ["Notes"] = "Примітки",
                    ["AddPhoneNumber"] = "+ Додати номер телефону",
                    ["AddEmail"] = "+ Додати ел. пошту",
                    ["Phone"] = "Телефон",
                    ["Initials"] = "Ініціали",
                    ["Labels"] = "Мітки",
                    ["Email"] = "Ел. пошта",
                    ["PhoneTypeMobile"] = "Мобільний",
                    ["PhoneTypeWork"] = "Робочий",
                    ["PhoneTypeHome"] = "Домашній",
                    ["PhoneTypeOther"] = "Інший",
                    ["EmailTypePersonal"] = "Особистий",
                    ["EmailTypeWork"] = "Робочий",
                    ["EmailTypeOther"] = "Інший",
                    ["RelationshipFriend"] = "Друг",
                    ["RelationshipColleague"] = "Колега",
                    ["RelationshipFamily"] = "Сім’я",
                    ["RelationshipOther"] = "Інший",
                    ["None"] = "Немає",
                    ["January"] = "Січня",
                    ["February"] = "Лютого",
                    ["March"] = "Березня",
                    ["April"] = "Квітня",
                    ["May"] = "Травня",
                    ["June"] = "Червня",
                    ["July"] = "Липня",
                    ["August"] = "Серпня",
                    ["September"] = "Вересня",
                    ["October"] = "Жовтня",
                    ["November"] = "Листопада",
                    ["December"] = "Грудня",
                    ["Back"] = "Назад",
                    ["ContactInfo"] = "Контактна інформація",
                    ["History"] = "Історія",
                    ["CreatedDateLabel"] = "Додано до списку контактів • {0:dd MMMM yyyy}р.",
                    ["UpdatedDateLabel"] = "Останнє оновлення • {0:dd MMMM yyyy}р.",
                    ["CreatedDate"] = "Дата створення",
                    ["UpdatedDate"] = "Дата оновлення",
                    ["ToggleFavourite"] = "Перемкнути Вибране",
                    ["ConfirmDeleteMessage"] = "Ви впевнені, що хочете видалити цей контакт?",
                    ["ConfirmPermanentDeleteMessage"] = "Цей контакт буде остаточно видалено без можливості відновлення. Ви впевнені?",
                    ["ConfirmDeleteTitle"] = "Підтвердження видалення",
                    ["DeletionDate"] = "Дата видалення",
                    ["Today"] = "Сьогодні",
                    ["Yesterday"] = "Вчора",
                    ["Separator"] = "о",
                    ["ViewButtonText"] = "Переглянути",
                    ["SaveContactError"] = "Помилка при збереженні контакту: {0}",
                    ["ContactNotFoundError"] = "Контакт з Id {0} не знайдено",
                    ["LoadContactsError"] = "Помилка при читанні контактів: {0}",
                    ["SaveContactsError"] = "Помилка при збереженні контактів: {0}",
                    ["LoadUsersError"] = "Помилка при зчитуванні файлу користувачів: {0}",
                    ["SaveUsersError"] = "Помилка при записі файлу користувачів: {0}",
                    ["LoadSettingsError"] = "Помилка при читанні налаштувань: {0}",
                    ["SaveSettingsError"] = "Помилка при збереженні налаштувань: {0}",
                    ["NameCannotBeEmptyError"] = "Ім'я контакту не може бути порожнім",
                    ["InvalidEmailError"] = "Невірний формат ел. пошти: {0}",
                    ["Error"] = "Помилка",
                    ["DeleteContact"] = "Видалити контакт",
                    ["Delete"] = "Видалити",
                    ["Restore"] = "Відновити",
                    ["DeletePermanently"] = "Видалити назавжди",
                    ["SelectAll"] = "Вибрати все",
                    ["ClearSelection"] = "Скасувати виділення",
                    ["MergeDuplicatesHeader"] = "Об'єднайте дубльовані контакти",
                    ["MergeAllButton"] = "Об'єднати все",
                    ["NoDuplicatesMessage"] = "Немає нових пропозицій",
                    ["MergeButton"] = "Об'єднати",
                    ["ChangePassword"] = "Змінити пароль",
                    ["NewPassword"] = "Новий пароль",
                    ["OldPassword"] = "Пароль",
                    ["ConfirmNewPassword"] = "Підтвердити новий пароль",
                    ["SavePassword"] = "Зберегти пароль",
                    ["DeleteAccount"] = "Видалити акаунт",
                    ["DeleteAccountConfirmation"] = "Ви впевнені, що хочете видалити свій акаунт? Цю дію неможливо скасувати.",
                    ["AllFieldsRequired"] = "Усі поля обов’язкові.",
                    ["PasswordMinLength"] = "Пароль має містити мінімум 8 символів.",
                    ["PasswordsDoNotMatch"] = "Новий пароль і підтвердження не збігаються.",
                    ["UserNotFound"] = "Користувача не знайдено.",
                    ["OldPasswordIncorrect"] = "Старий пароль введено неправильно.",
                    ["PasswordChangedSuccess"] = "Пароль успішно змінено!",
                    ["DeleteAccountFailed"] = "Не вдалося видалити акаунт.",
                    ["Logout"] = "Вихід"
                },
                ["en"] = new Dictionary<string, string>
                {
                    ["CreateContact"] = "Create Contact",
                    ["Contacts"] = "Contacts",
                    ["Selected"] = "Selected",
                    ["Frequent"] = "Frequent",
                    ["Password2"] = "dds",
                    ["FindDuplicate"] = "Find Duplicates",
                    ["Basket"] = "Trash",
                    ["Settings"] = "Settings",
                    ["Organization"] = "Organization",
                    ["FavouriteContacts"] = "Selected contacts",
                    ["LoginHeader"] = "Sign In",
                    ["Login"] = "Login",
                    ["Password"] = "Password",
                    ["LoginButton"] = "Sign In",
                    ["RegisterHeader"] = "Register",
                    ["RegisterButton"] = "Register",
                    ["UserExistsError"] = "User with this login already exists",
                    ["InvalidCredentialsError"] = "Invalid login or password",
                    ["Language"] = "Language",
                    ["DeleteAccountSuccess"] = "Delete account success",
                    ["Theme"] = "Theme",
                    ["LightTheme"] = "Light Theme",
                    ["DarkTheme"] = "Dark Theme",
                    ["HighContrastTheme"] = "High Contrast Theme",
                    ["English"] = "English",
                    ["Ukrainian"] = "Ukrainian",
                    ["Edit"] = "Edit",
                    ["AddContact"] = "Add contact",
                    ["Save"] = "Save",
                    ["Cancel"] = "Cancel",
                    ["ChangePhoto"] = "Change picture",
                    ["RemovePhoto"] = "Remove picture",
                    ["Name"] = "Name",
                    ["Surname"] = "Surname",
                    ["Company"] = "Company",
                    ["Address"] = "Address",
                    ["Photo"] = "Photo",
                    ["IsFavourite"] = "Favorite",
                    ["Day"] = "Day",
                    ["Month"] = "Month",
                    ["Website"] = "Website",
                    ["Relationship"] = "Relationship",
                    ["CustomField"] = "Custom Field",
                    ["Notes"] = "Notes",
                    ["AddPhoneNumber"] = "+ Add phone number",
                    ["AddEmail"] = "+ Add email",
                    ["Phone"] = "Phone",
                    ["Initials"] = "Initials",
                    ["Labels"] = "Labels",
                    ["Email"] = "Email",
                    ["PhoneTypeMobile"] = "Mobile",
                    ["PhoneTypeWork"] = "Work",
                    ["PhoneTypeHome"] = "Home",
                    ["PhoneTypeOther"] = "Other",
                    ["EmailTypePersonal"] = "Personal",
                    ["EmailTypeWork"] = "Work",
                    ["EmailTypeOther"] = "Other",
                    ["RelationshipFriend"] = "Friend",
                    ["RelationshipColleague"] = "Colleague",
                    ["RelationshipFamily"] = "Family",
                    ["RelationshipOther"] = "Other",
                    ["None"] = "None",
                    ["January"] = "January",
                    ["February"] = "February",
                    ["March"] = "March",
                    ["April"] = "April",
                    ["May"] = "May",
                    ["June"] = "June",
                    ["July"] = "July",
                    ["August"] = "August",
                    ["September"] = "September",
                    ["October"] = "October",
                    ["November"] = "November",
                    ["December"] = "December",
                    ["Back"] = "Back",
                    ["ContactInfo"] = "Contact Info",
                    ["History"] = "History",
                    ["CreatedDateLabel"] = "Added to contact list • {0:dd MMMM yyyy} year",
                    ["UpdatedDateLabel"] = "Last updated • {0:dd MMMM yyyy} year",
                    ["CreatedDate"] = "Created Date",
                    ["UpdatedDate"] = "Updated Date",
                    ["ToggleFavourite"] = "Toggle Favorite",
                    ["ConfirmDeleteMessage"] = "Are you sure you want to delete this contact?",
                    ["ConfirmPermanentDeleteMessage"] = "This contact will be permanently deleted and cannot be restored. Are you sure?",
                    ["ConfirmDeleteTitle"] = "Confirm Deletion",
                    ["DeletionDate"] = "Deletion date",
                    ["Today"] = "Today",
                    ["Yesterday"] = "Yesterday",
                    ["Separator"] = "at",
                    ["ViewButtonText"] = "View",
                    ["SaveContactError"] = "Error saving contact: {0}",
                    ["ContactNotFoundError"] = "Contact with Id {0} not found",
                    ["LoadContactsError"] = "Error loading contacts: {0}",
                    ["SaveContactsError"] = "Error saving contacts: {0}",
                    ["LoadUsersError"] = "Error reading users file: {0}",
                    ["SaveUsersError"] = "Error writing users file: {0}",
                    ["LoadSettingsError"] = "Error loading settings: {0}",
                    ["SaveSettingsError"] = "Error saving settings: {0}",
                    ["NameCannotBeEmptyError"] = "Contact name cannot be empty",
                    ["InvalidEmailError"] = "Invalid email format: {0}",
                    ["Error"] = "Error",
                    ["DeleteContact"] = "Delete Contact",
                    ["Delete"] = "Delete",
                    ["Restore"] = "Restore",
                    ["DeletePermanently"] = "Delete permanently",
                    ["SelectAll"] = "Select all",
                    ["ClearSelection"] = "Clear selection",
                    ["MergeDuplicatesHeader"] = "Merge duplicate contacts",
                    ["MergeAllButton"] = "Merge All",
                    ["NoDuplicatesMessage"] = "No new suggestions",
                    ["MergeButton"] = "Merge",
                    ["ChangePassword"] = "Change Password",
                    ["NewPassword"] = "New Password",
                    ["OldPassword"] = "Password",
                    ["ConfirmNewPassword"] = "Confirm New Password",
                    ["SavePassword"] = "Save Password",
                    ["DeleteAccount"] = "Delete Account",
                    ["DeleteAccountConfirmation"] = "Are you sure you want to delete your account? This action cannot be undone.",
                    ["AllFieldsRequired"] = "All fields are required.",
                    ["PasswordMinLength"] = "Password must be at least 8 characters long.",
                    ["PasswordsDoNotMatch"] = "New password and confirmation do not match.",
                    ["UserNotFound"] = "User not found.",
                    ["OldPasswordIncorrect"] = "Old password is incorrect.",
                    ["PasswordChangedSuccess"] = "Password changed successfully!",
                    ["DeleteAccountFailed"] = "Failed to delete account.",
                    ["Logout"] = "Logout",
                    // Додаткові ключі для LoginView і RegistrationView (тільки англійська)
                    ["Welcome"] = "Welcome",
                    ["Username"] = "Username",
                    ["Password"] = "Password",
                    ["RememberMe"] = "Remember Me",
                    ["LoginButton"] = "Login",
                    ["NoAccountPrompt"] = "Don't have an account yet?",
                    ["SignUpButton"] = "Sign up",
                    ["EmptyFieldsError"] = "All fields are required.",
                    ["UserIdEmptyError"] = "Error: UserId is empty",
                    ["LoginSuccess"] = "Login successful!",
                    ["CreateAccount"] = "Create an account",
                    ["RegisterButton"] = "Register",
                    ["AlreadyHaveAccount"] = "Already have an account?",
                    ["SignInButton"] = "Sign in",
                    ["LoginAfterRegistrationError"] = "Error: Failed to log in after registration",
                    ["RegistrationSuccess"] = "Registration successful!",
                    ["RegistrationFailed"] = "Registration failed. Please try again."
                }
            };
        }

        public static string GetString(string key, params object[] args)
        {
            // Якщо ключ належить до англійських вікон (LoginView або RegistrationView), завжди повертаємо англійський переклад
            string language = _englishOnlyKeys.Contains(key) ? "en" : _instance._currentLanguage;

            if (_instance._translations.TryGetValue(language, out var languageDict) &&
                languageDict.TryGetValue(key, out var value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }
            return key;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}