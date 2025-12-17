using Microsoft.Win32;
using RandomWheel.Models;
using RandomWheel.ViewModels;
using RandomWheel.Services;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RandomWheel.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _vm;
        private readonly CsvService _csvService = new();
        private readonly SoundService _soundService = new();
        private readonly SettingsService _settingsService = new();
        private const int LargeListWarningThreshold = 500;
        private bool _sidebarVisible = true;
        private bool _isShuffling = false;
        private bool _isSpinning = false;
        private bool _isBulkOperating = false;
        
        private static readonly Random _random = new();
        private static readonly Color[] ConfettiColors =
        {
            Color.FromRgb(255, 107, 107),  // Red
            Color.FromRgb(78, 205, 196),   // Teal
            Color.FromRgb(255, 230, 109),  // Yellow
            Color.FromRgb(170, 111, 255),  // Purple
            Color.FromRgb(255, 159, 243),  // Pink
            Color.FromRgb(46, 213, 115),   // Green
            Color.FromRgb(30, 144, 255),   // Blue
            Color.FromRgb(255, 165, 2),    // Orange
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _sidebarVisible = !_sidebarVisible;
            
            if (_sidebarVisible)
            {
                SidebarColumn.Width = new GridLength(400);
                SidebarPanel.Visibility = Visibility.Visible;
                SidebarToggleButton.Content = "<";
                SidebarToggleButton.ToolTip = "Hide sidebar";
            }
            else
            {
                SidebarColumn.Width = new GridLength(0);
                SidebarPanel.Visibility = Visibility.Collapsed;
                SidebarToggleButton.Content = ">";
                SidebarToggleButton.ToolTip = "Show sidebar";
            }

            // Re-render wheel after layout change
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateWheelDisplay();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _vm = (MainViewModel)DataContext;
            
            if (_vm != null)
            {
                // Initial render
                UpdateWheelDisplay();
                UpdateInteractiveControlsState();
                
                // Load branding logo if configured
                if (!string.IsNullOrEmpty(_settingsService.BrandingLogoPath) && 
                    File.Exists(_settingsService.BrandingLogoPath))
                {
                    Wheel.SetBrandingLogo(
                        _settingsService.BrandingLogoPath,
                        _settingsService.BrandingLogoOffsetX,
                        _settingsService.BrandingLogoOffsetY,
                        _settingsService.BrandingLogoScale > 0 ? _settingsService.BrandingLogoScale : 1.0);
                }

                // Subscribe to list changes
                _vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.CurrentList))
                    {
                        SubscribeToCurrentListItems();
                        UpdateWheelDisplay();
                        UpdateInteractiveControlsState();
                        CheckListSize();
                    }
                };

                // Subscribe to initial list items
                SubscribeToCurrentListItems();
            }
        }

        private void SubscribeToCurrentListItems()
        {
            if (_vm?.CurrentList == null) return;

            // Subscribe to collection changes
            _vm.CurrentList.Items.CollectionChanged += (s, e) =>
            {
                if (_isShuffling || _isBulkOperating) return; // Skip updates during bulk operations
                UpdateWheelDisplay();
                UpdateInteractiveControlsState();
            };

            // Subscribe to each item's PropertyChanged for IsMarked changes
            foreach (var item in _vm.CurrentList.Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }

            // Also subscribe when new items are added
            _vm.CurrentList.Items.CollectionChanged += (s, e) =>
            {
                if (_isShuffling || _isBulkOperating) return; // Skip updates during bulk operations
                if (e.NewItems != null)
                {
                    foreach (ListItem item in e.NewItems)
                    {
                        item.PropertyChanged += OnItemPropertyChanged;
                    }
                }
            };
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ListItem.IsMarked))
            {
                UpdateWheelDisplay();
                UpdateInteractiveControlsState();
            }
        }

        private void UpdateInteractiveControlsState()
        {
            bool isBusy = _isSpinning || _isShuffling || _isBulkOperating;
            bool hasCurrentList = _vm?.CurrentList != null;
            bool hasItems = hasCurrentList && _vm!.CurrentList!.Items.Count > 0;
            bool hasUnmarkedItems = hasCurrentList && _vm!.CurrentList!.Items.Any(i => !i.IsMarked);
            bool hasMultipleItems = hasCurrentList && _vm!.CurrentList!.Items.Count >= 2;

            // Top bar controls
            ListComboBox.IsEnabled = !isBusy;
            RenameButton.IsEnabled = !isBusy && hasCurrentList;
            NewListButton.IsEnabled = !isBusy;
            DeleteButton.IsEnabled = !isBusy && hasCurrentList;
            OptionsButton.IsEnabled = !isBusy;

            // Sidebar controls
            ItemsGrid.IsEnabled = !isBusy;
            AddBox.IsEnabled = !isBusy;
            AddItemButton.IsEnabled = !isBusy && hasCurrentList;
            BulkBox.IsEnabled = !isBusy;
            BulkAddButton.IsEnabled = !isBusy && hasCurrentList;

            // Wheel controls
            SpinButton.IsEnabled = !isBusy && hasUnmarkedItems;
            ShuffleButton.IsEnabled = !isBusy && hasMultipleItems;
        }

        private void UpdateWheelDisplay()
        {
            if (_vm?.CurrentList == null)
            {
                EmptyMessage.Visibility = Visibility.Visible;
                Wheel.Visibility = Visibility.Hidden;
                ItemCountInfo.Text = "";
                return;
            }

            var unmarkedCount = _vm.CurrentList.Items.Count(i => !i.IsMarked);
            var totalCount = _vm.CurrentList.Items.Count;
            
            if (unmarkedCount == 0)
            {
                EmptyMessage.Visibility = Visibility.Visible;
                Wheel.Visibility = Visibility.Hidden;
                if (totalCount == 0)
                    ItemCountInfo.Text = "Add items to get started";
                else
                    ItemCountInfo.Text = $"All {totalCount} items selected";
            }
            else
            {
                EmptyMessage.Visibility = Visibility.Hidden;
                Wheel.Visibility = Visibility.Visible;
                
                if (totalCount > unmarkedCount)
                    ItemCountInfo.Text = $"{unmarkedCount} of {totalCount} remaining";
                else
                    ItemCountInfo.Text = $"{unmarkedCount} item{(unmarkedCount != 1 ? "s" : "")} remaining";
                
                Wheel.Render(_vm.CurrentList.Items);
            }
        }

        private void CheckListSize()
        {
            if (_vm?.CurrentList == null)
                return;

            int itemCount = _vm.CurrentList.Items.Count;
            if (itemCount > LargeListWarningThreshold)
            {
                var response = MessageBox.Show(
                    $"This list has {itemCount} items. This might cause performance issues.\n\nContinue anyway?",
                    "Large List Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (response == MessageBoxResult.No)
                {
                    // Could implement list truncation or other handling here
                }
            }
        }

        private void OptionsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                // Update checkbox state before showing menu
                if (button.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "WinnerSoundEnabledMenuItem") is MenuItem soundMenuItem)
                {
                    soundMenuItem.IsChecked = _settingsService.WinnerSoundEnabled;
                }
                
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void RenameList_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null)
            {
                MessageBox.Show("No list selected to rename.", "Rename List", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string currentName = _vm.CurrentList.Name;
            string? newName = ShowInputDialog("Rename List", "Enter new name for the list:", currentName);
            
            if (newName != null && !string.IsNullOrWhiteSpace(newName) && newName != currentName)
            {
                // Check for duplicate list names
                if (_vm.Lists.Any(l => l.Id != _vm.CurrentList.Id && 
                    string.Equals(l.Name, newName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"A list named \"{newName}\" already exists.",
                        "Duplicate Name",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _vm.CurrentList.Name = newName.Trim();
                _vm.Save();
            }
        }

        private string? ShowInputDialog(string title, string prompt, string defaultValue)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(16) };
            var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
            var textBox = new TextBox { Text = defaultValue, Padding = new Thickness(8), FontSize = 14 };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            
            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancelButton = new Button { Content = "Cancel", Width = 80, IsCancel = true };

            string? result = null;
            okButton.Click += (s, e) => { result = textBox.Text; dialog.DialogResult = true; };
            cancelButton.Click += (s, e) => { dialog.DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(label);
            panel.Children.Add(textBox);
            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            textBox.SelectAll();
            textBox.Focus();

            return dialog.ShowDialog() == true ? result : null;
        }

        private void ItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Block editing during spin/shuffle operations
            if (_isSpinning || _isShuffling || _isBulkOperating)
            {
                e.Cancel = true;
                return;
            }
            
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            if (e.Column.Header?.ToString() == "Name" && e.EditingElement is TextBox textBox)
            {
                string newName = textBox.Text.Trim();
                var editedItem = e.Row.Item as ListItem;

                if (editedItem == null || _vm?.CurrentList == null)
                    return;

                // Check for duplicates (excluding the current item by ID)
                var duplicate = _vm.CurrentList.Items.FirstOrDefault(i => 
                    i.Id != editedItem.Id && 
                    string.Equals(i.Name, newName, StringComparison.OrdinalIgnoreCase));

                if (duplicate != null)
                {
                    e.Cancel = true;
                    MessageBox.Show($"An item named \"{newName}\" already exists in this list.",
                        "Duplicate Item",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    
                    // Reset the text to original
                    textBox.Text = editedItem.Name;
                }
                else
                {
                    // Update wheel display after edit
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _vm?.Save();
                        UpdateWheelDisplay();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void MarkedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Block marking during spin/shuffle operations
            if (_isSpinning || _isShuffling || _isBulkOperating)
            {
                // Revert the checkbox state
                if (sender is System.Windows.Controls.CheckBox checkBox)
                {
                    checkBox.IsChecked = !checkBox.IsChecked;
                }
                return;
            }
            
            // Wheel and button state will be updated via PropertyChanged subscription
            _vm?.Save();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null || string.IsNullOrWhiteSpace(AddBox.Text))
                return;

            string itemName = AddBox.Text.Trim();

            // Check for duplicates
            if (_vm.CurrentList.Items.Any(i => string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"An item named \"itemName\" already exists in this list.",
                    "Duplicate Item",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _vm.AddItem(itemName);
            AddBox.Clear();
            UpdateWheelDisplay();
            UpdateInteractiveControlsState();
        }

        private void BulkAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null || string.IsNullOrWhiteSpace(BulkBox.Text))
                return;

            if (_isBulkOperating || _isSpinning || _isShuffling)
                return;

            _isBulkOperating = true;
            UpdateInteractiveControlsState();

            try
            {
                var parts = BulkBox.Text.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                var duplicates = new System.Collections.Generic.List<string>();
                var added = 0;

                foreach (var part in parts)
                {
                    if (_vm.CurrentList.Items.Any(i => string.Equals(i.Name, part, StringComparison.OrdinalIgnoreCase)))
                    {
                        duplicates.Add(part);
                    }
                    else
                    {
                        _vm.AddItem(part);
                        added++;
                    }
                }

                BulkBox.Clear();
                UpdateWheelDisplay();

                if (duplicates.Count > 0)
                {
                    string duplicateList = string.Join(", ", duplicates.Take(5));
                    if (duplicates.Count > 5)
                        duplicateList += $", ... and {duplicates.Count - 5} more";

                    MessageBox.Show($"Added {added} item(s).\n\nSkipped {duplicates.Count} duplicate(s): {duplicateList}",
                        "Bulk Add Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            finally
            {
                _isBulkOperating = false;
                UpdateInteractiveControlsState();
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog 
            { 
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import List from CSV"
            };
            
            if (ofd.ShowDialog() == true)
            {
                // Validate before importing
                var (isValid, errorMessage) = _csvService.ValidateCsvFile(ofd.FileName);
                if (!isValid)
                {
                    MessageBox.Show($"Cannot import file:\n\n{errorMessage}", 
                        "Import Validation Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                _isBulkOperating = true;
                UpdateInteractiveControlsState();
                
                try
                {
                    _vm?.ImportCsv(ofd.FileName);
                    UpdateWheelDisplay();
                    UpdateInteractiveControlsState();
                    AddBox.Clear();
                    MessageBox.Show("Items imported successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing CSV:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _isBulkOperating = false;
                    UpdateInteractiveControlsState();
                }
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null || _vm.CurrentList.Items.Count == 0)
            {
                MessageBox.Show("No items to export. Add items to the list first.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog 
            { 
                Filter = "CSV files (*.csv)|*.csv", 
                FileName = SanitizeFilename(_vm.CurrentList.Name ?? "List") + ".csv",
                Title = "Export List to CSV"
            };
            
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _vm.ExportCsv(sfd.FileName);
                    MessageBox.Show("List exported successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting CSV:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WinnerSoundEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                _settingsService.WinnerSoundEnabled = menuItem.IsChecked;
            }
        }

        private void ChooseCustomSound_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Audio files (*.wav;*.mp3;*.wma;*.aac)|*.wav;*.mp3;*.wma;*.aac|All files (*.*)|*.*",
                Title = "Select Winner Sound"
            };

            if (ofd.ShowDialog() == true)
            {
                if (_soundService.IsValidSoundFile(ofd.FileName))
                {
                    _settingsService.CustomWinnerSoundPath = ofd.FileName;
                    
                    // Play a preview
                    _soundService.PlayWinnerSound(ofd.FileName);
                    
                    MessageBox.Show($"Custom sound set:\n{System.IO.Path.GetFileName(ofd.FileName)}", 
                        "Sound Updated", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Invalid audio file. Please select a .wav, .mp3, .wma, or .aac file.",
                        "Invalid File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void ChooseBrandingLogo_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*",
                Title = "Select Branding Logo"
            };

            if (ofd.ShowDialog() == true)
            {
                if (File.Exists(ofd.FileName))
                {
                    var previewWindow = new BrandingLogoPreviewWindow(
                        ofd.FileName,
                        _settingsService.BrandingLogoOffsetX,
                        _settingsService.BrandingLogoOffsetY,
                        _settingsService.BrandingLogoScale > 0 ? _settingsService.BrandingLogoScale : 1.0);
                    previewWindow.Owner = this;

                    if (previewWindow.ShowDialog() == true && previewWindow.Applied)
                    {
                        _settingsService.BrandingLogoPath = ofd.FileName;
                        _settingsService.BrandingLogoOffsetX = previewWindow.OffsetX;
                        _settingsService.BrandingLogoOffsetY = previewWindow.OffsetY;
                        _settingsService.BrandingLogoScale = previewWindow.Scale;
                        Wheel.SetBrandingLogo(ofd.FileName, previewWindow.OffsetX, previewWindow.OffsetY, previewWindow.Scale);
                    }
                }
            }
        }

        private void ClearBrandingLogo_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.BrandingLogoPath = null;
            _settingsService.BrandingLogoOffsetX = 0;
            _settingsService.BrandingLogoOffsetY = 0;
            _settingsService.BrandingLogoScale = 1.0;
            Wheel.ClearBrandingLogo();
        }

        private string SanitizeFilename(string filename)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                filename = filename.Replace(c, '_');
            return filename;
        }

        private void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSpin();
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null || _vm.CurrentList.Items.Count < 2)
                return;

            if (_isShuffling || _isSpinning || _isBulkOperating)
                return;

            _isShuffling = true;
            UpdateInteractiveControlsState();
            
            // Force UI to update before starting the shuffle
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            
            try
            {
                var items = _vm.CurrentList.Items;
                int n = items.Count;
                
                // Create shuffled list
                var itemList = items.ToList();
                var random = new Random();
                for (int i = n - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = itemList[i];
                    itemList[i] = itemList[j];
                    itemList[j] = temp;
                }
                
                // Temporarily clear binding to prevent cascading updates
                System.Windows.Data.BindingOperations.ClearBinding(ItemsGrid, DataGrid.ItemsSourceProperty);
                ItemsGrid.ItemsSource = null;
                
                // Clear without triggering repeated updates
                while (items.Count > 0)
                {
                    items.RemoveAt(items.Count - 1);
                }
                
                // Add all at once
                foreach (var item in itemList)
                {
                    items.Add(item);
                }
                
                // Restore the binding to CurrentList.Items
                var binding = new System.Windows.Data.Binding("CurrentList.Items");
                ItemsGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
                
                _vm.Save();
                UpdateWheelDisplay();
            }
            finally
            {
                _isShuffling = false;
                UpdateInteractiveControlsState();
            }
        }

        private void ExecuteSpin()
        {
            if (_vm?.CurrentList == null)
            {
                MessageBox.Show("No list selected", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isSpinning || _isShuffling)
                return;

            var unmarkedItems = _vm.CurrentList.Items.Where(i => !i.IsMarked).ToList();
            if (unmarkedItems.Count == 0)
            {
                var response = MessageBox.Show(
                    "No unselected items to spin.\n\nWould you like to reset all items?",
                    "No Items Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (response == MessageBoxResult.Yes)
                {
                    foreach (var item in _vm.CurrentList.Items)
                        item.IsMarked = false;
                    _vm.Save();
                    UpdateWheelDisplay();
                    UpdateInteractiveControlsState();
                }
                return;
            }

            _isSpinning = true;
            UpdateInteractiveControlsState();

            var winnerIdx = _vm.ExecuteSpin();
            if (winnerIdx >= 0)
            {
                Wheel.Spin(winnerIdx, OnSpinComplete);
            }
            else
            {
                _isSpinning = false;
                UpdateInteractiveControlsState();
            }
        }

        private void OnSpinComplete(int winnerIndex)
        {
            _isSpinning = false;
            
            if (_vm?.CurrentList == null)
            {
                UpdateInteractiveControlsState();
                return;
            }

            var unmarkedItems = _vm.CurrentList.Items.Where(i => !i.IsMarked).ToList();
            if (winnerIndex >= 0 && winnerIndex < unmarkedItems.Count)
            {
                var winner = unmarkedItems[winnerIndex];
                
                // Play winner sound
                if (_settingsService.WinnerSoundEnabled)
                {
                    _soundService.PlayWinnerSound(_settingsService.CustomWinnerSoundPath);
                }
                
                // Start confetti animation
                StartConfettiAnimation();
                
                var dialog = new WinnerDialog(winner.Name)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true && dialog.MarkAsSelected)
                {
                    _vm.MarkWinner(winnerIndex);
                    UpdateWheelDisplay();
                }
                else
                {
                    Wheel.ResetRotation();
                }
                
                // Stop sound and clear confetti after dialog closes
                _soundService.Stop();
                ConfettiCanvas.Children.Clear();
            }
            
            UpdateInteractiveControlsState();
        }
        
        private void StartConfettiAnimation()
        {
            ConfettiCanvas.Children.Clear();
            int confettiCount = 150;

            for (int i = 0; i < confettiCount; i++)
            {
                var confetti = CreateConfettiPiece();
                ConfettiCanvas.Children.Add(confetti);

                // Random starting position across the entire window width
                double startX = _random.NextDouble() * ActualWidth;
                double startY = -20 - (_random.NextDouble() * 200); // Start above the window

                Canvas.SetLeft(confetti, startX);
                Canvas.SetTop(confetti, startY);

                // Animate falling
                AnimateConfetti(confetti, startX, startY, i * 20); // Stagger start times
            }
        }

        private Shape CreateConfettiPiece()
        {
            var color = ConfettiColors[_random.Next(ConfettiColors.Length)];
            var shapeType = _random.Next(3);

            Shape shape;
            if (shapeType == 0)
            {
                // Rectangle
                shape = new Rectangle
                {
                    Width = 10 + _random.NextDouble() * 8,
                    Height = 14 + _random.NextDouble() * 10,
                    Fill = new SolidColorBrush(color),
                    RadiusX = 1,
                    RadiusY = 1
                };
            }
            else if (shapeType == 1)
            {
                // Circle
                double size = 8 + _random.NextDouble() * 8;
                shape = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color)
                };
            }
            else
            {
                // Small square
                double size = 8 + _random.NextDouble() * 6;
                shape = new Rectangle
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color)
                };
            }

            shape.RenderTransformOrigin = new Point(0.5, 0.5);
            shape.RenderTransform = new RotateTransform(_random.NextDouble() * 360);
            shape.Opacity = 0.9;

            return shape;
        }

        private void AnimateConfetti(Shape confetti, double startX, double startY, int delayMs)
        {
            var duration = TimeSpan.FromSeconds(3.0 + _random.NextDouble() * 2.0);
            var delay = TimeSpan.FromMilliseconds(delayMs);

            // Horizontal sway
            double swayAmount = 40 + _random.NextDouble() * 60;
            double swayDirection = _random.Next(2) == 0 ? 1 : -1;

            // Fall animation (Y position)
            var fallAnimation = new DoubleAnimation
            {
                From = startY,
                To = ActualHeight + 50,
                Duration = duration,
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // Sway animation (X position)
            var swayAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = duration,
                BeginTime = delay
            };
            swayAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(startX, KeyTime.FromPercent(0)));
            swayAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(startX + swayAmount * swayDirection, KeyTime.FromPercent(0.25)));
            swayAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(startX - swayAmount * swayDirection * 0.5, KeyTime.FromPercent(0.5)));
            swayAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(startX + swayAmount * swayDirection * 0.3, KeyTime.FromPercent(0.75)));
            swayAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(startX, KeyTime.FromPercent(1)));

            // Rotation animation
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360 * (_random.Next(2) == 0 ? 1 : -1) * (1 + _random.NextDouble()),
                Duration = duration,
                BeginTime = delay
            };

            confetti.BeginAnimation(Canvas.TopProperty, fallAnimation);
            confetti.BeginAnimation(Canvas.LeftProperty, swayAnimation);

            if (confetti.RenderTransform is RotateTransform rotateTransform)
            {
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            }
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null)
            {
                MessageBox.Show("No list selected to delete.", "Delete List", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string listName = _vm.CurrentList.Name;
            int itemCount = _vm.CurrentList.Items.Count;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the list \"{listName}\"?\n\nThis list contains {itemCount} item{(itemCount != 1 ? "s" : "")} and cannot be recovered.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                _vm.DeleteCurrentList();
                SubscribeToCurrentListItems();
                UpdateWheelDisplay();
                UpdateInteractiveControlsState();
            }
        }

        // Handle AddBox Enter key for quick add
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Return && AddBox.IsFocused)
            {
                AddItem_Click(AddBox, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}
