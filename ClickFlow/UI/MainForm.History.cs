using ClickFlow.Core;
using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed partial class MainForm
{
    private void RefreshHistory(string? selectId = null)
    {
        var previousId = selectId ?? _selectedEntry?.Id;
        foreach (var card in _historyCards)
        {
            card.Dispose();
        }

        _historyCards.Clear();
        _historyFlow.Controls.Clear();

        var entries = RecordingLibrary.All;
        var scale = WindowEffects.GetDpiScale(this);
        foreach (var entry in entries)
        {
            var card = new RecordingCard(entry, scale);
            card.SelectedRequested += OnCardSelected;
            card.PlayRequested += OnCardPlayRequested;
            card.DeleteRequested += OnCardDeleteRequested;
            card.ExpandedChanged += _ => ResizeHistoryCards();
            _historyCards.Add(card);
            _historyFlow.Controls.Add(card);
        }

        _historyCount.Text = L.T($"{entries.Count} 条", $"{entries.Count} items");
        _historySize.Text = entries.Count == 0
            ? L.T("本地保存", "Saved locally")
            : L.T(
                $"{FormatBytes(RecordingLibrary.TotalBytes)} · 本地保存",
                $"{FormatBytes(RecordingLibrary.TotalBytes)} · saved locally");
        _historyEmpty.Visible = entries.Count == 0;
        if (_historyEmpty.Visible)
        {
            _historyEmpty.BringToFront();
        }

        ResizeHistoryCards();

        var selected = previousId == null
            ? null
            : _historyCards.FirstOrDefault(c => c.Entry.Id == previousId);
        if (selected != null)
        {
            SelectEntry(selected.Entry, selected);
        }
        else
        {
            SelectEntry(null, null);
        }
    }

    private void ResizeHistoryCards()
    {
        if (_historyFlow.ClientSize.Width <= 0)
        {
            return;
        }

        var width = Math.Max(
            260,
            _historyFlow.ClientSize.Width - _historyFlow.Padding.Horizontal -
            SystemInformation.VerticalScrollBarWidth - 10);
        _historyFlow.SuspendLayout();
        try
        {
            foreach (var card in _historyCards)
            {
                if (card.Width != width)
                {
                    card.Width = width;
                }
            }
        }
        finally
        {
            _historyFlow.ResumeLayout(true);
        }
    }

    private void OnCardSelected(RecordingCard card)
    {
        if (_state != RunState.Idle)
        {
            return;
        }

        SelectEntry(card.Entry, card);
    }

    private void OnCardPlayRequested(RecordingCard card)
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        if (!card.IsSelected)
        {
            SelectEntry(card.Entry, card);
            ShowToast(L.T("已选中这条录制，再点一次 ▶ 回放", "Selected; click ▶ again to replay"), 2.5);
            return;
        }

        RunSafe(() => PlayEntryAsync(card.Entry));
    }

    private void OnCardDeleteRequested(RecordingCard card)
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        var confirmed = MessageDialog.Show(
            this,
            L.T("删除这条录制？", "Delete this recording?"),
            L.T(
                $"“{card.Entry.Name}”会从录制历史中移除，本地文件也会一起删除，无法恢复。",
                $"“{card.Entry.Name}” will be removed from History, its local file will be deleted, and this cannot be undone."),
            L.T("删除", "Delete"),
            L.T("取消", "Cancel"),
            danger: true);
        if (!confirmed)
        {
            return;
        }

        try
        {
            var deletedId = card.Entry.Id;
            var nextId = _historyCards
                .FirstOrDefault(c => c.Entry.Id != deletedId)?.Entry.Id;
            RecordingLibrary.Delete(card.Entry);
            if (_selectedEntry?.Id == deletedId)
            {
                _selectedEntry = null;
            }

            RefreshHistory(nextId);
            ShowToast(L.T("已删除录制和本地文件", "Recording and local file deleted"), 2.5);
        }
        catch (Exception ex)
        {
            MessageDialog.Show(
                this,
                L.T("删除失败", "Delete failed"),
                ex.Message,
                L.T("知道了", "OK"),
                danger: true);
        }
    }

    private void SelectEntry(RecordingEntry? entry, RecordingCard? card = null)
    {
        _selectedEntry = entry;
        foreach (var historyCard in _historyCards)
        {
            historyCard.SetSelected(entry != null && historyCard.Entry.Id == entry.Id);
        }

        if (card != null)
        {
            card.Focus();
        }

        _playButton.Enabled = _state == RunState.Idle && entry != null;
        UpdateFooter();
    }

    private void ImportMacroDialog()
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = L.T("导入录制", "Import recording"),
            Filter = L.T(
                "轻录录制 (*.cfmacro)|*.cfmacro|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                "ClickFlow recording (*.cfmacro)|*.cfmacro|JSON file (*.json)|*.json|All files (*.*)|*.*"),
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = RecordingLibrary.FolderPath
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ImportMacroFile(dialog.FileName, select: true);
    }

    private void ImportMacroFile(string path, bool select)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var folder = Path.GetFullPath(RecordingLibrary.FolderPath);
            if (fullPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                RefreshHistory();
                var existing = _historyCards.FirstOrDefault(c =>
                    string.Equals(
                        Path.GetFullPath(c.Entry.FilePath),
                        fullPath,
                        StringComparison.OrdinalIgnoreCase));
                if (existing != null && select)
                {
                    SelectEntry(existing.Entry, existing);
                }

                return;
            }

            var entry = RecordingLibrary.Import(fullPath);
            RefreshHistory(entry.Id);
            var card = _historyCards.FirstOrDefault(c => c.Entry.Id == entry.Id);
            SelectEntry(entry, card);
            ShowToast(L.T($"已导入 · {entry.Name}", $"Imported · {entry.Name}"), 3);
        }
        catch (Exception ex)
        {
            MessageDialog.Show(
                this,
                L.T("无法导入录制", "Cannot import recording"),
                ex.Message,
                L.T("知道了", "OK"),
                danger: true);
        }
    }

    private void OnFileDragEnter(object? sender, DragEventArgs e)
    {
        if (_state == RunState.Idle &&
            e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files is { Length: > 0 } && files.Any(IsMacroFile))
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }
        }

        e.Effect = DragDropEffects.None;
    }

    private void OnFileDragDrop(object? sender, DragEventArgs e)
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            var macro = files.FirstOrDefault(IsMacroFile);
            if (macro != null)
            {
                ImportMacroFile(macro, select: true);
            }
        }
    }

    private static bool IsMacroFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(MacroStore.Extension, StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:0.#} KB";
        }

        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }
}
