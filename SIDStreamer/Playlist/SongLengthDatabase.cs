namespace SIDStreamer.Playlists
{
    public class SongLengthEntry
    {
        public string Path { get; init; } = string.Empty;
        public string Md5Hash { get; init; } = string.Empty;
        public List<string> Lengths { get; init; } = new();

        public string PrimaryLength => Lengths.FirstOrDefault() ?? string.Empty;

        public int PrimaryLengthSeconds => ParseLengthToSeconds(PrimaryLength);

        public int GetLengthSeconds(int songIndex)
        {
            if (songIndex < 0 || songIndex >= Lengths.Count)
                return -1;
            return ParseLengthToSeconds(Lengths[songIndex]);
        }

        internal static int ParseLengthToSeconds(string length)
        {
            if (string.IsNullOrEmpty(length))
                return -1;

            var parts = length.Split(':');
            if (parts.Length != 2)
                return -1;

            if (!int.TryParse(parts[0], out int minutes))
                return -1;

            var secPart = parts[1];
            var dotIndex = secPart.IndexOf('.');
            if (dotIndex >= 0)
                secPart = secPart[..dotIndex];

            if (!int.TryParse(secPart, out int seconds))
                return -1;

            return minutes * 60 + seconds;
        }
    }

    public class SongLengthDatabase
    {
        private readonly Dictionary<string, SongLengthEntry> _byPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SongLengthEntry> _byMd5 = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _c64MusicBasePath;

        public int Count => _byMd5.Count;

        public SongLengthDatabase(string c64MusicPath)
        {
            _c64MusicBasePath = c64MusicPath;
            var md5Path = Path.Combine(c64MusicPath, "DOCUMENTS", "Songlengths.md5");
            if (File.Exists(md5Path))
                Parse(md5Path);
        }

        private void Parse(string filePath)
        {
            string? currentPath = null;

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line == "[Database]")
                    continue;

                if (line.StartsWith(';'))
                {
                    currentPath = line[1..].Trim();
                    continue;
                }

                if (currentPath == null)
                    continue;

                var eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                    continue;

                var md5 = line[..eqIndex].Trim();
                var lengthsRaw = line[(eqIndex + 1)..].Trim();
                var lengths = lengthsRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

                var entry = new SongLengthEntry
                {
                    Path = currentPath,
                    Md5Hash = md5,
                    Lengths = lengths
                };

                _byPath[currentPath] = entry;
                _byMd5[md5] = entry;

                currentPath = null;
            }
        }

        public SongLengthEntry? LookupByMd5(string md5Hash)
        {
            return _byMd5.TryGetValue(md5Hash, out var entry) ? entry : null;
        }

        public SongLengthEntry? LookupByPath(string fullWindowsPath)
        {
            var hvscPath = NormalizeToHvscPath(fullWindowsPath);
            if (hvscPath == null)
                return null;

            return _byPath.TryGetValue(hvscPath, out var entry) ? entry : null;
        }

        private string? NormalizeToHvscPath(string fullWindowsPath)
        {
            var c64MusicMarker = "C64Music";
            var idx = fullWindowsPath.IndexOf(c64MusicMarker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            var relative = fullWindowsPath[(idx + c64MusicMarker.Length)..];
            relative = relative.Replace('\\', '/');

            if (!relative.StartsWith('/'))
                relative = "/" + relative;

            return relative;
        }
    }
}
