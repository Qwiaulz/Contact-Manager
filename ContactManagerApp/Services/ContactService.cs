using ContactManagerApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ContactManagerApp.Services
{
    public class ContactService
    {
        private readonly string _basePath;
        private readonly string _userContactFolder;
        private readonly string _contactsFilePath;
        private readonly List<Contact> _contacts;
        public event EventHandler ContactsChanged;

        public ContactService(string userContactFolderCode)
        {
            _basePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data");
            _userContactFolder = Path.Combine(_basePath, "Contacts", $"user_{userContactFolderCode}");
            _contactsFilePath = Path.Combine(_userContactFolder, "contacts.json");
            _contacts = new List<Contact>();

            // Створюємо папку для контактів користувача, якщо вона не існує
            if (!Directory.Exists(_userContactFolder))
            {
                Directory.CreateDirectory(_userContactFolder);
            }

            LoadContacts();
        }

        private void LoadContacts()
        {
            try
            {
                if (File.Exists(_contactsFilePath))
                {
                    string json = File.ReadAllText(_contactsFilePath);
                    _contacts.Clear();
                    _contacts.AddRange(JsonSerializer.Deserialize<List<Contact>>(json));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading contacts: {ex.Message}");
            }
        }

        private void SaveContacts()
        {
            try
            {
                string json = JsonSerializer.Serialize(_contacts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_contactsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving contacts: {ex.Message}");
            }
        }

        public List<Contact> GetAllContacts()
        {
            return _contacts.OrderBy(c => c.Name).ToList();
        }

        public void AddContact(Contact contact)
        {
            contact.Id = _contacts.Any() ? _contacts.Max(c => c.Id) + 1 : 1;
            contact.CreatedDate = DateTime.Now;
            contact.UpdatedDate = DateTime.Now;
            _contacts.Add(contact);
            SaveContacts();
            NotifyContactsChanged();
        }

        public void UpdateContact(Contact contact)
        {
            var existingContact = _contacts.FirstOrDefault(c => c.Id == contact.Id);
            if (existingContact != null)
            {
                existingContact.Name = contact.Name;
                existingContact.Emails = contact.Emails;
                existingContact.Phones = contact.Phones;
                existingContact.Address = contact.Address;
                existingContact.Relationship = contact.Relationship;
                existingContact.Notes = contact.Notes;
                existingContact.IsFavourite = contact.IsFavourite;
                existingContact.Photo = contact.Photo;
                existingContact.IsDeleted = contact.IsDeleted;
                existingContact.DeletionDate = contact.DeletionDate;
                existingContact.UpdatedDate = DateTime.Now;
                SaveContacts();
                NotifyContactsChanged();
            }
        }

        public void MarkAsDeleted(Contact contact)
        {
            var existingContact = _contacts.FirstOrDefault(c => c.Id == contact.Id);
            if (existingContact != null && !existingContact.IsDeleted)
            {
                existingContact.IsDeleted = true;
                existingContact.DeletionDate = DateTime.Now;
                SaveContacts();
                NotifyContactsChanged();
            }
        }

        public void DeleteContact(Contact contact)
        {
            var existingContact = _contacts.FirstOrDefault(c => c.Id == contact.Id);
            if (existingContact != null)
            {
                if (!string.IsNullOrEmpty(existingContact.Photo) && !existingContact.IsPhotoDefault)
                {
                    string photoPath = Path.Combine(_basePath, existingContact.Photo);
                    if (File.Exists(photoPath))
                    {
                        File.Delete(photoPath);
                    }
                }
                _contacts.Remove(existingContact);
                SaveContacts();
                NotifyContactsChanged();
            }
        }

        public List<List<Contact>> FindDuplicates()
        {
            var duplicates = new List<List<Contact>>();
            var processed = new HashSet<int>();

            foreach (var contact in _contacts)
            {
                if (processed.Contains(contact.Id) || contact.IsDeleted) continue;

                var duplicateGroup = new List<Contact> { contact };
                foreach (var otherContact in _contacts)
                {
                    if (otherContact.Id == contact.Id || processed.Contains(otherContact.Id) || otherContact.IsDeleted) continue;

                    if (IsDuplicate(contact, otherContact))
                    {
                        duplicateGroup.Add(otherContact);
                        processed.Add(otherContact.Id);
                    }
                }

                if (duplicateGroup.Count > 1)
                {
                    duplicates.Add(duplicateGroup);
                }
                processed.Add(contact.Id);
            }

            return duplicates;
        }

        private bool IsDuplicate(Contact contact1, Contact contact2)
        {
            bool sameName = !string.IsNullOrEmpty(contact1.Name) && !string.IsNullOrEmpty(contact2.Name) &&
                            contact1.Name.Equals(contact2.Name, StringComparison.OrdinalIgnoreCase);

            bool samePhone = contact1.Phones.Any(p1 => !string.IsNullOrEmpty(p1.Number) &&
                              contact2.Phones.Any(p2 => !string.IsNullOrEmpty(p2.Number) && p2.Number == p1.Number));

            bool sameEmail = contact1.Emails.Any(e1 => !string.IsNullOrEmpty(e1.Address) &&
                              contact2.Emails.Any(e2 => !string.IsNullOrEmpty(e2.Address) && e2.Address == e1.Address));

            return sameName || samePhone || sameEmail;
        }

        public void MergeContacts(List<Contact> contacts)
        {
            if (contacts == null || contacts.Count < 2) return;

            var primaryContact = contacts.OrderByDescending(c => c.UpdatedDate).First();
            foreach (var contact in contacts)
            {
                if (contact.Id == primaryContact.Id) continue;

                foreach (var phone in contact.Phones)
                {
                    if (!primaryContact.Phones.Any(p => p.Number == phone.Number && p.Type == phone.Type))
                    {
                        primaryContact.Phones.Add(phone);
                    }
                }
                foreach (var email in contact.Emails)
                {
                    if (!primaryContact.Emails.Any(e => e.Address == email.Address && e.Type == email.Type))
                    {
                        primaryContact.Emails.Add(email);
                    }
                }
                if (string.IsNullOrEmpty(primaryContact.Address) && !string.IsNullOrEmpty(contact.Address))
                {
                    primaryContact.Address = contact.Address;
                }
                if (string.IsNullOrEmpty(primaryContact.Notes) && !string.IsNullOrEmpty(contact.Notes))
                {
                    primaryContact.Notes = contact.Notes;
                }
                if (string.IsNullOrEmpty(primaryContact.Photo) && !string.IsNullOrEmpty(contact.Photo))
                {
                    primaryContact.Photo = contact.Photo;
                }
                if (string.IsNullOrEmpty(primaryContact.Relationship) && !string.IsNullOrEmpty(contact.Relationship))
                {
                    primaryContact.Relationship = contact.Relationship;
                }
                primaryContact.IsFavourite |= contact.IsFavourite;
                DeleteContact(contact);
            }

            primaryContact.UpdatedDate = DateTime.Now;
            UpdateContact(primaryContact);
        }

        public void NotifyContactsChanged()
        {
            ContactsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}