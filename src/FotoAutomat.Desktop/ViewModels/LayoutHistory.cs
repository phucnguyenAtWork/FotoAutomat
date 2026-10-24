using FotoAutomat.Core;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FotoAutomat.Desktop.ViewModels;

public sealed partial class LayoutEditorViewModel
{
    private sealed record EditState(PrintLayout Layout, byte[]? Bytes, ImageSource? Image);
    private readonly List<EditState> undo = [], redo = [];
    private readonly HashSet<PhotoSlotViewModel> observed = [];
    private readonly DispatcherTimer historyTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private EditState? current;
    private bool restoring;
    private bool gesture;
    public void BeginGesture() { FlushHistory(); gesture = true; }
    public void EndGesture() { gesture = false; FlushHistory(); }
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public ICommand UndoCommand => new EditorCommand(_ => RestoreHistory(undo, redo));
    public ICommand RedoCommand => new EditorCommand(_ => RestoreHistory(redo, undo));
    public ICommand NewLayoutCommand => new EditorCommand(_ =>
    {
        if (ConfirmReplaceLayout?.Invoke() == false) return;
        SelectedSlot = null; Slots.Clear(); SetArtwork(null, null); TemplateName = "Khung mới";
        CutIntoStrips = false; SingleStrip = false; BackgroundColor = "#FFFFFF"; preset = null; Changed(nameof(SelectedPreset)); Preview = false;
    });
    public ICommand LayerOrderCommand => new EditorCommand(parameter =>
    {
        if (SelectedSlot is not { } slot || slot.Locked || Preview) return;
        int old = Slots.IndexOf(slot), next = Math.Clamp(old + (parameter?.ToString() == "Front" ? 1 : -1), 0, Slots.Count - 1);
        if (old != next) Slots.Move(old, next);
        SelectedSlot = slot;
    });
    private EditState CaptureState() => new(new(TemplateName, CutIntoStrips, Slots.Select(s => s.Export()).ToArray(), ArtworkBehindPhotos: ArtworkBehindPhotos, SingleStrip: SingleStrip, BackgroundColor: BackgroundColor, Dpi: Dpi, Landscape: landscape), artworkBytes, Artwork);
    private void InitializeHistory()
    {
        historyTimer.Tick += (_, _) => FlushHistory();
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TemplateName) or nameof(CutIntoStrips) or nameof(Artwork) or nameof(ArtworkBehindPhotos) or nameof(SingleStrip) or nameof(BackgroundColor) or nameof(Dpi) or nameof(OrientationIndex)) ScheduleHistory();
        };
        Slots.CollectionChanged += (_, _) =>
        {
            foreach (var slot in observed.Where(s => !Slots.Contains(s)).ToArray()) { slot.PropertyChanged -= SlotChanged; observed.Remove(slot); }
            foreach (var slot in Slots.Where(s => !observed.Contains(s))) { observed.Add(slot); slot.PropertyChanged += SlotChanged; }
            ScheduleHistory();
        };
        ResetHistory();
    }
    private void SlotChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(PhotoSlotViewModel.IsSelected) or nameof(PhotoSlotViewModel.Label))) ScheduleHistory();
    }
    private void ScheduleHistory()
    {
        if (restoring || gesture) return;
        historyTimer.Stop(); historyTimer.Start();
    }
    private void FlushHistory()
    {
        historyTimer.Stop();
        var next = CaptureState();
        if (current is not null && (current.Bytes != next.Bytes || JsonSerializer.Serialize(current.Layout) != JsonSerializer.Serialize(next.Layout)))
        {
            undo.Add(current); if (undo.Count > 30) undo.RemoveAt(0); redo.Clear();
        }
        current = next; Changed(nameof(CanUndo)); Changed(nameof(CanRedo));
    }
    private void RestoreHistory(List<EditState> from, List<EditState> to)
    {
        FlushHistory();
        if (from.Count == 0 || current is null) return;
        to.Add(current);
        var state = from[^1]; from.RemoveAt(from.Count - 1);
        restoring = true;
        try
        {
            SelectedSlot = null; Slots.Clear(); SingleStrip = state.Layout.SingleStrip; BackgroundColor = state.Layout.BackgroundColor; TemplateName = state.Layout.Name; CutIntoStrips = state.Layout.CutIntoStrips;
            foreach (var slot in state.Layout.Slots) Slots.Add(PhotoSlotViewModel.From(slot, Slots.Count + 1, CutIntoStrips, PageWidth));
            ArtworkBehindPhotos = state.Layout.ArtworkBehindPhotos; SetArtwork(state.Bytes, state.Image); SelectedSlot = Slots.FirstOrDefault();
            preset = null; Changed(nameof(SelectedPreset)); current = state;
        }
        finally { restoring = false; }
        Changed(nameof(CanUndo)); Changed(nameof(CanRedo));
    }
    public void ResetHistory() { historyTimer.Stop(); undo.Clear(); redo.Clear(); current = CaptureState(); Changed(nameof(CanUndo)); Changed(nameof(CanRedo)); }
    public void StopHistory() => historyTimer.Stop();
}
