using Sharpdown.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScintillaNET;

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
        public Scintilla MarkdownEditor;
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

            this.MarkdownEditor = new Scintilla();
            this.MarkdownEditor.Dock = DockStyle.Fill;
            this.MarkdownEditor.Text = "";
            this.MarkdownEditor.KeyUp += OnMarkdownChanged;
            this.MarkdownEditor.ViewWhitespace = WhitespaceMode.VisibleOnlyIndent;
            this.MarkdownEditor.UseTabs = false;
            this.MarkdownEditor.TabWidth = 4;
            this.MarkdownEditor.Click += OnClickSyncScrollbar;
            this.Panel1.Controls.Add(this.MarkdownEditor);

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
                        "<script>function scroll(ratio) { " +
                            "document.documentElement.scrollTop = ratio * document.documentElement.scrollHeight;" +
                        "}</script>" +
                    "</body>" +
                "</html>";

            this.HtmlView.Dock = DockStyle.Fill;
            this.Panel2.Controls.Add(this.HtmlView);
        }

        private void OnClickSyncScrollbar(object sender, EventArgs e)
        {

            double ratio = (double)this.MarkdownEditor.CurrentLine / (double)this.MarkdownEditor.Lines.Count;

            this.HtmlView.Document.InvokeScript("scroll", new object[] { ratio });
        }

        private void OnMarkdownChanged(object sender, EventArgs e)
        {

            if ((DateTime.Now - this.LastHit).Milliseconds < 50)
                return;

            this.LastHit = DateTime.Now;
            this.ParseMarkdown();
        }

        public void CenterSplitter()
        {

            this.SplitContainer.SplitterDistance = (int)(this.SplitContainer.Height / 2);
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
