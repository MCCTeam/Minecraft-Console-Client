using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tui;

public static class BookTuiHost
{
    private static volatile bool isRunning;

    public static bool TryOpen(McClient handler, BookHand hand, bool editable)
    {
        if (ConsoleIO.Backend is not TuiConsoleBackend)
            return false;

        Open(handler, hand, editable);
        return true;
    }

    public static void OpenFromServer(McClient handler, BookHand hand)
    {
        if (ConsoleIO.Backend is TuiConsoleBackend)
            Open(handler, hand, editable: BookContentHelper.IsWritableBook(handler.GetHeldBook(hand)));
    }

    private static void Open(McClient handler, BookHand hand, bool editable)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (isRunning)
                return;

            MainTuiView? view = TuiConsoleBackend.Instance?.GetView();
            if (view is null)
                return;

            if (!handler.TryGetHeldBookContent(out BookContent content, hand))
                return;

            isRunning = true;
            var bookView = new BookView(handler, content, editable && !content.IsSigned);
            view.ShowOverlay(bookView, () => isRunning = false);
        });
    }
}

internal sealed class BookView : UserControl
{
    private readonly McClient handler;
    private readonly bool editable;
    private readonly List<string> pages;
    private readonly TextBlock header;
    private readonly TextBlock status;
    private readonly TextBlock shortcutTip;
    private readonly TextBox pageText;
    private readonly TextBox titleText;
    private readonly Button previousButton;
    private readonly Button nextButton;
    private readonly Button insertButton;
    private readonly Button deleteButton;
    private readonly Button saveButton;
    private readonly Button signButton;
    private bool bookSigned;
    private int pageIndex;

    public BookView(McClient handler, BookContent content, bool editable)
    {
        this.handler = handler;
        this.editable = editable;
        bookSigned = content.IsSigned;
        pages = content.Pages.ToList();
        if (pages.Count == 0)
            pages.Add(string.Empty);

        Focusable = true;
        Background = Brushes.Black;

        header = new TextBlock
        {
            Foreground = Brushes.Yellow,
            Margin = new Thickness(1, 0),
            TextWrapping = TextWrapping.Wrap
        };

        status = new TextBlock
        {
            Foreground = Brushes.Gray,
            Margin = new Thickness(1, 0),
            TextWrapping = TextWrapping.Wrap
        };

        shortcutTip = new TextBlock
        {
            Foreground = Brushes.DarkGray,
            Margin = new Thickness(1, 0),
            Text = Translations.tui_book_page_shortcut_tip,
            TextWrapping = TextWrapping.Wrap
        };

        pageText = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = !CanEdit,
            Foreground = Brushes.White,
            Background = Brushes.Black,
            BorderBrush = Brushes.Gray,
            MinHeight = 12,
            Margin = new Thickness(1)
        };
        pageText.TextChanged += (_, _) =>
        {
            if (CanEdit && pageIndex >= 0 && pageIndex < pages.Count)
                pages[pageIndex] = pageText.Text ?? string.Empty;
        };

        titleText = new TextBox
        {
            Watermark = Translations.tui_book_title_watermark,
            IsVisible = CanEdit,
            Foreground = Brushes.White,
            Background = Brushes.Black,
            BorderBrush = Brushes.Gray,
            Margin = new Thickness(1)
        };

