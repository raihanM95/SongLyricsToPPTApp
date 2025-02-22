using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SongLyricsToPPTApp
{
    public partial class AddEditForm : Form
    {
        private string dbPath = "songs.db";
        private int? songId = null;

        public AddEditForm()
        {
            InitializeComponent();
            SetPlaceholders();
            AttachEvents();
        }

        public AddEditForm(DataRowView row) : this()
        {
            songId = Convert.ToInt32(row["Id"]);
            txtTitle.Text = row["Title"].ToString();
            txtArtist.Text = row["Artist"].ToString();
            txtLyrics.Text = row["Lyrics"].ToString();

            txtTitle.ForeColor = Color.Black;
            txtArtist.ForeColor = Color.Black;
            txtLyrics.ForeColor = Color.Black;
        }

        private void SetPlaceholders()
        {
            SetPlaceholder(txtTitle, "Enter song title");
            SetPlaceholder(txtArtist, "Enter artist name");
            SetPlaceholder(txtLyrics, "Enter song lyrics");
        }

        private void SetPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;
        }

        private void AttachEvents()
        {
            txtTitle.GotFocus += RemovePlaceholder;
            txtTitle.LostFocus += AddPlaceholder;
            txtArtist.GotFocus += RemovePlaceholder;
            txtArtist.LostFocus += AddPlaceholder;
            txtLyrics.GotFocus += RemovePlaceholder;
            txtLyrics.LostFocus += AddPlaceholder;
        }

        private void RemovePlaceholder(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.ForeColor == Color.Gray)
            {
                textBox.Text = "";
                textBox.ForeColor = Color.Black;
            }
        }

        private void AddPlaceholder(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox == txtTitle)
                    SetPlaceholder(textBox, "Enter song title");
                else if (textBox == txtArtist)
                    SetPlaceholder(textBox, "Enter artist name");
                else if (textBox == txtLyrics)
                    SetPlaceholder(textBox, "Enter song lyrics");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query;
                SQLiteCommand cmd;
                if (songId == null)
                {
                    query = "INSERT INTO Songs (Title, Artist, Lyrics) VALUES (@title, @artist, @lyrics)";
                    cmd = new SQLiteCommand(query, conn);
                }
                else
                {
                    query = "UPDATE Songs SET Title=@title, Artist=@artist, Lyrics=@lyrics WHERE Id=@id";
                    cmd = new SQLiteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", songId);
                }

                cmd.Parameters.AddWithValue("@title", txtTitle.Text);
                cmd.Parameters.AddWithValue("@artist", txtArtist.Text);
                cmd.Parameters.AddWithValue("@lyrics", txtLyrics.Text);
                cmd.ExecuteNonQuery();
            }
            this.Close();
        }
    }
}
