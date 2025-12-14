# RandomWheel

A fun and fair random selection tool with a beautiful spinning wheel animation. Perfect for raffles, giveaways, choosing what to eat for dinner, or any situation where you need to pick something randomly!

---

## Download

Download the latest version from the [Releases](../../releases) page.

1. Download `RandomWheel-vX.X.X-win-x64.zip`
2. Extract the zip file
3. Run `RandomWheel.exe`

**Note:** The application is self-contained and does not require .NET to be installed.

**Requirements:** Windows 10/11 (64-bit)

---

## Features

### Fair Random Selection
Every item has an equal chance of being selected. The winner is chosen using a cryptographically secure random number generator — the same technology used in security applications — ensuring truly unbiased results.

### Dramatic Spinning Wheel
Watch the colorful wheel spin with a suspenseful animation that gradually slows to a crawl before landing on the winner. Features include:
- **Zoom effect** — The wheel zooms in as it slows down, focusing on the pointer area
- **Edge landing** — The wheel lands near the edge of segments for maximum suspense
- **Confetti celebration** — Colorful confetti falls across the screen when a winner is chosen
- **Winner sound** — A customizable sound plays when the winner is revealed

### Winner Celebration
When the wheel stops, a beautiful styled dialog announces the winner with:
- Large, prominent winner name display
- Festive confetti animation
- Custom victory sound (customizable)
- Option to mark the winner as selected or keep them in the pool

### Shuffle
Shuffle your list to randomize the visual order of items on the wheel. This is purely cosmetic but great for mixing things up before a spin.

### Multiple Lists
Create and manage multiple lists for different purposes:
- "Lunch Options" for deciding where to eat
- "Team Members" for assigning tasks
- "Movie Night" for picking what to watch
- ...and anything else you can imagine!

### Track Selected Items
Mark items as "selected" to exclude them from future spins. Perfect for:
- Raffles where each person can only win once
- Going through a list without repeats
- Tracking what you've already tried

### Import & Export
- **Import CSV**: Quickly add items from a spreadsheet or text file
- **Export CSV**: Save your lists to share or backup

### Audit Log
Every spin is logged with timestamps, so you have a record of all selections made.

### Customizable Sound
Personalize the winner announcement with your own sound:
- **Options > Choose Custom Sound** — Select any .mp3, .wav, .wma, or .aac file
- **Options > Winner Sound Enabled** — Toggle the sound on or off
- **Options > Reset to Default Sound** — Restore the built-in sound

### Flexible Display
- Collapse the sidebar to give the wheel maximum screen space
- The wheel looks great on any screen size
- Adjacent segments never have the same color for easy visibility

---

## Getting Started

### Adding Items

1. **Single Item**: Type a name in the "Add Item" box and click "Add Item" (or press Enter)
2. **Multiple Items**: Paste a list into "Bulk Add" — items can be separated by commas, semicolons, or new lines
3. **From File**: Click **Options > Import CSV** to load items from a CSV file

### Spinning the Wheel

1. Click the big **SPIN** button
2. Watch the wheel spin and zoom in dramatically!
3. Enjoy the confetti celebration when it stops
4. Choose whether to mark the winner as "selected" (removes them from future spins) or keep them in

### Shuffling the Wheel

Click the **Shuffle** button to randomize the order of items on the wheel. This changes the visual layout but doesn't affect the fairness of spins.

### Managing Items

- **Edit**: Click on any item name in the list to edit it
- **Remove**: Click the "Remove" button next to any item
- **Mark/Unmark**: Check or uncheck the "Selected" checkbox to include or exclude items from spins

### Managing Lists

- **New List**: Create a fresh list for a new purpose
- **Rename**: Give your list a meaningful name
- **Delete**: Remove lists you no longer need
- **Switch**: Use the dropdown to switch between your lists

### Maximizing the Wheel

Click the **<** button between the sidebar and wheel to hide the sidebar. This gives the wheel more room to display, making it easier to read with many items. Click **>** to bring the sidebar back.

---

## How the Randomness Works

### The Spinning Animation is Just for Fun!

Here's a little secret: **the winner is decided the instant you click SPIN**, before the wheel even starts moving. The spinning animation is purely visual entertainment — it doesn't affect the outcome at all.

### True Randomness Under the Hood

RandomWheel uses `System.Security.Cryptography.RandomNumberGenerator` to select winners. This is the same cryptographic random number generator used for:
- Generating encryption keys
- Secure password generation
- Online gambling systems

Unlike simple random number generators (like rolling dice in code), cryptographic RNG:
- Cannot be predicted, even if you know previous results
- Draws from system entropy (mouse movements, timing variations, hardware noise)
- Is designed to be statistically uniform — every item truly has an equal chance

### Why the Animation?

The spinning wheel animation serves several purposes:
1. **Builds anticipation** — the suspense makes it more exciting!
2. **Feels fair** — watching a physical-style spin feels more trustworthy than instant results
3. **It's fun!** — who doesn't love watching a colorful wheel spin?

But rest assured: whether the animation shows 3 spins or 30, the same winner was chosen the moment you clicked the button.

---

## Data Storage

Your lists are automatically saved to:
```
%APPDATA%\RandomWheel\data.json
```

The audit log is saved to:
```
%APPDATA%\RandomWheel\audit.log
```

Your settings (including custom sound preferences) are saved to:
```
%APPDATA%\RandomWheel\settings.json
```

---

## Tips & Tricks

- **Quick Add**: Press **Enter** while typing in the Add Item box to quickly add items
- **Bulk Paste**: Copy a column from Excel or a list from anywhere and paste it into Bulk Add
- **Reset All**: If everyone's been selected, the spin button will offer to reset all items
- **Large Lists**: The wheel handles hundreds of items, automatically adjusting text size
- **Shuffle First**: Use Shuffle before spinning to mix up the visual order for variety
- **Custom Sounds**: Set a fun victory sound via Options > Choose Custom Sound

---

## Build & Run (For Developers)

```powershell
# From repo root
dotnet restore RandomWheel.sln
dotnet build RandomWheel.sln -c Release
dotnet run --project src/RandomWheel/RandomWheel.csproj -c Debug
```

### Publish (Self-contained single-file win-x64)

```powershell
dotnet publish src/RandomWheel/RandomWheel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### Run Tests

```powershell
dotnet test RandomWheel.sln -c Debug
```

---

## License

This project is open source. Feel free to use, modify, and share!