        previousButton = Button(Translations.tui_book_prev, (_, _) => TryMovePage(-1));
        nextButton = Button(Translations.tui_book_next, (_, _) => TryMovePage(1));
        insertButton = Button(Translations.tui_book_insert, (_, _) => InsertPage(), editable);
        deleteButton = Button(Translations.tui_book_delete, (_, _) => DeletePage(), editable);
        saveButton = Button(Translations.tui_book_save, (_, _) => Save(), editable);
        signButton = Button(Translations.tui_book_sign, (_, _) => Sign(), editable);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 1,
            Margin = new Thickness(1),
            Children =
            {
                previousButton,
                nextButton,
                insertButton,
                deleteButton,
                saveButton,
                signButton,
                Button(Translations.tui_book_close, (_, _) => Close())
            }
        };

        var panel = new DockPanel
        {
            Background = Brushes.Black,
            Children =
            {
                DockTo(header, Dock.Top),
                DockTo(status, Dock.Bottom),
                DockTo(shortcutTip, Dock.Bottom),
                DockTo(controls, Dock.Bottom),
                DockTo(titleText, Dock.Bottom),
                pageText
            }
        };

        Content = panel;
        AttachedToVisualTree += (_, _) =>
        {
            AddHandler(KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            FocusPageText();
        };
        DetachedFromVisualTree += (_, _) => RemoveHandler(KeyDownEvent, OnTunnelKeyDown);
        Refresh();
    }

    private bool CanEdit => editable && !bookSigned;

    private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.PageUp)
        {
            TryMovePage(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.PageDown)
        {
            TryMovePage(1);
            e.Handled = true;
        }
    }

    private static Control DockTo(Control control, Dock dock)
    {
        DockPanel.SetDock(control, dock);
        return control;
    }

    private static Button Button(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> handler, bool enabled = true)
    {
        var button = new Button
        {
            Content = text,
            IsEnabled = enabled,
            Padding = new Thickness(1, 0),
            Margin = new Thickness(0)
        };
        button.Click += handler;
        return button;
    }

    private bool TryMovePage(int delta)
    {
        int targetPageIndex = Math.Clamp(pageIndex + delta, 0, pages.Count - 1);
        if (targetPageIndex == pageIndex)
            return false;

        pageIndex = targetPageIndex;
        Refresh();
        return true;
    }

    private void InsertPage()
    {
        if (!CanEdit)
            return;

        pages.Insert(pageIndex + 1, string.Empty);
        pageIndex++;
        Refresh();
    }

    private void DeletePage()
    {
        if (!CanEdit)
            return;

        if (pages.Count == 1)
            pages[0] = string.Empty;
        else
        {
            pages.RemoveAt(pageIndex);
            pageIndex = Math.Clamp(pageIndex, 0, pages.Count - 1);
        }
        Refresh();
    }

    private void Save()
    {
        if (bookSigned || IsHeldBookSigned())
        {
            bookSigned = true;
            status.Text = Translations.cmd_book_cannot_edit_signed;
            RefreshEditability();
            return;
        }

        if (!Validate(out string error))
        {
            status.Text = error;
            return;
        }

        status.Text = handler.SendBookEdit(pages)
            ? Translations.tui_book_saved
            : Translations.tui_book_save_failed;
    }

    private void Sign()
    {
        if (bookSigned || IsHeldBookSigned())
        {
            bookSigned = true;
            status.Text = Translations.cmd_book_already_signed;
            RefreshEditability();
            return;
        }

        string title = (titleText.Text ?? string.Empty).Trim();
        if (!Validate(out string error, title))
        {
            status.Text = error;
            return;
        }

        if (handler.SendBookEdit(pages, title))
        {
            bookSigned = true;
            status.Text = Translations.tui_book_signed;
            RefreshEditability();
            return;
        }

        status.Text = IsHeldBookSigned()
            ? Translations.cmd_book_already_signed
            : Translations.tui_book_save_failed;
    }

    private bool Validate(out string error, string? title = null)
    {
        BookLimits limits = BookLimits.ForProtocol(handler.GetProtocolVersion());
        error = string.Empty;

        if (pages.Count > limits.MaxPages)
        {
            error = string.Format(Translations.cmd_book_too_many_pages, pages.Count, limits.MaxPages);
            return false;
        }

        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i].Length > limits.MaxPageLength)
            {
                error = string.Format(Translations.cmd_book_page_too_long, i + 1, pages[i].Length, limits.MaxPageLength);
                return false;
            }
        }

        if (title is not null && (title.Length == 0 || title.Length > limits.MaxTitleLength))
        {
            error = string.Format(Translations.cmd_book_title_invalid, limits.MaxTitleLength);
            return false;
        }

        return true;
    }

    private void Close()
    {
        TuiConsoleBackend.Instance?.GetView()?.HideOverlay();
    }

    private void Refresh()
    {
        string currentPageText = pages[pageIndex];
        if (!string.Equals(pageText.Text, currentPageText, StringComparison.Ordinal))
            pageText.Text = currentPageText;

        header.Text = string.Format(Translations.tui_book_page_header, pageIndex + 1, pages.Count);
        status.Text = CanEdit ? Translations.tui_book_editing : Translations.tui_book_reading;
        RefreshEditability();
        FocusPageText();
    }

    private void RefreshEditability()
    {
        previousButton.IsEnabled = pageIndex > 0;
        nextButton.IsEnabled = pageIndex < pages.Count - 1;
        insertButton.IsEnabled = CanEdit;
        deleteButton.IsEnabled = CanEdit;
        saveButton.IsEnabled = CanEdit;
        signButton.IsEnabled = CanEdit;
        pageText.IsReadOnly = !CanEdit;
        titleText.IsEnabled = CanEdit;
        titleText.IsVisible = CanEdit;
        shortcutTip.Text = CanEdit
            ? Translations.tui_book_edit_shortcut_tip
            : Translations.tui_book_page_shortcut_tip;
    }

    private void FocusPageText()
    {
        Dispatcher.UIThread.Post(() =>
        {
            pageText.Focus();
            if (CanEdit)
                pageText.CaretIndex = pageText.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    private bool IsHeldBookSigned()
    {
        return BookContentHelper.TryRead(handler.GetHeldBook(BookHand.Main), out BookContent content) && content.IsSigned;
    }
}
