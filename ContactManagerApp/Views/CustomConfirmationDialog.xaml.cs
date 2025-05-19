using System.Windows;
using System.Windows.Controls;

namespace ContactManagerApp.Views
{
    public partial class CustomConfirmationDialog : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CustomConfirmationDialog), new PropertyMetadata(""));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(CustomConfirmationDialog), new PropertyMetadata(""));

        public static readonly DependencyProperty ConfirmButtonTextProperty =
            DependencyProperty.Register("ConfirmButtonText", typeof(string), typeof(CustomConfirmationDialog), new PropertyMetadata(""));

        public static readonly DependencyProperty CancelButtonTextProperty =
            DependencyProperty.Register("CancelButtonText", typeof(string), typeof(CustomConfirmationDialog), new PropertyMetadata(""));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public string ConfirmButtonText
        {
            get => (string)GetValue(ConfirmButtonTextProperty);
            set => SetValue(ConfirmButtonTextProperty, value);
        }

        public string CancelButtonText
        {
            get => (string)GetValue(CancelButtonTextProperty);
            set => SetValue(CancelButtonTextProperty, value);
        }

        public event EventHandler<bool> DialogResult;

        public CustomConfirmationDialog()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                TitleText.Text = Title;
                MessageText.Text = Message;
                CancelButton.Content = CancelButtonText;
                ConfirmButton.Content = ConfirmButtonText;
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult?.Invoke(this, false);
            CloseDialog();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult?.Invoke(this, true);
            CloseDialog();
        }

        private void CloseDialog()
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }
    }
}