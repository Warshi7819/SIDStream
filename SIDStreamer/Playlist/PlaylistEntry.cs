namespace SIDStreamer.Playlists
{
    public class PlaylistEntry
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int LengthSeconds { get; set; } = -1;
        public string LengthDisplay { get; set; } = string.Empty;

        public PlaylistEntry() { }

        public PlaylistEntry(string filePath, string? title = null, int lengthSeconds = -1, string lengthDisplay = "")
        {
            FilePath = filePath;
            Title = title ?? Path.GetFileNameWithoutExtension(filePath);
            LengthSeconds = lengthSeconds;
            LengthDisplay = lengthDisplay;
        }

        public override string ToString()
        {
            if (LengthSeconds >= 0 && !string.IsNullOrEmpty(LengthDisplay))
                return $"{Title}  {LengthDisplay}";
            return Title;
        }
    }
}
