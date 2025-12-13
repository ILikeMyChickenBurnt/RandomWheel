using RandomWheel.Models;
using RandomWheel.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RandomWheel.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PersistenceService _persistence = new();
        private readonly LoggingService _logging = new();
        private readonly RandomService _random = new();
        private readonly CsvService _csv = new();

        private NamedList? _currentList;

        public ObservableCollection<NamedList> Lists { get; } = new();
        
        public NamedList? CurrentList
        {
            get => _currentList;
            set
            {
                if (_currentList != value)
                {
                    if (_currentList != null)
                    {
                        _currentList.Items.CollectionChanged -= OnCurrentListItemsChanged;
                    }
                    
                    _currentList = value;
                    
                    if (_currentList != null)
                    {
                        _currentList.Items.CollectionChanged += OnCurrentListItemsChanged;
                    }
                    
                    OnPropertyChanged();
                }
            }
        }

        public ICommand NewListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand BulkAddCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ToggleMarkCommand { get; }
        public ICommand SpinCommand { get; }
        public ICommand ImportCsvCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ViewLogCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            NewListCommand = new RelayCommand(_ => NewList());
            DeleteListCommand = new RelayCommand(_ => DeleteCurrentList(), _ => CurrentList != null);
            AddItemCommand = new RelayCommand(name => 
            {
                AddItem(name as string ?? string.Empty);
                OnPropertyChanged(nameof(CurrentList)); // Trigger update
            });
            BulkAddCommand = new RelayCommand(text => 
            {
                BulkAdd(text as string ?? string.Empty);
                OnPropertyChanged(nameof(CurrentList)); // Trigger update
            });
            RemoveItemCommand = new RelayCommand(item => RemoveItem(item as ListItem), item => item is ListItem);
            ToggleMarkCommand = new RelayCommand(item => { if (item is ListItem li) ToggleMark(li); }, item => item is ListItem);
            SpinCommand = new RelayCommand(_ => RequestSpin(), _ => CurrentList?.Items.Any(i => !i.IsMarked) == true);
            ImportCsvCommand = new RelayCommand(path => ImportCsv(path as string ?? string.Empty));
            ExportCsvCommand = new RelayCommand(path => ExportCsv(path as string ?? string.Empty), _ => CurrentList != null);
            ViewLogCommand = new RelayCommand(_ => ViewLog());

            Load();
        }

        private void OnCurrentListItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(CurrentList));
        }

        private void Load()
        {
            foreach (var list in _persistence.Load()) 
                Lists.Add(list);
            
            if (Lists.Count == 0)
            {
                NewList();
            }
            else
            {
                CurrentList = Lists.FirstOrDefault();
            }
        }

        public void NewList()
        {
            var nl = new NamedList { Name = $"List {Lists.Count + 1}" };
            Lists.Add(nl);
            CurrentList = nl;
            _logging.Log("CreateList", nl.Name, "Created new list");
            Save();
        }

        public void DeleteCurrentList()
        {
            if (CurrentList == null) return;
            var name = CurrentList.Name;
            Lists.Remove(CurrentList);
            CurrentList = Lists.FirstOrDefault();
            _logging.Log("DeleteList", name, "Deleted list");
            Save();
        }

        public void AddItem(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || CurrentList == null) return;
            CurrentList.Items.Add(new ListItem { Name = name.Trim(), IsMarked = false });
            _logging.Log("AddItem", CurrentList.Name, $"Added item: '{name.Trim()}'");
            Save();
        }

        public void BulkAdd(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || CurrentList == null) return;
            var parts = text.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrWhiteSpace(p));
            foreach (var p in parts) 
                CurrentList.Items.Add(new ListItem { Name = p });
            _logging.Log("BulkAdd", CurrentList.Name, $"Bulk added {parts.Count()} items");
            Save();
        }

        public void RemoveItem(ListItem? item)
        {
            if (item == null || CurrentList == null) return;
            CurrentList.Items.Remove(item);
            _logging.Log("RemoveItem", CurrentList.Name, $"Removed item: '{item.Name}'");
            Save();
        }

        public void ToggleMark(ListItem item)
        {
            item.IsMarked = !item.IsMarked;
            _logging.Log("MarkItem", CurrentList?.Name ?? "", $"Marked={item.IsMarked}: '{item.Name}'");
            Save();
        }

        public void RequestSpin()
        {
            // Called by the spin button; MainWindow will execute the actual spin animation.
        }

        public int ExecuteSpin()
        {
            if (CurrentList == null) return -1;
            var candidates = CurrentList.Items.Where(i => !i.IsMarked).ToList();
            if (candidates.Count == 0) return -1;
            var idx = _random.NextIndex(candidates.Count);
            var winner = candidates[idx];
            _logging.Log("Spin", CurrentList.Name, $"Winner: '{winner.Name}'");
            return idx;
        }

        public void MarkWinner(int winnerIndex)
        {
            if (CurrentList == null) return;
            var candidates = CurrentList.Items.Where(i => !i.IsMarked).ToList();
            if (winnerIndex >= 0 && winnerIndex < candidates.Count)
            {
                candidates[winnerIndex].IsMarked = true;
                _logging.Log("MarkWinnerAfterSpin", CurrentList.Name, $"Marked winner: '{candidates[winnerIndex].Name}'");
                Save();
                OnPropertyChanged(nameof(CurrentList));
            }
        }

        public void ImportCsv(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || CurrentList == null || !File.Exists(path)) return;
            foreach (var item in _csv.ReadItems(path)) 
                CurrentList.Items.Add(new ListItem { Name = item });
            _logging.Log("ImportCSV", CurrentList.Name, $"Imported from '{Path.GetFileName(path)}'");
            Save();
            OnPropertyChanged(nameof(CurrentList));
        }

        public void ExportCsv(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || CurrentList == null) return;
            _csv.WriteItems(path, CurrentList.Items.Select(i => i.Name));
            _logging.Log("ExportCSV", CurrentList.Name, $"Exported to '{Path.GetFileName(path)}'");
        }

        public void ViewLog()
        {
            Paths.EnsureAppFolder();
            if (!File.Exists(Paths.AuditLogFile)) 
                File.WriteAllText(Paths.AuditLogFile, string.Empty);
            
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{Paths.AuditLogFile}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public void Save()
        {
            _persistence.Save(Lists);
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
