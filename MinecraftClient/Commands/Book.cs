using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Tui;

namespace MinecraftClient.Commands
{
    public class Book : Command
    {
        private const char PageDelimiter = '\f';

        public override string CmdName => "book";
        public override string CmdUsage => Translations.cmd_book_usage;
        public override string CmdDesc => Translations.cmd_book_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("read").Executes(r => GetUsage(r.Source, "read")))
                    .Then(l => l.Literal("write").Executes(r => GetUsage(r.Source, "write")))
                    .Then(l => l.Literal("edit").Executes(r => GetUsage(r.Source, "edit")))
                    .Then(l => l.Literal("sign").Executes(r => GetUsage(r.Source, "sign")))));

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Literal("read")
                    .Executes(r => ReadBook(r.Source, null))
                    .Then(l => l.Argument("Page", Arguments.Integer(min: 1))
                        .Executes(r => ReadBook(r.Source, Arguments.GetInteger(r, "Page")))))
                .Then(l => l.Literal("write")
                    .Then(l => l.Literal("text")
                        .Then(l => l.Argument("Text", Arguments.GreedyString())
                            .Executes(r => WriteBook(r.Source, Arguments.GetString(r, "Text")))))
                    .Then(l => l.Literal("file")
                        .Then(l => l.Argument("Path", Arguments.GreedyString())
                            .Executes(r => WriteBookFromFile(r.Source, Arguments.GetString(r, "Path"))))))
                .Then(l => l.Literal("edit")
                    .Executes(r => OpenEditor(r.Source))
                    .Then(l => l.Literal("page")
                        .Then(l => l.Argument("Page", Arguments.Integer(min: 1))
                            .Then(l => l.Argument("Text", Arguments.GreedyString())
                                .Executes(r => EditPage(r.Source, Arguments.GetInteger(r, "Page"), Arguments.GetString(r, "Text"))))))
                    .Then(l => l.Literal("insert")
                        .Then(l => l.Argument("Page", Arguments.Integer(min: 1))
                            .Then(l => l.Argument("Text", Arguments.GreedyString())
                                .Executes(r => InsertPage(r.Source, Arguments.GetInteger(r, "Page"), Arguments.GetString(r, "Text"))))))
                    .Then(l => l.Literal("delete")
                        .Then(l => l.Argument("Page", Arguments.Integer(min: 1))
                            .Executes(r => DeletePage(r.Source, Arguments.GetInteger(r, "Page"))))))
                .Then(l => l.Literal("sign")
                    .Then(l => l.Argument("Title", Arguments.GreedyString())
                        .Executes(r => SignBook(r.Source, Arguments.GetString(r, "Title")))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName))));
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
                "read" => Translations.cmd_book_help_read,
                "write" => Translations.cmd_book_help_write,
                "edit" => Translations.cmd_book_help_edit,
                "sign" => Translations.cmd_book_help_sign,
                _ => GetCmdDescTranslated()
            });
        }

        private int ReadBook(CmdResult r, int? page)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureInventory(r, handler))
                return -1;

            if (!handler.TryGetHeldBookContent(out BookContent content))
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_not_holding_book);

            if (page is null && BookTuiHost.TryOpen(handler, BookHand.Main, editable: false))
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_tui_opened);

            handler.Log.Info(FormatBook(content, page));
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int OpenEditor(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureWritable(r, handler, out _))
                return -1;

            return BookTuiHost.TryOpen(handler, BookHand.Main, editable: true)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_tui_opened)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_tui_required);
        }

        private int WriteBook(CmdResult r, string text)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureWritable(r, handler, out _))
                return -1;

            IReadOnlyList<string> pages = SplitPages(text);
            if (!Validate(r, handler, pages, title: null))
                return -1;

            return handler.SendBookEdit(pages)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_write_sent)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_write_failed);
        }

        private int WriteBookFromFile(CmdResult r, string path)
        {
            if (!File.Exists(path))
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_file_not_found, path));

            return WriteBook(r, File.ReadAllText(path, Encoding.UTF8));
        }

        private int EditPage(CmdResult r, int page, string text)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureWritable(r, handler, out BookContent content))
                return -1;

            List<string> pages = content.Pages.ToList();
            if (page > pages.Count)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_page_out_of_range, page, pages.Count));

            pages[page - 1] = DecodeInlineText(text);
            if (!Validate(r, handler, pages, title: null))
                return -1;

            return handler.SendBookEdit(pages)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_edit_sent)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_write_failed);
        }

        private int InsertPage(CmdResult r, int page, string text)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureWritable(r, handler, out BookContent content))
                return -1;

            List<string> pages = content.Pages.ToList();
            if (page > pages.Count + 1)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_page_out_of_range, page, pages.Count));

            pages.Insert(page - 1, DecodeInlineText(text));
            if (!Validate(r, handler, pages, title: null))
                return -1;

            return handler.SendBookEdit(pages)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_edit_sent)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_write_failed);
        }

        private int DeletePage(CmdResult r, int page)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureWritable(r, handler, out BookContent content))
                return -1;

            List<string> pages = content.Pages.ToList();
            if (page > pages.Count)
                return r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_page_out_of_range, page, pages.Count));

            pages.RemoveAt(page - 1);
            if (pages.Count == 0)
                pages.Add(string.Empty);

            if (!Validate(r, handler, pages, title: null))
                return -1;

            return handler.SendBookEdit(pages)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_edit_sent)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_write_failed);
        }

        private int SignBook(CmdResult r, string title)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!EnsureWritable(r, handler, out BookContent content))
                return -1;

            string normalizedTitle = title.Trim();
            if (!Validate(r, handler, content.Pages, normalizedTitle))
                return -1;

            return handler.SendBookEdit(content.Pages, normalizedTitle)
                ? r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_book_sign_sent)
                : r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_write_failed);
        }

        private static bool EnsureInventory(CmdResult r, McClient handler)
        {
            if (handler.GetInventoryEnabled())
                return true;

            r.SetAndReturn(CmdResult.Status.FailNeedInventory);
            return false;
        }

        private static bool EnsureWritable(CmdResult r, McClient handler, out BookContent content)
        {
            content = BookContent.EmptyWritable;
            if (!EnsureInventory(r, handler))
                return false;

            Item? item = handler.GetHeldBook();
            if (!BookContentHelper.IsWritableBook(item))
            {
                r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_book_not_holding_writable);
                return false;
            }

            return BookContentHelper.TryRead(item, out content);
        }

        private static IReadOnlyList<string> SplitPages(string text)
        {
            return BookContentHelper.NormalizePages(DecodeInlineText(text).Split(PageDelimiter));
        }

        private static string DecodeInlineText(string text)
        {
            return text.Replace("\\f", PageDelimiter.ToString(), StringComparison.Ordinal)
                .Replace("\\n", "\n", StringComparison.Ordinal);
        }

        private static bool Validate(CmdResult r, McClient handler, IReadOnlyList<string> pages, string? title)
        {
            BookLimits limits = BookLimits.ForProtocol(handler.GetProtocolVersion());

            if (pages.Count > limits.MaxPages)
            {
                r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_too_many_pages, pages.Count, limits.MaxPages));
                return false;
            }

            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Length > limits.MaxPageLength)
                {
                    r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_page_too_long, i + 1, pages[i].Length, limits.MaxPageLength));
                    return false;
                }
            }

            if (title is not null && (title.Length == 0 || title.Length > limits.MaxTitleLength))
            {
                r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_book_title_invalid, limits.MaxTitleLength));
                return false;
            }

            return true;
        }

        private static string FormatBook(BookContent content, int? page)
        {
            StringBuilder sb = new();
            sb.AppendLine(content.IsSigned
                ? string.Format(Translations.cmd_book_header_signed, content.Title ?? string.Empty, content.Author ?? string.Empty)
                : Translations.cmd_book_header_writable);

            if (page is not null)
            {
                int index = page.Value - 1;
                if (index < 0 || index >= content.Pages.Count)
                    return string.Format(Translations.cmd_book_page_out_of_range, page.Value, content.Pages.Count);

                sb.AppendLine(string.Format(Translations.cmd_book_page_header, page.Value, content.Pages.Count));
                sb.Append(content.Pages[index]);
                return sb.ToString();
            }

            for (int i = 0; i < content.Pages.Count; i++)
            {
                sb.AppendLine(string.Format(Translations.cmd_book_page_header, i + 1, content.Pages.Count));
                sb.AppendLine(content.Pages[i]);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
