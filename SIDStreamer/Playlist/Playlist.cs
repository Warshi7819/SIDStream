namespace SIDStreamer.Playlists
{
    public class Playlist
    {
        private readonly List<PlaylistEntry> _entries = new();
        private int _currentIndex = -1;

        public IReadOnlyList<PlaylistEntry> Entries => _entries;
        public string? FilePath { get; set; }

        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (value != _currentIndex)
                {
                    _currentIndex = value;
                    CurrentTrackChanged?.Invoke(this, _currentIndex);
                }
            }
        }

        public int Count => _entries.Count;

        public bool HasCurrentTrack => _currentIndex >= 0 && _currentIndex < _entries.Count;

        public PlaylistEntry? CurrentTrack =>
            HasCurrentTrack ? _entries[_currentIndex] : null;

        public event EventHandler? PlaylistChanged;
        public event EventHandler<int>? CurrentTrackChanged;

        public void Add(PlaylistEntry entry)
        {
            _entries.Add(entry);
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddRange(IEnumerable<PlaylistEntry> entries)
        {
            _entries.AddRange(entries);
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return;

            _entries.RemoveAt(index);

            if (_currentIndex >= _entries.Count)
                _currentIndex = _entries.Count - 1;

            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveUp(int index)
        {
            if (index <= 0 || index >= _entries.Count)
                return;

            var item = _entries[index];
            _entries[index] = _entries[index - 1];
            _entries[index - 1] = item;

            if (_currentIndex == index)
                _currentIndex--;
            else if (_currentIndex == index - 1)
                _currentIndex++;

            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveDown(int index)
        {
            if (index < 0 || index >= _entries.Count - 1)
                return;

            var item = _entries[index];
            _entries[index] = _entries[index + 1];
            _entries[index + 1] = item;

            if (_currentIndex == index)
                _currentIndex++;
            else if (_currentIndex == index + 1)
                _currentIndex--;

            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveRangeUp(int firstIndex, int count)
        {
            if (firstIndex <= 0 || firstIndex + count > _entries.Count)
                return;

            for (int i = 0; i < count; i++)
            {
                int swapIdx = firstIndex - 1 + i;
                (_entries[swapIdx], _entries[swapIdx + 1]) = (_entries[swapIdx + 1], _entries[swapIdx]);
            }

            AdjustCurrentIndexForRangeMove(firstIndex, count, -1);
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveRangeDown(int firstIndex, int count)
        {
            if (firstIndex < 0 || firstIndex + count >= _entries.Count)
                return;

            for (int i = count - 1; i >= 0; i--)
            {
                int swapIdx = firstIndex + i;
                (_entries[swapIdx], _entries[swapIdx + 1]) = (_entries[swapIdx + 1], _entries[swapIdx]);
            }

            AdjustCurrentIndexForRangeMove(firstIndex, count, 1);
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        private void AdjustCurrentIndexForRangeMove(int firstIndex, int count, int direction)
        {
            if (_currentIndex < 0) return;

            int lastIndex = firstIndex + count - 1;

            if (_currentIndex >= firstIndex && _currentIndex <= lastIndex)
            {
                _currentIndex += direction;
            }
            else if (direction < 0 && _currentIndex == firstIndex - 1)
            {
                _currentIndex = lastIndex;
            }
            else if (direction > 0 && _currentIndex == lastIndex + 1)
            {
                _currentIndex = firstIndex;
            }
        }

        public void Clear()
        {
            _entries.Clear();
            _currentIndex = -1;
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
