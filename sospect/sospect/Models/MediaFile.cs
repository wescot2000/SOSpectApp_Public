using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sospect.Models
{
    public class MediaFile
    {
        public string FilePath { get; set; }
        public string ThumbnailPath { get; set; }
        public bool IsVideo { get; set; }
        public bool IsPhoto => !IsVideo;
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public long FileSizeBytes { get; set; }

        // Para mostrar en la UI
        public string DisplaySize => FormatFileSize(FileSizeBytes);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}