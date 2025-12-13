This is a local random selection tool where users manage multiple lists of items (e.g., names, options, or entries), persist them to JSON files, and perform random selections via a spinning wheel animation. The wheel shows only unmarked items and dynamically adjusts based on the number of items. After a spin, users can manually mark the selected item to hide it from future spins (but it remains in the list). Include an audit log for all actions. The app is Windows-only, with a modern, intuitive UI that's easy on the eyes (use a theme like MaterialDesignInXaml if needed for polish).
Functional Requirements

Multi-List Management:
Users can create new lists, name them, save, load, and delete them.
Display a list selector (e.g., ComboBox or ListBox) to switch between lists.
For the current list: Add, edit, or remove items (items are strings).
Support bulk input (e.g., paste comma-separated values into a text box to add multiple items at once).
Each item has a "marked as selected" flag (bool): Users can toggle this manually via checkbox or button.
Marked items are hidden from the wheel but remain in the list (filter unmarked for spins).

Import/Export:
Import lists from CSV files (e.g., one column for items; support quoted/escaped values).
Export current list to CSV.
Use OpenFileDialog and SaveFileDialog for file selection.

Persistence:
Store all lists in JSON files (e.g., one master JSON with all lists or per-list files) in a local directory like Environment.SpecialFolder.ApplicationData + "/RandomWheel/".
Automatically save changes (e.g., on add/remove/edit/mark/spin).
Load lists on app startup; default to a new empty list if none exist.
No limits on list size or number of lists, but handle large lists gracefully (e.g., UI warnings for 1000+ items).

Audit Trail Logging:
Log all actions: Item additions, edits, removals, markings, list creations/deletions, and spins (with selected item).
Each log entry: Timestamp, action type, list name, details (e.g., "Added item: 'John Doe'").
Store in a separate JSON or plain text file in the same app data folder (append-only).
Provide a UI button to view/export the log (e.g., open in Notepad or display in a TextBlock).

Random Selection:
Use cryptographic RNG for true randomness (System.Security.Cryptography.RandomNumberGenerator.GetInt32).
Select from unmarked items only; allow repeats unless user marks them.
Log each spin result.

Spinning Wheel UI:
Custom WPF UserControl for the wheel: Circular layout with dynamic segments (use Path or Canvas to draw pie slices based on unmarked item count).
Each segment displays an item name (auto-adjust font size/segment width for readability with many items).
Animation: Smooth rotation (Storyboard with DoubleAnimation on RotateTransform), slowdown, and stop on the randomly selected item.
Highlight winner (e.g., glow, arrow pointer).
Trigger via "Spin" button; after spin, show popup/message with winner and option to mark as selected.
If list is empty or all items marked: Hide wheel, disable spin, show warning message.
Basic visuals: Functional and appealing (e.g., colorful segments, no heavy customization).

Core Workflow and UI:
Main Window: Split into sections – list selector, item list (DataGrid or ListView with columns for name, marked checkbox, edit/remove buttons), add/bulk add controls, import/export buttons, spin button, log view button.
Spin mode: Switch to a view showing the wheel, or overlay it.
Edge cases: Handle single-item lists (minimal spin), very large lists (optimize rendering, e.g., virtualize item list).


Non-Functional Requirements

Platform and Deployment:
Windows-only; target win-x64.
Self-contained single-file exe: Use dotnet publish with --self-contained true -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true.
Optional: Create an MSI installer using Visual Studio Setup Project for desktop shortcut and uninstaller (idiot-proof installation).
No network dependencies; all local.

Performance and Usability:
Smooth animations (target 60 FPS).
Handle up to 500 items without lag; warn for larger.
Intuitive UI: Modern theme, tooltips, keyboard navigation, error messages (e.g., for file issues).
Accessibility: Basic support (e.g., high contrast, screen reader-friendly labels).

Security/Privacy:
Use crypto RNG for fairness.
All data local; no external access.

Testing:
Unit tests (xUnit): For RNG, persistence, logging, CSV handling.
Integration/manual tests: Full workflows, edge cases (empty lists, large data).

Libraries and Dependencies:
Core: System.Text.Json for JSON, System.Security.Cryptography for RNG.
CSV: CsvHelper NuGet package.
UI Theme (optional): MaterialDesignInXamlToolkit for polish.
Minimize externals; bundle all in the exe.


Project Structure Suggestions

Solution: RandomWheel.sln
Project: RandomWheel.csproj (WPF App)
Folders:
Models: Classes like NamedList (name, ObservableCollection<ListItem>), ListItem (string Name, bool IsMarked).
ViewModels: MainViewModel (handles lists, commands for CRUD/mark/spin/log), WheelViewModel.
Views: MainWindow.xaml, WheelControl.xaml.
Services: PersistenceService (JSON save/load), LoggingService, RandomService.
Tests: Separate test project.

Use Commands for actions (e.g., RelayCommand).
Bind UI to ViewModels via DataContext.

Development Phases

Setup: Create project, configure self-contained publish, add NuGets.
Persistence and Models: Implement JSON save/load, logging.
List Management: UI for lists/items, CRUD, marking, import/export.
Random Logic: Crypto RNG, selection function.
Wheel: Custom control, animation, integration with spin.
Polish: Themes, errors, tests.
Deploy: Build exe/MSI, README with instructions.

Generate a detailed step-by-step implementation plan, including code snippets for key parts (e.g., wheel animation, RNG), and ensure the project is extensible for future features like sounds or themes. Use GitHub Copilot for coding assistance during implementation.