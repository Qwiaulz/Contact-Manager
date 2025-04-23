using System.Windows.Controls;

namespace ContactManagerApp.Services
{
    public class NavigationService
    {
        private readonly Frame _frame;

        public NavigationService(Frame frame)
        {
            _frame = frame;
        }

        public Frame GetFrame()
        {
            return _frame;
        }

        public void Navigate(Page page)
        {
            _frame.Navigate(page);
        }

        public void GoBack()
        {
            if (_frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }
    }
}