using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sharpdown
{
    public partial class EditorForm : Form
    {

        public List<EditorFile> OpenFiles;

        private string[] PassedFiles;

        public EditorForm(string[] files = null)
        {
            InitializeComponent();

            this.OpenFiles = new List<EditorFile>();
            this.PassedFiles = files;
        }

        

        public void CreateNewFile()
        {

            this.AddFile(new EditorFile());
        }

        public void OpenFile(string path)
        {

            EditorFile file;
            try {

                file = new EditorFile(path);
            } catch(Exception ex) {

                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
                return;
            }

            this.AddFile(file);
        }

        public void AddFile(EditorFile file)
        {

            this.OpenFiles.Add(file);
            this.TabControl.TabPages.Add(file.TabPage);
            this.TabControl.SelectedTab = file.TabPage;
            file.SplitContainer.SplitterDistance = (int)(this.Width / 2);
        }

        private void OnNewMenuItemClicked(object sender, EventArgs e)
        {

            this.CreateNewFile();
        }

        private void EditorForm_Load(object sender, EventArgs e)
        {

            if (this.PassedFiles != null)
                foreach (string path in this.PassedFiles)
                    this.OpenFile(path);

            if (this.OpenFiles.Count == 0)
                this.CreateNewFile();
        }
    }
}