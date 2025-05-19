# 📇 Contact Manager

## 📑 Table of Contents

- [🚀 Project Overview](#-project-overview)
- [🛠️ Features](#️-features)
- [🔧 Technologies and Architecture](#-technologies-and-architecture)
- [📋 System Requirements](#-system-requirements)
- [📥 Installation](#-installation)
- [📝 Project Structure](#-project-structure)
- [👤 Author](#-author)

## 🚀 Project Overview

**Contact Manager** is a desktop application for Windows developed in C# using Windows Presentation Foundation. It enables efficient management of personal contacts, including adding, editing, deleting, and searching contact information. The application also supports detection and merging of duplicate contacts

## 🛠️ Features

- **Add New Contacts**: Create new entries with detailed information.
- **Edit Contacts**: Update existing records at any time
- **Delete Contacts**: Easily remove unnecessary entries
- **Search Contacts**: Quickly search by name or other criteria
- **Duplicate Detection and Merging**: Identify duplicates based on phone numbers and merge them
- **Data Persistence**: Uses a local JSON file to save contact information

## 🔧 Technologies and Architecture

- **Programming Language**: C#
- **Platform**: .NET Framework 4.7.2
- **User Interface**: WPF
- **Data Storage**: Local JSON file

## 📋 System Requirements

- **Operating System**: Windows 7 or later
- **.NET Framework**: Version 4.7.2 or later
- **RAM**: Minimum 2 GB
- **Disk Space**: Minimum 15 MB

## 📥 Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/Qwiaulz/Contact-Manager.git
   ```
2. **Open the project**:
   - Open the `ContactManagerApp.sln` file in Rider, Visual Studio or VS Code
3. **Run the application**:
   - Press `Start` in IDE

## 📝 Project Structure

```
Contact-Manager/
├── ContactManagerApp.sln                  # Solution file
├── ContactManagerApp/                     # Main project directory
│   ├── App.xaml                           # Application configuration
│   ├── App.xaml.cs                        # Application logic
│   ├── AssemblyInfo.cs                    # Assembly metadata
│
│   ├── Assets/                            # Static resources
│   │   ├── default_photo.png
│   │   ├── icon.ico
│
│   ├── Converters/                        # Value converters for XAML
│   │   ├── BooleanToVisibilityConverter.cs
│   │   ├── CountToVisibilityConverter.cs
│   │   ├── InitialsToColorConverter.cs
│   │   ├── InverseBooleanToVisibilityConverter.cs
│   │   ├── IsNullOrEmptyConverter.cs
│   │   ├── LocalizationConverter.cs
│   │   ├── LocalizedDateConverter.cs
│   │   ├── PhotoVisibilityConverter.cs
│   │   └── StringToVisibilityConverter.cs
│
│   ├── Data/                              # Data files and photos
│   │   ├── Contacts/
│   │   ├── Photos/                        # Placeholder and contact images
│   │   ├── Settings/
│   │   └── Users/                        
│
│   ├── Models/                            # Data models
│   │   ├── Contact.cs
│   │   └── User.cs
│
│   ├── Services/                          # Business logic and services
│   │   ├── AuthService.cs
│   │   ├── ContactService.cs
│   │   ├── LocalizationManager.cs
│   │   ├── NavigationService.cs
│   │   ├── SettingService.cs
│   │   └── ThemeManager.cs
│
│   ├── Themes/                            # UI themes
│   │   ├── DarkTheme.xaml
│   │   ├── LightTheme.xaml
│   │   └── HighContrastTheme.xaml
│
│   ├── Views/                             # UI views and code-behind files
│   │   ├── AddContactView.xaml
│   │   ├── AddContactView.xaml.cs
│   │   ├── AuthView.xaml
│   │   ├── AuthView.xaml.cs
│   │   ├── ContactDetailsView.xaml
│   │   ├── ContactDetailsView.xaml.cs
│   │   ├── ContactListView.xaml
│   │   ├── ContactListView.xaml.cs
│   │   ├── CustomConfirmationDialog.xaml
│   │   ├── CustomConfirmationDialog.xaml.cs
│   │   ├── EditContactView.xaml
│   │   ├── EditContactView.xaml.cs
│   │   ├── FavouriteContactListView.xaml
│   │   ├── FavouriteContactListView.xaml.cs
│   │   ├── LoginView.xaml
│   │   ├── LoginView.xaml.cs
│   │   ├── MainView.xaml
│   │   ├── MainView.xaml.cs
│   │   ├── MergeDuplicatesView.xaml
│   │   ├── MergeDuplicatesView.xaml.cs
│   │   ├── RegisterView.xaml
│   │   ├── RegisterView.xaml.cs
│   │   ├── SettingsView.xaml
│   │   ├── SettingsView.xaml.cs
│   │   ├── TrashContactListView.xaml
│   │   └── TrashContactListView.xaml.cs
│
└── README.md                              # Project documentation

```

## 👤 Author

- **Qwiaulz** – [GitHub profile](https://github.com/Qwiaulz)

---

*© 2025 Contact Manager. All rights reserved.*