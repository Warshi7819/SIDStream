using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using SIDStreamer.Playlists;

namespace SIDStreamer
{
    public partial class PlaylistForm : Form
    {
        private readonly Playlist _playlist = new();
        private readonly string _c64MusicPath;
        private readonly SongLengthDatabase _songLengths;
        private List<string> _allSidFiles = new();
        private bool _suppressAfterCheck;

        public Playlist CurrentPlaylist => _playlist;

        public event EventHandler<int>? TrackSelected;

        public PlaylistForm(string hvscPath, string skinDir)
        {
            InitializeComponent();

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo", "simple.ico");
            this.Icon = new Icon(iconPath);

            ApplySkinColors(skinDir);

            _c64MusicPath = hvscPath;
            _songLengths = new SongLengthDatabase(_c64MusicPath);

            WireEvents();
            PopulateBrowserAsync();
        }

        private void WireEvents()
        {
            _playlist.PlaylistChanged += (_, _) => RefreshPlaylistUI();

            browserTree.AfterSelect += BrowserTree_AfterSelect;
            browserTree.AfterCheck += BrowserTree_AfterCheck;
            browserTree.NodeMouseDoubleClick += BrowserTree_NodeMouseDoubleClick;

            addButton.Click += AddButton_Click;
            addFileButton.Click += AddFileButton_Click;
            searchButton.Click += SearchButton_Click;
            removeButton.Click += RemoveButton_Click;
            moveUpButton.Click += MoveUpButton_Click;
            moveDownButton.Click += MoveDownButton_Click;
            clearButton.Click += ClearButton_Click;

            playlistList.SelectedIndexChanged += PlaylistList_SelectedIndexChanged;
            playlistList.DoubleClick += PlaylistList_DoubleClick;

            loadMenuItem.Click += LoadMenuItem_Click;
            saveMenuItem.Click += SaveMenuItem_Click;
            saveAsMenuItem.Click += SaveAsMenuItem_Click;

            this.FormClosing += PlaylistForm_FormClosing;
        }

        private void PopulateBrowserAsync()
        {
            if (string.IsNullOrWhiteSpace(_c64MusicPath) || !Directory.Exists(_c64MusicPath))
            {
                statusLabel.Text = string.IsNullOrWhiteSpace(_c64MusicPath)
                    ? "HVSC path not configured. Use Settings to set the HVSC directory."
                    : $"HVSC directory not found at: {_c64MusicPath}";
                return;
            }

            browserTree.BeginUpdate();
            var rootNode = new TreeNode("C64Music") { Tag = _c64MusicPath };
            var loadingNode = new TreeNode("Loading...") { Tag = null };
            rootNode.Nodes.Add(loadingNode);
            browserTree.Nodes.Add(rootNode);
            rootNode.Expand();
            browserTree.EndUpdate();

            Task.Run(() =>
            {
                var topDirs = Directory.GetDirectories(_c64MusicPath).OrderBy(d => d).ToList();
                int fileCount = 0;

                foreach (var dir in topDirs)
                {
                    var dirNode = new TreeNode(Path.GetFileName(dir)) { Tag = dir };
                    PopulateDirectoryNode(dirNode, dir);

                    if (dirNode.Nodes.Count == 0 && !HasSidFiles(dir))
                        continue;

                    this.BeginInvoke(() =>
                    {
                        rootNode.Nodes.Remove(loadingNode);
                        rootNode.Nodes.Add(dirNode);
                        addButton.Enabled = HasCheckedFiles();
                    });
                }

                try
                {
                    _allSidFiles = Directory.GetFiles(_c64MusicPath, "*.sid", SearchOption.AllDirectories).OrderBy(f => f).ToList();
                    fileCount = _allSidFiles.Count;
                }
                catch { /* ignore */ }

                this.BeginInvoke(() =>
                {
                    rootNode.Expand();
                    statusLabel.Text = $"Browse: {fileCount} SID files found";
                });
            });
        }

        private void PopulateDirectoryNode(TreeNode parentNode, string dirPath)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(dirPath).OrderBy(d => d))
                {
                    var dirNode = new TreeNode(Path.GetFileName(dir)) { Tag = dir };
                    PopulateDirectoryNode(dirNode, dir);
                    if (dirNode.Nodes.Count > 0 || HasSidFiles(dir))
                        parentNode.Nodes.Add(dirNode);
                }

