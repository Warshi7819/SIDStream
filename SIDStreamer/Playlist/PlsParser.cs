namespace SIDStreamer.Playlists
{
    public static class PlsParser
    {
        public static Playlist Load(string path)
        {
            var playlist = new Playlist { FilePath = path };
            var lines = File.ReadAllLines(path);

            var entries = new Dictionary<int, PlaylistEntry>();
            int numberOfEntries = 0;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(';'))
                    continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                    continue;

                var eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                    continue;

                var key = line[..eqIndex].Trim();
                var value = line[(eqIndex + 1)..].Trim();

                if (key.Equals("NumberOfEntries", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(value, out numberOfEntries);
                }
                else if (key.StartsWith("File", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(key[4..], out int fileIndex))
                {
                    if (!entries.ContainsKey(fileIndex))
                        entries[fileIndex] = new PlaylistEntry();

                    entries[fileIndex].FilePath = value;
                    if (string.IsNullOrEmpty(entries[fileIndex].Title))
                        entries[fileIndex].Title = Path.GetFileNameWithoutExtension(value);
                }
                else if (key.StartsWith("Title", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(key[5..], out int titleIndex))
                {
                    if (!entries.ContainsKey(titleIndex))
                        entries[titleIndex] = new PlaylistEntry();

                    entries[titleIndex].Title = value;
                }
                else if (key.StartsWith("Length", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(key[6..], out int lengthIndex))
                {
                    if (!entries.ContainsKey(lengthIndex))
                        entries[lengthIndex] = new PlaylistEntry();

                    int.TryParse(value, out int len);
                    entries[lengthIndex].LengthSeconds = len;
                }
            }

            var sortedEntries = entries.Keys.OrderBy(k => k).Select(k => entries[k]).ToList();
            playlist.AddRange(sortedEntries);

            return playlist;
        }

        public static void Save(Playlist playlist, string path)
        {
            using var writer = new StreamWriter(path);

            writer.WriteLine("[playlist]");
            writer.WriteLine($"NumberOfEntries={playlist.Count}");

            for (int i = 0; i < playlist.Count; i++)
            {
                var entry = playlist.Entries[i];
                int num = i + 1;

                writer.WriteLine($"File{num}={entry.FilePath}");
                writer.WriteLine($"Title{num}={entry.Title}");
                writer.WriteLine($"Length{num}={entry.LengthSeconds}");
            }

            writer.WriteLine("Version=2");
        }
    }
}
