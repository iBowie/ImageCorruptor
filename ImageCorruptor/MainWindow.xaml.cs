using System.Windows;

namespace ImageCorruptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.ViewModel = new MainWindowViewModel(this);

            this.DataContext = this.ViewModel;
        }

        public MainWindowViewModel ViewModel { get; }
    }
}
