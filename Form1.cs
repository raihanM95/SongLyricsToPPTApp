using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SongLyricsToPPTApp
{
    public partial class Form1 : Form
    {
        private string dbPath = "songs.db";
        private string placeholderText = "Search song";
        private Color placeholderColor = Color.Gray;
        private Color textColor = Color.Black;

        public Form1()
        {
            InitializeComponent();

            InitializeDatabase();
            LoadSongs();
        }

        private void InitializePlaceholder()
        {
            txtSearch.Text = placeholderText;
            txtSearch.ForeColor = placeholderColor;

            txtSearch.Enter += txtSearch_MouseEnter;
            txtSearch.Leave += txtSearch_MouseLeave;
        }

        private void InitializeDatabase()
        {
            //// Check if the database file exists and delete it
            //if (File.Exists(dbPath))
            //{
            //    File.Delete(dbPath);
            //}
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string createTableQuery = "CREATE TABLE Songs (Id INTEGER PRIMARY KEY, Title TEXT, Artist TEXT, Lyrics TEXT);";
                    SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void LoadSongs()
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT * FROM Songs";
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn);
                System.Data.DataTable dt = new System.Data.DataTable();
                adapter.Fill(dt);
                listShow.DataSource = dt;
                listShow.DisplayMember = "Title";
                listShow.ValueMember = "Id";
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddEditForm addEditForm = new AddEditForm();
            addEditForm.ShowDialog();
            LoadSongs();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            listShow.Focus();
            if (listShow.SelectedItem != null)
            {
                System.Data.DataRowView row = listShow.SelectedItem as System.Data.DataRowView;
                AddEditForm addEditForm = new AddEditForm(row);
                addEditForm.ShowDialog();
                LoadSongs();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Ignore filtering if the placeholder text is present
            if (txtSearch.Text == placeholderText)
                return;

            string filter = txtSearch.Text.ToLower();

            if (listShow.DataSource is System.Data.DataTable dt)
            {
                dt.DefaultView.RowFilter = string.IsNullOrWhiteSpace(filter)
                    ? ""  // Clear filter if empty
                    : $"Title LIKE '%{filter}%'";
            }
        }

        private void listShow_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listShow.SelectedItem != null)
            {
                // Directly access the DataRowView from the bound DataTable
                DataRowView selectedRow = (DataRowView)listShow.SelectedItem;

                Song song = new Song  // Assuming you have a Song class
                {
                    Title = selectedRow["Title"].ToString(),
                    Artist = selectedRow["Artist"].ToString(),
                    Lyrics = selectedRow["Lyrics"].ToString()
                };

                // Check for duplicates using LINQ and the Song objects
                if (!listSelected.Items.Cast<Song>().Any(s => s.Title == song.Title && s.Artist == song.Artist && s.Lyrics == song.Lyrics))
                {
                    listSelected.Items.Add(song);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            listSelected.Items.Clear();
        }

        //private void btnPublish_Click(object sender, EventArgs e)
        //{
        //    if (listSelected.Items.Count == 0) return;

        //    Type pptAppType = Type.GetTypeFromProgID("PowerPoint.Application");
        //    dynamic pptApp = Activator.CreateInstance(pptAppType);
        //    pptApp.Visible = true;

        //    dynamic presentations = pptApp.Presentations;
        //    dynamic presentation = presentations.Add();

        //    int slideIndex = 1;

        //    foreach (Song song in listSelected.Items) // Iterate through Song objects directly
        //    {
        //        dynamic slides = presentation.Slides;
        //        dynamic slide = slides.Add(slideIndex, 1);
        //        slideIndex++;

        //        dynamic shapes = slide.Shapes;
        //        dynamic titleShape = shapes.Item(1);
        //        titleShape.TextFrame.TextRange.Text = $"{song.Title} ({song.Artist})";

        //        dynamic bodyShape = shapes.Item(2);
        //        bodyShape.TextFrame.TextRange.Text = song.Lyrics;
        //    }

        //    SaveFileDialog saveFileDialog = new SaveFileDialog();
        //    saveFileDialog.Filter = "PowerPoint Presentation (*.pptx)|*.pptx|PowerPoint 97-2003 Presentation (*.ppt)|*.ppt";
        //    saveFileDialog.Title = "Save PowerPoint Presentation";
        //    saveFileDialog.FileName = "Song.pptx"; // Default file name

        //    // Release COM objects (important to prevent PowerPoint from hanging)
        //    System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
        //    System.Runtime.InteropServices.Marshal.ReleaseComObject(presentations);
        //    System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApp);

        //    GC.Collect(); // Force garbage collection
        //    GC.WaitForPendingFinalizers();
        //}

        private void btnPublish_Click(object sender, EventArgs e)
        {
            if (listSelected.Items.Count == 0) return;

            Type pptAppType = Type.GetTypeFromProgID("PowerPoint.Application");
            dynamic pptApp = Activator.CreateInstance(pptAppType);
            pptApp.Visible = true;

            dynamic presentations = pptApp.Presentations;
            dynamic presentation = presentations.Add();

            foreach (Song song in listSelected.Items)
            {
                string[] lyricsLines = song.Lyrics.Split(new[] { Environment.NewLine }, StringSplitOptions.None); // Split lyrics into lines

                dynamic slides = presentation.Slides;
                dynamic slide = slides.Add(slides.Count + 1, 1); // Add the first slide for the song
                dynamic shapes = slide.Shapes;
                dynamic titleShape = shapes.Item(1);
                titleShape.TextFrame.TextRange.Text = $"{song.Title} ({song.Artist})";

                dynamic bodyShape = shapes.Item(2);


                StringBuilder currentSlideLyrics = new StringBuilder();

                foreach (string line in lyricsLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) // Check for empty or whitespace-only lines
                    {
                        // Create a new slide if the current slide has content
                        if (currentSlideLyrics.Length > 0)
                        {
                            bodyShape.TextFrame.TextRange.Text = currentSlideLyrics.ToString().TrimEnd(); // Set lyrics of current slide
                            currentSlideLyrics.Clear(); // Clear for the next slide

                            slide = slides.Add(slides.Count + 1, 1); // Create a new slide
                            shapes = slide.Shapes;
                            bodyShape = shapes.Item(2); // Get the body shape of the new slide

                        }
                        continue; // Skip the empty line
                    }

                    currentSlideLyrics.AppendLine(line); // Add the line to the current slide's lyrics
                }

                // Add any remaining lyrics to a new slide (important for the last set of verses)
                if (currentSlideLyrics.Length > 0)
                {
                    bodyShape.TextFrame.TextRange.Text = currentSlideLyrics.ToString().TrimEnd();
                }

            }

            //SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.Filter = "PowerPoint Presentation (*.pptx)|*.pptx|PowerPoint 97-2003 Presentation (*.ppt)|*.ppt";
            //saveFileDialog.Title = "Save PowerPoint Presentation";
            //saveFileDialog.FileName = "Song.pptx";

            //if (saveFileDialog.ShowDialog() == DialogResult.OK)
            //{
            //    presentation.SaveAs(saveFileDialog.FileName);
            //}



            // Release COM objects (important to prevent PowerPoint from hanging)
            System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(presentations);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApp);

            GC.Collect(); // Force garbage collection
            GC.WaitForPendingFinalizers();
        }

        private void btnPublish_Click3(object sender, EventArgs e)
        {
            if (listSelected.Items.Count == 0) return;

            Type pptAppType = Type.GetTypeFromProgID("PowerPoint.Application");
            dynamic pptApp = Activator.CreateInstance(pptAppType);
            pptApp.Visible = true;

            dynamic presentations = pptApp.Presentations;
            dynamic presentation = presentations.Add();

            foreach (Song song in listSelected.Items)
            {
                string[] lyricsLines = song.Lyrics.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                dynamic slides = presentation.Slides;
                dynamic slide;
                dynamic shapes;
                dynamic titleShape;
                dynamic bodyShape;

                // Add the first slide (Title and Content layout)
                slide = slides.Add(slides.Count + 1, Microsoft.Office.Interop.PowerPoint.PpSlideLayout.ppLayoutText); // Use Title and Content layout
                shapes = slide.Shapes;
                titleShape = shapes.Title; // Access title shape directly
                titleShape.TextFrame.TextRange.Text = $"{song.Title} ({song.Artist})";
                bodyShape = shapes.Placeholders(2); // Get the content placeholder (index 2)



                StringBuilder currentSlideLyrics = new StringBuilder();

                foreach (string line in lyricsLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (currentSlideLyrics.Length > 0)
                        {
                            bodyShape.TextFrame.TextRange.Text = currentSlideLyrics.ToString().TrimEnd();
                            currentSlideLyrics.Clear();

                            // Create a new slide (Title and Content layout)
                            slide = slides.Add(slides.Count + 1, Microsoft.Office.Interop.PowerPoint.PpSlideLayout.ppLayoutText);
                            shapes = slide.Shapes;
                            bodyShape = shapes.Placeholders(2); // Get the content placeholder of the new slide

                        }
                        continue;
                    }

                    currentSlideLyrics.AppendLine(line);
                }

                if (currentSlideLyrics.Length > 0)
                {
                    bodyShape.TextFrame.TextRange.Text = currentSlideLyrics.ToString().TrimEnd();
                }

            }

            //SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.Filter = "PowerPoint Presentation (*.pptx)|*.pptx|PowerPoint 97-2003 Presentation (*.ppt)|*.ppt";
            //saveFileDialog.Title = "Save PowerPoint Presentation";
            //saveFileDialog.FileName = "Song.pptx";

            //if (saveFileDialog.ShowDialog() == DialogResult.OK)
            //{
            //    presentation.SaveAs(saveFileDialog.FileName);
            //}


            System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(presentations);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApp);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void btnPublish_Click2(object sender, EventArgs e)
        {
            if (listSelected.Items.Count == 0) return;

            Type pptAppType = Type.GetTypeFromProgID("PowerPoint.Application");
            dynamic pptApp = Activator.CreateInstance(pptAppType);
            pptApp.Visible = true;

            dynamic presentations = pptApp.Presentations;
            dynamic presentation = presentations.Add();

            foreach (Song song in listSelected.Items)
            {
                string[] lyricsLines = song.Lyrics.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                dynamic slides = presentation.Slides;
                dynamic slide;
                dynamic shapes;
                dynamic titleShape;
                dynamic bodyShape;

                slide = slides.Add(slides.Count + 1, PpSlideLayout.ppLayoutTitleOnly); // Or ppLayoutBlank
                shapes = slide.Shapes;

                titleShape = null;
                foreach (dynamic shape in shapes)
                {
                    if (shape.HasTextFrame != null && shape.HasTextFrame != 0) // Check for null AND non-zero
                    {
                        if ((int)shape.Type == (int)PpPlaceholderType.ppPlaceholderTitle)
                        {
                            titleShape = shape;
                            break;
                        }
                    }
                }

                // *** CRUCIAL CHECK: Make sure titleShape is not null before setting the text ***
                if (titleShape != null)
                {
                    titleShape.TextFrame.TextRange.Text = $"{song.Title} ({song.Artist})";
                }


                bodyShape = shapes.AddTextbox(1, 100, 150, 600, 300); // 1 for msoTextOrientationHorizontal - Adjust position/size!

                StringBuilder currentSlideLyrics = new StringBuilder();

                foreach (string line in lyricsLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (currentSlideLyrics.Length > 0)
                        {
                            bodyShape.TextFrame.TextRange.Text = currentSlideLyrics.ToString().TrimEnd();
                            currentSlideLyrics.Clear();

                            slide = slides.Add(slides.Count + 1, PpSlideLayout.ppLayoutTitleOnly); // Or ppLayoutBlank
                            shapes = slide.Shapes;

                            // *** KEY FIX: Find and set titleShape for the NEW slide ***
                            titleShape = null; // Reset titleShape
                            foreach (dynamic shape in shapes)
                            {
                                if (shape.HasTextFrame != null && shape.HasTextFrame != 0)
                                {
                                    if ((int)shape.Type == (int)PpPlaceholderType.ppPlaceholderTitle)
                                    {
                                        titleShape = shape;
                                        break;
                                    }
                                }
                            }

                            // *** CRUCIAL CHECK: Make sure titleShape is not null before setting the text ***
                            if (titleShape != null)
                            {
                                titleShape.TextFrame.TextRange.Text = $"{song.Title} ({song.Artist})";
                            }

                            bodyShape = shapes.AddTextbox(1, 100, 150, 600, 300); // 1 for msoTextOrientationHorizontal - Adjust position/size!
                        }
                        continue;
                    }

                    currentSlideLyrics.AppendLine(line);
                }

                if (currentSlideLyrics.Length > 0)
                {
                    bodyShape.TextFrame.TextRange.Text = currentSlideLyrics.ToString().TrimEnd();
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PowerPoint Presentation (*.pptx)|*.pptx|PowerPoint 97-2003 Presentation (*.ppt)|*.ppt";
            saveFileDialog.Title = "Save PowerPoint Presentation";
            saveFileDialog.FileName = "Song.pptx";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                presentation.SaveAs(saveFileDialog.FileName);
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(presentations);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApp);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void txtSearch_MouseEnter(object sender, EventArgs e)
        {
            if (txtSearch.Text == placeholderText)
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = textColor;
            }
        }

        private void txtSearch_MouseLeave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = placeholderText;
                txtSearch.ForeColor = placeholderColor;
            }
        }
    }
}
