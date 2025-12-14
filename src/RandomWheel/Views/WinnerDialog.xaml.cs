using System.Windows;

namespace RandomWheel.Views
{
    public partial class WinnerDialog : Window
    {
        public bool MarkAsSelected { get; private set; }

        public WinnerDialog(string winnerName)
        {
            InitializeComponent();
            WinnerNameText.Text = winnerName;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            MarkAsSelected = true;
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            MarkAsSelected = false;
            DialogResult = false;
            Close();
        }
    }
}
