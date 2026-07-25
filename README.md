This is Copier Application.

# I made this because I wanted to copy in coding test ngl :)

So search the algorithm on another tab using search and copy the algorithm. :) :) :)

Now I am using it to store links... notes... code... everything!

This is built entirely by C# and Avalonia

## So this is what is included:

## 🧠 Core Features

### ✅ Entry Management

- **Add Entries**: User can enter a title and text snippet.
- **Copy Button**: Copies the text content to the clipboard.
- **Edit Button**: Toggle between read-only and editable modes for each entry.
- **Remove Button**: Deletes the entry from the UI.
- **Clear All Button**: Deletes all entries with one click.

---

## 🔎 Search & Filtering

- **Search Bar**: Real-time filtering of entries based on the title text.

---

## 💾 Persistence

- **AutoSave on Close**: Entries are automatically saved to disk (in JSON format).
- **AutoLoad on Start**: Automatically loads saved entries from the last session.

---

## 📤 Data Export/Import

- **Export to JSON**: Saves all entries to a user-selected `.json` file.
- **Import from JSON**: Loads entries from a selected `.json` file and replaces current entries.

---

## 🖥️ Platform Integration

- **Desktop Shortcut Creation** (🔁 One-Time):
  - Windows: `.bat` launcher on desktop
  - Linux: `.desktop` file
  - macOS: Symlink on desktop
  - ✅ Skips creation if already done
  - ✅ Fully cross-platform, no external libraries required

---

## ✅ Tech Stack

- **Avalonia UI (XAML/C#)** for the cross-platform UI.
- **.NET 6/7/8** depending on your project.
- Uses only built-in libraries (safe, no external dependencies except Avalonia).

---

## Images

![Change Password](https://github.com/UJKC/Copier/blob/No-Comments-Branch/images/Change%20Password%20Screen.png)
![Entry Password](https://github.com/UJKC/Copier/blob/No-Comments-Branch/images/Entry%20Password%20Screen.png)
![Home Screen](https://github.com/UJKC/Copier/blob/No-Comments-Branch/images/Home%20Screen.png)
![New Screen](https://github.com/UJKC/Copier/blob/No-Comments-Branch/images/New%20Screen.png)
![Search Screen](https://github.com/UJKC/Copier/blob/No-Comments-Branch/images/Search%20Screen.png)

---

## 📌 Optional Next Steps (When You're Ready)

These aren't built-in yet but would be great next steps:

| Feature               | Description                                         |
| --------------------- | --------------------------------------------------- |
| 🔁 Undo/Redo          | Let users undo accidental edits or deletions        |
| 🌙 Dark Mode Toggle   | User theme switch between light/dark                |
| 🏷️ Categories or Tags | Group entries for better organization               |
| ⭐ Favorite/Pin       | Keep important entries at the top                   |
| ☁️ Cloud Sync         | Sync entries using a service like Dropbox, OneDrive |
| 📦 Installer          | MSIX (Windows), AppImage (Linux), PKG (macOS)       |
| 📦 Version check      | Make sure new versions are automatically downloaded |
