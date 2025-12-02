using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using copier.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace copier.Services
{
    // small state holder attached to each panel
    public class EntryPanelState
    {
        public bool IsPinned { get; set; } = false;
    }

    public class EntryManager
    {
        public EntryManager(List<StackPanel> allEntryPanels)
        {
            this.Panels = allEntryPanels;
        }

        // Shared external list (MainWindow passes its allEntryPanels here)
        public List<StackPanel> Panels { get; }

        /// <summary>
        /// Create a UI StackPanel representing an entry.
        /// The created panel is added to Panels list by the caller when needed.
        /// </summary>
        public StackPanel CreateEntryPanel(
            string title,
            string text,
            Window window)
        {
            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = Avalonia.Media.FontWeight.Bold
            };

            var editableText = new TextBox
            {
                Text = text,
                AcceptsReturn = true,
                Width = 400,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.LightGray,
                Foreground = Brushes.Black,
                BorderBrush = Brushes.Transparent,
                IsReadOnly = true,
                Focusable = false,
                IsHitTestVisible = false
            };

            var copyButton = new Button { Content = "Copy", Width = 60 };
            var editButton = new Button { Content = "Edit", Width = 60 };
            var pinButton = new Button { Content = "Pin", Width = 60 };
            var removeButton = new Button { Content = "Remove", Width = 60 };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(pinButton);
            buttonPanel.Children.Add(removeButton);

            var entryPanel = new StackPanel { Spacing = 5 };
            entryPanel.Children.Add(titleText);
            entryPanel.Children.Add(editableText);
            entryPanel.Children.Add(buttonPanel);

            // attach state object so we can persist pin status
            entryPanel.Tag = new EntryPanelState { IsPinned = false };

            // copy event
            copyButton.Click += async (_, _) =>
            {
                var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(editableText.Text ?? "");
            };

            // remove: store panel reference in button tag so MainWindow can remove it
            removeButton.Tag = entryPanel;

            // edit toggle
            editButton.Click += (_, _) =>
            {
                if (editableText.IsReadOnly)
                {
                    // enable editing
                    editableText.IsReadOnly = false;
                    editableText.Focusable = true;
                    editableText.IsHitTestVisible = true;
                    editableText.Background = Brushes.White;
                    editableText.Foreground = Brushes.Black;

                    // put caret at the end so user can continue typing
                    editableText.CaretIndex = (editableText.Text ?? "").Length;

                    // focus
                    editableText.Focus();

                    editButton.Content = "Save";
                }
                else
                {
                    // disable editing (save state is the TextBox.Text)
                    editableText.IsReadOnly = true;
                    editableText.Focusable = false;
                    editableText.IsHitTestVisible = false;
                    editableText.Background = Brushes.LightGray;
                    editableText.Foreground = Brushes.Black;

                    editButton.Content = "Edit";
                }
            };

            // pin toggle: toggles the EntryPanelState and updates button text
            pinButton.Click += (_, _) =>
            {
                if (entryPanel.Tag is not EntryPanelState state)
                    return;

                state.IsPinned = !state.IsPinned;
                pinButton.Content = state.IsPinned ? "Unpin" : "Pin";

                // Reorder the UI - caller must provide the containing Panel to ReorderPanels.
                // We'll attempt to find parent Panel from the entryPanel's Parent chain.
                var parent = entryPanel.Parent as Panel;
                if (parent != null)
                {
                    ReorderPanels(parent);
                }
            };

            // Add to internal list (Panels). Caller may also choose to add it to UI container.
            Panels.Add(entryPanel);
            return entryPanel;
        }

        /// <summary>
        /// Convert current UI panels into EntryData list (used for saving/export).
        /// </summary>
        public List<EntryData> ToEntryList() =>
            Panels.Select(panel =>
            {
                var state = panel.Tag as EntryPanelState;
                return new EntryData
                {
                    Title = (panel.Children[0] as TextBlock)?.Text ?? "",
                    Text = (panel.Children[1] as TextBox)?.Text ?? "",
                    IsPinned = state?.IsPinned ?? false
                };
            }).ToList();

        /// <summary>
        /// Load panels from data into a given itemsPanel (visual container) and window (used for clipboard).
        /// This clears existing Panels and itemsPanel.Children and recreates panels from entries.
        /// Pinned items will be sorted to the top (Option A behavior).
        /// </summary>
        public void LoadPanels(IEnumerable<EntryData> entries, Window window, Panel itemsPanel)
        {
            Panels.Clear();
            itemsPanel.Children.Clear();

            // Create panels
            foreach (var entry in entries)
            {
                var panel = CreateEntryPanel(entry.Title, entry.Text, window);

                // restore pinned state
                if (panel.Tag is EntryPanelState state)
                {
                    state.IsPinned = entry.IsPinned;

                    // update pin button label (button is in panel.Children[2] StackPanel)
                    if (panel.Children[2] is StackPanel sp)
                    {
                        // find the pin button
                        var pinBtn = sp.Children.OfType<Button>().FirstOrDefault(b =>
                            (b.Content?.ToString() == "Pin" || b.Content?.ToString() == "Unpin"));
                        if (pinBtn != null)
                            pinBtn.Content = state.IsPinned ? "Unpin" : "Pin";
                    }
                }

                itemsPanel.Children.Add(panel);
            }

            // Ensure pinned items are shown first
            ReorderPanels(itemsPanel);
        }

        /// <summary>
        /// Reorders Panels (in-memory list) and the visual children of itemsPanel so pinned items appear first.
        /// Pinned groups keep their relative order among themselves, and unpinned keep theirs.
        /// </summary>
        public void ReorderPanels(Panel itemsPanel)
        {
            if (Panels.Count == 0) return;
            if (itemsPanel == null) return;

            // Build a list of panels that are currently children of itemsPanel in their current visual order
            // to preserve relative ordering within pinned/unpinned groups.
            var visualOrdered = itemsPanel.Children.OfType<StackPanel>().ToList();

            // Ensure our internal Panels list is consistent with visualOrdered.
            // We'll reconstruct Panels to match visual order first (but keep only those present).
            var newPanelsOrder = new List<StackPanel>();
            foreach (var p in visualOrdered)
            {
                if (Panels.Contains(p))
                    newPanelsOrder.Add(p);
            }
            // Include any panels missing from visual (shouldn't normally happen), append them
            foreach (var p in Panels)
                if (!newPanelsOrder.Contains(p))
                    newPanelsOrder.Add(p);

            // Partition into pinned and unpinned preserving relative order
            var pinned = newPanelsOrder.Where(p => (p.Tag as EntryPanelState)?.IsPinned ?? false).ToList();
            var unpinned = newPanelsOrder.Where(p => !(p.Tag as EntryPanelState)?.IsPinned ?? true).ToList();

            // Produce final order: pinned first, then unpinned
            var final = new List<StackPanel>();
            final.AddRange(pinned);
            final.AddRange(unpinned);

            // Update internal list
            Panels.Clear();
            Panels.AddRange(final);

            // Update visual children
            itemsPanel.Children.Clear();
            foreach (var p in final)
                itemsPanel.Children.Add(p);
        }
    }
}
