using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp3
{

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : ViewModelBase
    {
        public TabControlViewModel Tab { get; set; }

        public MainViewModel()
        {
            ButtonCommand = new RelayCommand(ExecuteButton);
        }

        public ICommand ButtonCommand { get; private set; }
        private void ExecuteButton(object parameter)
        {
        }
    }

    public class TabControlViewModel : ViewModelBase 
    {
        public ObservableCollection<TabItemViewModel> Pages { get; private set; }

        private TabItemViewModel _selectedPage;
        public TabItemViewModel SelectedPage
        {
            get => _selectedPage;
            set => SetProperty(ref _selectedPage, value);
        }

        public TabControlViewModel()
        {
            Pages = new ObservableCollection<TabItemViewModel>();

        }
    }

    public class TabItemViewModel : ViewModelBase
    {
        private static int _count = 0;
        private int _index;
        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        private SolidColorBrush _color;
        public SolidColorBrush Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public TabItemViewModel()
        {
            _count++;
            Index = _count;
            var random = new Random();
            int value = random.Next(2);

            switch (value)
            {
                case 0:
                    Color = Brushes.Red;
                    break;
                case 1:
                    Color = Brushes.Green;
                    break;
                case 2:
                    Color = Brushes.Yellow;
                    break;
                default:
                    break;
            }
        }
    }
}
