using Microsoft.Win32;
using RandomWheel.Models;
using RandomWheel.ViewModels;
using RandomWheel.Services;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RandomWheel.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _vm;
        private readonly CsvService _csvService = new();
        private const int LargeListWarningThreshold = 500;
        private bool _sidebarVisible = true;

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
                UpdateSpinButtonState();
                
                // Subscribe to list changes
                _vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.CurrentList))
                    {
                        SubscribeToCurrentListItems();
                        UpdateWheelDisplay();
                        UpdateSpinButtonState();
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
                UpdateWheelDisplay();
                UpdateSpinButtonState();
            };

            // Subscribe to each item's PropertyChanged for IsMarked changes
            foreach (var item in _vm.CurrentList.Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }

            // Also subscribe when new items are added
            _vm.CurrentList.Items.CollectionChanged += (s, e) =>
            {
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
                UpdateSpinButtonState();
            }
        }

        private void UpdateSpinButtonState()
        {
            bool hasUnmarkedItems = _vm?.CurrentList?.Items.Any(i => !i.IsMarked) == true;
            SpinButton.IsEnabled = hasUnmarkedItems;
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
                MessageBox.Show($"An item named \"{itemName}\" already exists in this list.",
                    "Duplicate Item",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _vm.AddItem(itemName);
            AddBox.Clear();
            UpdateWheelDisplay();
            UpdateSpinButtonState();
        }

        private void BulkAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentList == null || string.IsNullOrWhiteSpace(BulkBox.Text))
                return;

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
            UpdateSpinButtonState();

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

                try
                {
                    _vm?.ImportCsv(ofd.FileName);
                    UpdateWheelDisplay();
                    UpdateSpinButtonState();
                    AddBox.Clear();
                    MessageBox.Show("Items imported successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing CSV:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ExecuteSpin()
        {
            if (_vm?.CurrentList == null)
            {
                MessageBox.Show("No list selected", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                    UpdateSpinButtonState();
                }
                return;
            }

            var winnerIdx = _vm.ExecuteSpin();
            if (winnerIdx >= 0)
            {
                Wheel.Spin(winnerIdx, OnSpinComplete);
            }
        }

        private void OnSpinComplete(int winnerIndex)
        {
            if (_vm?.CurrentList == null)
                return;

            var unmarkedItems = _vm.CurrentList.Items.Where(i => !i.IsMarked).ToList();
            if (winnerIndex >= 0 && winnerIndex < unmarkedItems.Count)
            {
                var winner = unmarkedItems[winnerIndex];
                var result = MessageBox.Show(
                    $"Winner: {winner.Name}\n\nMark as selected and hide from future spins?",
                    "Spin Result",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    _vm.MarkWinner(winnerIndex);
                    UpdateWheelDisplay();
                    UpdateSpinButtonState();
                }
                else
                {
                    Wheel.ResetRotation();
                }
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
                UpdateSpinButtonState();
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
