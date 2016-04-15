using Sharpdown.Properties;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Sharpdown
{
    public class EditorFile : IDisposable
    {

        const string TemporaryFolderName = "Sharpdown";
        const string UntitledFileName = "Untitled";

        public string FilePath;
        public bool IsTemporaryFilePath;
        public bool HasChanges;

        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.FilePath);
            }
        }

        public string TabName
        {
            get
            {
                return this.Name + (this.HasChanges ? " *" : "");
            }
        }

        public string Markdown
        {
            get
            {
                return this.MarkdownEditor.Text;
            }
            set
            {

                this.MarkdownEditor.Text = value;
            }
        }

        public string Html
        {
            get
            {
                return this.HtmlView.Document.GetElementById("markdown").InnerHtml;
            }
            set
            {

                this.HtmlView.Document.GetElementById("markdown").InnerHtml = value;
                //Filter links, they shouldn't really do something, things will bug
                //TODO: Start IE?
                foreach (HtmlElement element in this.HtmlView.Document.GetElementsByTagName("a")) {

                    element.SetAttribute("target", "_blank");
                }
                this.HtmlView.Document.InvokeScript("highlight");
            }
        }

        public TabPage TabPage;
        public SplitContainer SplitContainer;
        public SplitterPanel Panel1;
        public SplitterPanel Panel2;
        public FastColoredTextBox MarkdownEditor;
        public WebBrowser HtmlView;

        private DateTime LastHit;

        public EditorFile(string filePath)
        {

            this.FilePath = filePath;
            this.IsTemporaryFilePath = false;
            this.HasChanges = false;
            this.CreateTabPage();
            this.LoadFile();
        }

        public EditorFile()
        {

            this.FilePath = this.GenerateTempFileName();
            this.IsTemporaryFilePath = true;
            this.HasChanges = true;
            this.CreateTabPage();
        }

        public void Dispose()
        {

            this.HtmlView.DocumentText = "";
            this.HtmlView.Dispose();
            this.HtmlView = null;
            this.MarkdownEditor.Text = "";
            this.MarkdownEditor.Dispose();
            this.MarkdownEditor = null;
            this.SplitContainer.Dispose();
            this.SplitContainer = null;
            this.TabPage.Dispose();
            this.TabPage = null;
        }

        public void LoadFile()
        {

            if (!File.Exists(this.FilePath))
                throw new FileNotFoundException(
                    "The file " + this.FilePath + " does not exist or is not accessible in some way."
                );

            this.Markdown = File.ReadAllText(this.FilePath, Encoding.UTF8);
        }

        public void ParseMarkdown()
        {

            this.Html = CommonMark.CommonMarkConverter.Convert(this.Markdown);
        }

        private void CreateTabPage()
        {

            this.LastHit = DateTime.Now.AddSeconds(-5);

            //Create a new Tab for this one
            this.TabPage = new TabPage();
            this.TabPage.Text = this.Name;

            this.SplitContainer = new SplitContainer();
            this.SplitContainer.Dock = DockStyle.Fill;
            this.TabPage.Controls.Add(this.SplitContainer);

            this.Panel1 = this.SplitContainer.Panel1;
            this.Panel2 = this.SplitContainer.Panel2;

            CreateMarkdownEditor();
            CreateHtmlView();
        }

        private void CreateHtmlView()
        {

            this.HtmlView = new WebBrowser();
            this.HtmlView.DocumentText =
                "<!DOCTYPE html>" +
                "<html lang=\"en\">" +
                    "<head>" +
                        "<meta charset=\"utf-8\">" +
                        "<title>Parsed Markdown Result HTML</title>" +
                        "<style>body { margin: 0; }</style>" +
                        "<style>" + Resources.DocumentStyle + "</style>" +
                        "<style>" + Resources.HighlightStyle + "</style>" +
                    "</head>" +
                    "<body>" +
                        "<div id=\"markdown\"></div>" +
                        "<script>" + Resources.HightlightScript + "</script>" +
                        "<script>function highlight() { Prism.highlightAll(); }</script>" +
                        "<script>function setScrollRatio(ratio) { " +
                            "document.documentElement.scrollTop = ratio * document.documentElement.scrollHeight;" +
                        "}</script>" +
                    "</body>" +
                "</html>";
            this.HtmlView.ScriptErrorsSuppressed = true;
            this.HtmlView.DocumentCompleted += (object sender, WebBrowserDocumentCompletedEventArgs e) => {

                this.HtmlView.Document.Window.Error += (object errorSender, HtmlElementErrorEventArgs errorEvent) => {

                    errorEvent.Handled = true;

                    if (Settings.Default.ShowHtmlViewScriptErrors)
                        Program.DisplayError("A JavaScript error occured: \n\n" + errorEvent.Description);
                };
            };
            this.HtmlView.Dock = DockStyle.Fill;
            this.Panel2.Controls.Add(this.HtmlView);
        }
        

        private void CreateMarkdownEditor()
        {


            Style headerStyle = new TextStyle(Brushes.Navy, null, FontStyle.Bold);
            Style emStyle = new TextStyle(Brushes.Gray, null, FontStyle.Italic);
            Style strongStyle = new TextStyle(Brushes.Gray, null, FontStyle.Bold);
            Style hrStyle = new TextStyle(Brushes.Orange, null, FontStyle.Bold);
            Style linkStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
            Style listStyle = new TextStyle(Brushes.LightGreen, null, FontStyle.Bold);
            Style quoteStyle = new TextStyle(Brushes.DarkGreen, null, FontStyle.Italic);
            Style codeStyle = new TextStyle(Brushes.Black, Brushes.LightGray, FontStyle.Regular);

            this.MarkdownEditor = new FastColoredTextBox();
            this.MarkdownEditor.Dock = DockStyle.Fill;
            this.MarkdownEditor.Text = "";
            this.MarkdownEditor.TextChangedDelayed += (object sender, TextChangedEventArgs e) => {

                if ((DateTime.Now - this.LastHit).Milliseconds < Settings.Default.MarkdownParseDelayMilliseconds)
                    return;

                this.LastHit = DateTime.Now;
                this.ParseMarkdown();
                SyncScrollbars();
            };
            this.MarkdownEditor.TextChanged += (object sender, TextChangedEventArgs e) => {

                e.ChangedRange.ClearStyle(headerStyle);
                e.ChangedRange.SetStyle(headerStyle, @"^[#]+ .*$", RegexOptions.Multiline);

                e.ChangedRange.ClearStyle(hrStyle);
                e.ChangedRange.SetStyle(hrStyle, @"^[\-*]{3,}\s*$", RegexOptions.Multiline);
                e.ChangedRange.SetStyle(listStyle, @"^- - -[\- ]*$", RegexOptions.Multiline);

                e.ChangedRange.ClearStyle(listStyle);
                e.ChangedRange.SetStyle(listStyle, @"^(?<range>[\-*])\s+[^\-].*$", RegexOptions.Multiline);

                e.ChangedRange.ClearStyle(linkStyle);
                e.ChangedRange.SetStyle(linkStyle, @"\[[^\]]*\]\([^\)]*\)");
                e.ChangedRange.SetStyle(linkStyle, @"\[[^\]]*\]\[[^\]]*\]");
                e.ChangedRange.SetStyle(linkStyle, @"<[^>]*>");
                e.ChangedRange.SetStyle(linkStyle, @"^\[[^\]]*\]:.*$");

                e.ChangedRange.ClearStyle(emStyle);
                e.ChangedRange.SetStyle(emStyle, @"(^|[^*_])([*_])[^*_]+\2([^*_]|$)", RegexOptions.Multiline);

                e.ChangedRange.ClearStyle(strongStyle);
                e.ChangedRange.SetStyle(strongStyle, @"([*_]{2})[^*_]+\1", RegexOptions.Multiline);

                e.ChangedRange.ClearStyle(quoteStyle);
                e.ChangedRange.SetStyle(quoteStyle, @"^[>]+\s+.*$", RegexOptions.Multiline);

                e.ChangedRange.ClearStyle(codeStyle);
                e.ChangedRange.SetStyle(codeStyle, @"`[^`]*`");

            };
            this.MarkdownEditor.AutoCompleteBrackets = Settings.Default.MarkdownAutoCompletionEnabled;
            this.MarkdownEditor.AutoIndent = true;
            this.MarkdownEditor.SelectionChanged += (object sender, EventArgs e) => SyncScrollbars();
            this.Panel1.Controls.Add(this.MarkdownEditor);
        }

        private void SyncScrollbars()
        {

            FastColoredTextBox editor = this.MarkdownEditor;
            var range = editor.Selection;
            Trace.WriteLine(range.FromLine + " " + editor.LinesCount + " " + ((double)range.FromLine / (double)editor.LinesCount));
            double ratio = ((double)range.FromLine / (double)editor.LinesCount);

            this.HtmlView.Document.InvokeScript("setScrollRatio", new object[] { ratio });
        }

        private string GenerateTempFileName()
        {

            string dir = Path.Combine(Path.GetTempPath(), EditorFile.TemporaryFolderName);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName;
            int i = 0;
            do {

                fileName = Path.Combine(dir, EditorFile.UntitledFileName) + (i == 0 ? "" : "-" + i);
            } while (File.Exists(fileName));

            return fileName;
        }
    }
}
