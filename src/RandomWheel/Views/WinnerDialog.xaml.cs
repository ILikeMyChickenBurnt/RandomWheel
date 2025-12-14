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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window lower on screen (25% down from center)
            if (Owner != null)
            {
                var ownerCenter = Owner.Top + (Owner.Height / 2);
                var offset = Owner.Height * 0.125; // 12.5% of owner height as additional offset
                Top = ownerCenter - (Height / 2) + offset;
            }
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
