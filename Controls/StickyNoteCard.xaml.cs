using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WindowsDashboard.ViewModels;

namespace WindowsDashboard.Controls;

public partial class StickyNoteCard : UserControl
{
    public StickyNoteCard()
    {
        InitializeComponent();
        AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(OnNoteTextChanged), true);
    }

    public event Action<NoteItemViewModel>? DeleteRequested;
    public event Action? AddRequested;
    public event Action? NotesChanged;

    public void SetNotes(IEnumerable<NoteItemViewModel> notes)
    {
        NotesList.ItemsSource = notes.ToList();
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is NoteItemViewModel vm)
        {
            DeleteRequested?.Invoke(vm);
        }
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        AddRequested?.Invoke();
    }

    private void OnNoteTextChanged(object sender, TextChangedEventArgs e)
    {
        NotesChanged?.Invoke();
    }
}
