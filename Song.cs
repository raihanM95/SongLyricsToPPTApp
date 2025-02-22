using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongLyricsToPPTApp
{
    public class Song
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Lyrics { get; set; }


        // Override ToString() for better display in listSelected (optional but recommended)
        public override string ToString()
        {
            return $"{Title} ({Artist})"; // Customize the display format
        }
    }
}