                foreach (var file in Directory.GetFiles(dirPath, "*.sid").OrderBy(f => f))
                {
                    var fileNode = new TreeNode(Path.GetFileName(file)) { Tag = file };
                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // skip inaccessible directories
            }
        }

        private static bool HasSidFiles(string dirPath)
        {
            try
            {
                return Directory.GetFiles(dirPath, "*.sid").Length > 0;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static List<string> CollectCheckedFiles(TreeNodeCollection nodes)
        {
            var result = new List<string>();
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is string path && File.Exists(path))
                    result.Add(path);
                result.AddRange(CollectCheckedFiles(node.Nodes));
            }
            return result;
        }

        private static void ClearAllChecks(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = false;
                ClearAllChecks(node.Nodes);
            }
        }

        private bool HasCheckedFiles()
        {
            return HasCheckedFilesRecursive(browserTree.Nodes);
        }

        private static bool HasCheckedFilesRecursive(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is string path && File.Exists(path))
                    return true;
                if (HasCheckedFilesRecursive(node.Nodes))
                    return true;
            }
            return false;
        }

        private void BrowserTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            addButton.Enabled = HasCheckedFiles();
        }

        private void BrowserTree_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (_suppressAfterCheck) return;

            _suppressAfterCheck = true;
            try
            {
                if (e.Node is not { } node) { return; }
            SetChildChecks(node, node.Checked);
            }
            finally
            {
                _suppressAfterCheck = false;
            }

            addButton.Enabled = HasCheckedFiles();
        }

        private static void SetChildChecks(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                SetChildChecks(child, isChecked);
            }
        }

        private void BrowserTree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is string path && File.Exists(path))
            {
                AddFileToPlaylist(path);
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            var files = CollectCheckedFiles(browserTree.Nodes);
            if (files.Count == 0) return;

            var entries = files.Select(f =>
            {
                var entry = new PlaylistEntry(f);
                EnrichWithSongLength(entry);
                return entry;
            }).ToList();
            _playlist.AddRange(entries);

            _suppressAfterCheck = true;
            ClearAllChecks(browserTree.Nodes);
            _suppressAfterCheck = false;

            addButton.Enabled = false;
            statusLabel.Text = $"Added {entries.Count} track(s)";
        }

        private void AddFileButton_Click(object? sender, EventArgs e)
        {
            openFileDialog.Title = "Add SID File";
            openFileDialog.Filter = "SID Files (*.sid)|*.sid|All Files (*.*)|*.*";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    AddFileToPlaylist(file);
                }
            }
        }

        private void AddFileToPlaylist(string filePath)
        {
            var entry = new PlaylistEntry(filePath);
            EnrichWithSongLength(entry);
            _playlist.Add(entry);
            statusLabel.Text = $"Added: {entry.Title}";
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            var indices = playlistList.SelectedIndices;
            if (indices.Count == 0) return;

            var sorted = new List<int>();
            foreach (int idx in indices) sorted.Add(idx);
            sorted.Sort();
            sorted.Reverse();

            foreach (int idx in sorted)
            {
                _playlist.RemoveAt(idx);
            }

            statusLabel.Text = $"Removed {sorted.Count} track(s)";
        }

        private void MoveUpButton_Click(object? sender, EventArgs e)
        {
            var indices = GetSortedSelectedIndices();
            if (indices.Count == 0 || indices[0] == 0) return;

            int first = indices[0];
            int count = indices.Count;

            if (!IsContiguous(indices)) return;

            _playlist.MoveRangeUp(first, count);
            ReapplySelection(Enumerable.Range(first - 1, count).ToList());
        }

        private void MoveDownButton_Click(object? sender, EventArgs e)
        {
            var indices = GetSortedSelectedIndices();
            if (indices.Count == 0 || indices[^1] >= _playlist.Count - 1) return;

            int first = indices[0];
            int count = indices.Count;

            if (!IsContiguous(indices)) return;

            _playlist.MoveRangeDown(first, count);
            ReapplySelection(Enumerable.Range(first + 1, count).ToList());
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            if (_playlist.Count == 0) return;

            var result = MessageBox.Show(
                "Clear all entries from the playlist?",
                "Confirm Clear",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _playlist.Clear();
                _playlist.FilePath = null;
                statusLabel.Text = "Playlist cleared";
            }
        }

        private void PlaylistList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var indices = GetSortedSelectedIndices();
            bool hasSelection = indices.Count > 0;

            removeButton.Enabled = hasSelection;
            moveUpButton.Enabled = hasSelection && indices[0] > 0;
            moveDownButton.Enabled = hasSelection && indices[^1] < _playlist.Count - 1;
        }

        private void PlaylistList_DoubleClick(object? sender, EventArgs e)
        {
            int idx = playlistList.SelectedIndex;
            if (idx >= 0)
            {
                _playlist.CurrentIndex = idx;
                TrackSelected?.Invoke(this, idx);
            }
        }

        private void LoadMenuItem_Click(object? sender, EventArgs e)
        {
            if (_playlist.Count > 0)
            {
                var result = MessageBox.Show(
                    "Loading a playlist will replace the current one. Continue?",
                    "Confirm Load",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var loaded = PlsParser.Load(openFileDialog.FileName);
                    _playlist.Clear();
                    foreach (var entry in loaded.Entries)
                        _playlist.Add(entry);
                    _playlist.FilePath = loaded.FilePath;
                    _playlist.CurrentIndex = -1;
                    statusLabel.Text = $"Loaded: {Path.GetFileName(loaded.FilePath)} ({_playlist.Count} tracks)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load playlist:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveMenuItem_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_playlist.FilePath))
            {
                SavePlaylistTo(_playlist.FilePath);
            }
            else
            {
                SaveAsMenuItem_Click(sender, e);
            }
        }

        private void SaveAsMenuItem_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_playlist.FilePath))
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(_playlist.FilePath);

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SavePlaylistTo(saveFileDialog.FileName);
                _playlist.FilePath = saveFileDialog.FileName;
            }
        }

        private void SavePlaylistTo(string path)
        {
            try
            {
                PlsParser.Save(_playlist, path);
                statusLabel.Text = $"Saved: {Path.GetFileName(path)} ({_playlist.Count} tracks)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save playlist:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshPlaylistUI()
        {
            playlistList.BeginUpdate();
            playlistList.Items.Clear();

            foreach (var entry in _playlist.Entries)
            {
                playlistList.Items.Add(entry);
            }

            playlistList.EndUpdate();

            bool hasItems = _playlist.Count > 0;
            clearButton.Enabled = hasItems;

            if (_playlist.HasCurrentTrack)
                playlistList.SelectedIndex = _playlist.CurrentIndex;

            PlaylistList_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private List<int> GetSortedSelectedIndices()
        {
            var sorted = new List<int>();
            foreach (int idx in playlistList.SelectedIndices)
                sorted.Add(idx);
            sorted.Sort();
            return sorted;
        }

        private static bool IsContiguous(List<int> sortedIndices)
        {
            if (sortedIndices.Count <= 1) return true;
            for (int i = 1; i < sortedIndices.Count; i++)
            {
                if (sortedIndices[i] != sortedIndices[i - 1] + 1)
                    return false;
            }
            return true;
        }

        private void ReapplySelection(List<int> indices)
        {
            playlistList.ClearSelected();
            foreach (int idx in indices)
            {
                if (idx >= 0 && idx < playlistList.Items.Count)
                    playlistList.SetSelected(idx, true);
            }
        }

        private void PlaylistForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void EnrichWithSongLength(PlaylistEntry entry)
        {
            var songEntry = _songLengths.LookupByPath(entry.FilePath);

            if (songEntry == null && File.Exists(entry.FilePath))
            {
                var hashBytes = MD5.HashData(File.ReadAllBytes(entry.FilePath));
                var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
                songEntry = _songLengths.LookupByMd5(hashString);
            }

            if (songEntry != null && songEntry.Lengths.Count > 0)
            {
                entry.LengthSeconds = songEntry.PrimaryLengthSeconds;
                entry.LengthDisplay = songEntry.PrimaryLength;
            }
        }

        private void ApplySkinColors(string skinDir)
        {
            try
            {
                var skinJson = Path.Combine(skinDir, "skin.json");
                if (!File.Exists(skinJson)) return;

                var root = JsonNode.Parse(File.ReadAllText(skinJson));
                var playlist = root?["playlist"] as JsonObject;
                if (playlist == null) return;

                var bg = ParseHexColor(playlist["bg-color"]?.ToString());
                var fg = ParseHexColor(playlist["fg-color"]?.ToString());

                if (bg == null && fg == null) return;

                if (bg != null)
                {
                    this.BackColor = bg.Value;
                    browserTree.BackColor = bg.Value;
                    playlistList.BackColor = bg.Value;
                    searchBox.BackColor = bg.Value;
                    searchButton.BackColor = bg.Value;
                    addButton.BackColor = bg.Value;
                    addFileButton.BackColor = bg.Value;
                    removeButton.BackColor = bg.Value;
                    moveUpButton.BackColor = bg.Value;
                    moveDownButton.BackColor = bg.Value;
                    clearButton.BackColor = bg.Value;
                    statusStrip.BackColor = bg.Value;
                }

                if (fg != null)
                {
                    browserLabel.ForeColor = fg.Value;
                    playlistLabel.ForeColor = fg.Value;
                    browserTree.ForeColor = fg.Value;
                    playlistList.ForeColor = fg.Value;
                    searchBox.ForeColor = fg.Value;
                    searchButton.ForeColor = fg.Value;
                    addButton.ForeColor = fg.Value;
                    addFileButton.ForeColor = fg.Value;
                    removeButton.ForeColor = fg.Value;
                    moveUpButton.ForeColor = fg.Value;
                    moveDownButton.ForeColor = fg.Value;
                    clearButton.ForeColor = fg.Value;
                    statusStrip.ForeColor = fg.Value;
                }
            }
            catch
            {
                // Missing or malformed playlist section — fall back to default colors
            }
        }

        private static Color? ParseHexColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            if (hex.Equals("transparent", StringComparison.OrdinalIgnoreCase)) return null;

            hex = hex.Replace("#", "");
            if (hex.Length == 3)
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

            if (hex.Length != 6) return null;

            if (!int.TryParse(hex[..2], NumberStyles.HexNumber, null, out int r)) return null;
            if (!int.TryParse(hex[2..4], NumberStyles.HexNumber, null, out int g)) return null;
            if (!int.TryParse(hex[4..6], NumberStyles.HexNumber, null, out int b)) return null;

            return Color.FromArgb(r, g, b);
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            var searchText = searchBox.Text.Trim();
            if (_allSidFiles.Count == 0) return;

            if (string.IsNullOrEmpty(searchText))
            {
                RebuildTree(_allSidFiles);
                statusLabel.Text = $"Browse: {_allSidFiles.Count} SID files found";
                return;
            }

            var filtered = _allSidFiles
                .Where(f => Path.GetFileNameWithoutExtension(f).Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            RebuildTree(filtered);
            statusLabel.Text = $"Search: {filtered.Count} result(s) for '{searchText}'";
        }

        private void RebuildTree(List<string> paths)
        {
            browserTree.BeginUpdate();
            browserTree.Nodes.Clear();

            var rootNode = new TreeNode("HVSC") { Tag = _c64MusicPath };
            browserTree.Nodes.Add(rootNode);

            var c64MusicLower = _c64MusicPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                    + Path.DirectorySeparatorChar;

            foreach (var filePath in paths)
            {
                if (!filePath.StartsWith(c64MusicLower, StringComparison.OrdinalIgnoreCase))
                    continue;

                var relative = filePath[c64MusicLower.Length..];
                var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var current = rootNode;
                for (int i = 0; i < segments.Length; i++)
                {
                    var isFile = i == segments.Length - 1;
                    var name = segments[i];
                    var existing = current.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == name);

                    if (existing != null)
                    {
                        current = existing;
                    }
                    else
                    {
                        var nodePath = Path.Combine(c64MusicLower, Path.Combine(segments[..(i + 1)]));
                        var node = new TreeNode(name) { Tag = isFile ? filePath : nodePath };
                        current.Nodes.Add(node);
                        current = node;
                    }
                }
            }

            rootNode.Expand();
            foreach (TreeNode child in rootNode.Nodes)
                child.Expand();

            browserTree.EndUpdate();
        }
    }
}
