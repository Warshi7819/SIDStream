using sidplay;
using SIDStream;
using SIDStreamer.Controls;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace SIDStreamer
{
    public partial class SIDstreamer : Form
    {
        private MonoSidPlayer player;
        private SidTune? tune;
        private string? pathToTune;

        // Logo fields
        private Bitmap? logoOriginal;
        private Bitmap? logoScaled;
        private Point logoPosition = Point.Empty;
        // If null => use original size. Otherwise use specified size (maintains aspect if one dimension is 0).
        private Size? logoSize = null;

        // Background fields (new)
        private Bitmap? bgOriginal;
        private Bitmap? bgScaled;
        // Null => original size, otherwise use specified size (preserve aspect when one dim is 0)
        private Size? bgSize = null;

        // P/Invoke to start a window drag from a client-area mouse down
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x02;

        private string currentSkin = "SteamPunk";

        Dictionary<string, EventHandler> _buttonHandlers;
        Dictionary<string, EventHandler> _trackBarHandlers;
        Dictionary<string, System.Windows.Forms.Label> _labels;
        Dictionary<string, NoFocusTrackBar> _trackBars;

        // <summary>
        // Constructor - Initializes a new instance of the SIDstreamer form.
        // </summary>
        public SIDstreamer()
        {
            InitializeComponent();

            // Define button handlers to match the ones in the skin settings. 
            _buttonHandlers = new Dictionary<string, EventHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "play", playButton_Click },
                { "settings", settingsButton_Click },
                { "stop", stopButton_Click },
                { "close", closeButton_Click },
                { "previous", prevButton_Click},
                { "next", nextButton_Click },
                { "playlist", playlistButton_Click},
                { "open", openFileButton_Click}
            };

            _trackBarHandlers = new Dictionary<string, EventHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "volume", TrackBar1_ValueChanged }
            };

            _labels = new Dictionary<string, System.Windows.Forms.Label>();
            _trackBars = new Dictionary<string, NoFocusTrackBar>();

            // Load icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo", "simple.ico");
            this.Icon = new Icon(iconPath);

            // Reduce flicker by enabling double buffering and controlling painting.
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;

            // Hide until we've applied shape/background
            Opacity = 0;

            // Apply shape during Load (occurs before first show) instead of Shown to avoid the rectangular flash.
            this.Load += SIDstreamer_Load;

            // Allow dragging by holding left mouse button anywhere on the form client area
            // (controls will still receive their own mouse events and won't trigger this).
            this.MouseDown += SIDstreamer_MouseDown;

            player = new MonoSidPlayer(true);
            player.setVolume(0.5f);

            // If HVSC (High Voltage Sid Collection) is set, then load song length database.
            
        }

        /// <summary>
        /// Load the logo from a relative path (e.g. "logo/logo.png").
        /// Call this before showing the form or at runtime. Default size = original image size.
        /// </summary>
        /// <param name="relativePath">Relative path under app base/current directory.</param>
        public void LoadLogo(string relativePath)
        {
            // Dispose existing
            logoOriginal?.Dispose();
            logoOriginal = null;
            logoScaled?.Dispose();
            logoScaled = null;
            logoSize = null;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, relativePath);

            if (!File.Exists(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (!File.Exists(path))
                return;

            using var src = new Bitmap(path);
            logoOriginal = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(logoOriginal))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }

            // default position: upper-left corner (0,0)
            logoPosition = new Point(0, 0);

            // no scaling by default -> use original on draw
            logoSize = null;

            Invalidate();
        }

        /// <summary>
        /// Set the logo's top-left position in client coordinates and request redraw.
        /// </summary>
        public void SetLogoPosition(int x, int y)
        {
            logoPosition = new Point(x, y);
            Invalidate();
        }

        /// <summary>
        /// Set the desired logo size. Pass (0,0) or null to revert to original size.
        /// If one dimension is zero the other will be calculated to preserve aspect ratio.
        /// </summary>
        public void SetLogoSize(int? width, int? height)
        {
            if (logoOriginal == null)
                return;

            if ((width == null && height == null) || (width == 0 && height == 0))
            {
                // revert to original size
                logoSize = null;
                logoScaled?.Dispose();
                logoScaled = null;
                Invalidate();
                return;
            }

            int origW = logoOriginal.Width;
            int origH = logoOriginal.Height;

            int targetW = width ?? 0;
            int targetH = height ?? 0;

            if (targetW <= 0 && targetH > 0)
            {
                // compute width preserving aspect
                targetW = Math.Max(1, (int)Math.Round(origW * (targetH / (double)origH)));
            }
            else if (targetH <= 0 && targetW > 0)
            {
                // compute height preserving aspect
                targetH = Math.Max(1, (int)Math.Round(origH * (targetW / (double)origW)));
            }
            else if (targetW <= 0 && targetH <= 0)
            {
                logoSize = null;
                logoScaled?.Dispose();
                logoScaled = null;
                Invalidate();
                return;
            }

            // Create scaled bitmap cache
            var scaled = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(logoOriginal, new Rectangle(0, 0, targetW, targetH), new Rectangle(0, 0, origW, origH), GraphicsUnit.Pixel);
            }

            logoScaled?.Dispose();
            logoScaled = scaled;
            logoSize = new Size(targetW, targetH);
            Invalidate();
        }

        // <summary>
        // Auto scale fonts/labels based on the resolution and DPI setting my dev machine
        // had at the time of development. (2560x1600 at 200% scaling → 192 DPI)
        // </summary>
        public void scaleLabelForResolution(System.Windows.Forms.Label lbl)
        {
            // Baseline resolution (your design reference)
            const float refW = 2560f;
            const float refH = 1600f;

            // Baseline DPI (your dev machine at 200% scaling → 192)
            const float refDpi = 192f;

            // Current resolution
            float curW = Screen.PrimaryScreen.Bounds.Width;
            float curH = Screen.PrimaryScreen.Bounds.Height;

            // Axis scale factors relative to the reference
            float scaleX = curW / refW;
            float scaleY = curH / refH;

            // Current DPI
            float dpi;
            using (Graphics g = this.CreateGraphics())
                dpi = g.DpiY;

            // Inverse DPI factor: lower DPI → larger fonts
            float dpiFactor = refDpi / dpi;

            // Final font scale = resolution scaling * inverse DPI scaling
            float fontScale = ((scaleX + scaleY) / 2f) * dpiFactor;

            // Clamp near baseline
            if (Math.Abs(fontScale - 1.0f) < 0.05f)
                fontScale = 1.0f;

            // Apply scaled font
            lbl.Font = new Font(lbl.Font.FontFamily, lbl.Font.Size * fontScale, lbl.Font.Style);

            // Scale position
            lbl.Location = new Point(
                (int)Math.Round(lbl.Location.X * scaleX),
                (int)Math.Round(lbl.Location.Y * scaleY)
            );

            // Optionally scale bounding box
            lbl.Size = new Size(
                (int)Math.Round(lbl.Size.Width * scaleX),
                (int)Math.Round(lbl.Size.Height * scaleY)
            );
        }

        /// <summary>
        /// Load the background from a relative path (e.g. "skins/christmas.png").
        /// Default draws original size unless SetBackgroundSize is used before drawing.
        /// </summary>
        public void LoadBackground(string relativePath)
        {
            bgOriginal?.Dispose();
            bgOriginal = null;
            bgScaled?.Dispose();
            bgScaled = null;
            bgSize = null;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, relativePath);

            if (!File.Exists(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (!File.Exists(path))
                return;

            using var src = new Bitmap(path);
            bgOriginal = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bgOriginal))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
        }

        /// <summary>
        /// Set the desired background size. Call before the form is shown to avoid visual resizing artifacts.
        /// Pass (0,0) or null to revert to original size.
        /// If one dimension is zero the other will be calculated to preserve aspect ratio.
        /// </summary>
        public void SetBackgroundSize(int? width, int? height)
        {
            if (bgOriginal == null)
                return;

            if ((width == null && height == null) || (width == 0 && height == 0))
            {
                bgSize = null;
                bgScaled?.Dispose();
                bgScaled = null;
                return;
            }

            int origW = bgOriginal.Width;
            int origH = bgOriginal.Height;

            int targetW = width ?? 0;
            int targetH = height ?? 0;

            if (targetW <= 0 && targetH > 0)
            {
                targetW = Math.Max(1, (int)Math.Round(origW * (targetH / (double)origH)));
            }
            else if (targetH <= 0 && targetW > 0)
            {
                targetH = Math.Max(1, (int)Math.Round(origH * (targetW / (double)origW)));
            }
            else if (targetW <= 0 && targetH <= 0)
            {
                bgSize = null;
                bgScaled?.Dispose();
                bgScaled = null;
                return;
            }

            var scaled = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(bgOriginal, new Rectangle(0, 0, targetW, targetH), new Rectangle(0, 0, origW, origH), GraphicsUnit.Pixel);
            }

            bgScaled?.Dispose();
            bgScaled = scaled;
            bgSize = new Size(targetW, targetH);
        }

        // <summary>
        // Enable window dragging from client area
        // </summary>
        private void SIDstreamer_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

    
        // <summary>
        // Load skin settings from JSON file
        // </summary>
        private SkinSettings? loadSkinSettings(string skinPath)
        {
            SkinSettings? deserializedSkin = JsonSerializer.Deserialize<SkinSettings>(File.ReadAllText(skinPath));
            return deserializedSkin;
        }

        // <summary>
        // Convert hex color string to Color
        // </summary>
        internal Color hexToColor(string hex)
        {

            if (hex.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            {
                return Color.Transparent;
            }

            // Fjerner eventuell leading '#'
            hex = hex.Replace("#", "");

            if (hex.Length == 3)
            {
                // Kortform (#RGB → #RRGGBB)
                hex = string.Concat(
                    hex[0], hex[0],
                    hex[1], hex[1],
                    hex[2], hex[2]
                );
            }

            if (hex.Length != 6)
                throw new ArgumentException("Hex Color code must be 3 or 6 chars long.");

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }

        // <summary>
        // Get the current skin name from skinsettings.json
        // </summary>
        private string getCurrentSkin()
        {
            SkinSettings? settings = loadSkinSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skinsettings.json"));
            return settings.skinName;
        }

        // <summary>
        // Form Load event handler - applies skin and initializes controls
        // </summary>
        private void SIDstreamer_Load(object? sender, EventArgs e)
        {
            try
            {

                this.currentSkin = getCurrentSkin(); 

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string skinDir = Path.Combine(baseDir, "skins", this.currentSkin);

                // Parse skin JSON file to get skin parameters
                var skinParser = new UiSkinParser(Path.Combine(skinDir, "skin.json"));

                // Suspend layout and apply shape before first paint
                SuspendLayout();

                // Load background into managed fields so we can scale before draw/region creation
                LoadBackground(Path.Combine(skinDir, skinParser.BgImage));

                // you can call SetBackgroundSize(...) here before ApplyImageShape if you want to pre-scale:
                // e.g. SetBackgroundSize(800, 0); // preserve aspect by width

                ApplyImageShapeFromLoadedBackground();

                // Traverse Buttons
                foreach (var (name, btn) in skinParser.GetButtons())
                {

                    var butt = new SIDStreamer.Controls.ImageButton();
                    butt.Location = new Point(UiSkinParser.Int(btn, "x-pos"), UiSkinParser.Int(btn, "y-pos"));
                    butt.Size = new Size(UiSkinParser.Int(btn, "width"), UiSkinParser.Int(btn, "height"));
                    butt.NormalImage = Image.FromFile(Path.Combine(skinDir, UiSkinParser.Str(btn, "image")));
                    butt.HoverImage = Image.FromFile(Path.Combine(skinDir, UiSkinParser.Str(btn, "hover-image")));
                    butt.PressedImage = Image.FromFile(Path.Combine(skinDir, UiSkinParser.Str(btn, "pressed-image")));

                    if (_buttonHandlers.TryGetValue(name, out var handler))
                    {
                        butt.Click += handler;
                    }

                    Controls.Add(butt);
                }

                // Traverse Labels
                foreach (var (name, lbl) in skinParser.GetLabels())
                {
                    var label = new System.Windows.Forms.Label();
                    label.Text = UiSkinParser.Str(lbl, "text");
                    label.Location = new Point(UiSkinParser.Int(lbl, "x-pos"), UiSkinParser.Int(lbl, "y-pos"));
                    label.BackColor = this.hexToColor(UiSkinParser.Str(lbl, "bg-color"));
                    label.ForeColor = this.hexToColor(UiSkinParser.Str(lbl, "fg-color"));
                    label.AutoSize = true;
                    scaleLabelForResolution(label);

                    _labels.Add(name, label);
                    Controls.Add(label);
                }

                // Images
                foreach (var (name, img) in skinParser.GetImages())
                {
                    LoadLogo(Path.Combine(skinDir, UiSkinParser.Str(img, "file")));
                    SetLogoPosition(UiSkinParser.Int(img, "x-pos"), UiSkinParser.Int(img, "y-pos"));
                    SetLogoSize(UiSkinParser.Int(img, "width"), UiSkinParser.Int(img, "height")); // preserve aspect by width
                }

                // TrackBars
                foreach (var (name, tb) in skinParser.GetTrackBars())
                {
                    var trackBar = new NoFocusTrackBar();
                    trackBar.Location = new Point(UiSkinParser.Int(tb, "x-pos"), UiSkinParser.Int(tb, "y-pos"));
                    trackBar.LargeChange = UiSkinParser.Int(tb, "large-change"); 
                    trackBar.Value = UiSkinParser.Int(tb, "value");
                    trackBar.Size = new Size(UiSkinParser.Int(tb, "width"), UiSkinParser.Int(tb, "height"));
                    trackBar.BackColor = this.hexToColor(UiSkinParser.Str(tb, "bg-color"));
                    
                    if (_trackBarHandlers.TryGetValue(name, out var handler))
                    {
                        trackBar.Click += handler;
                    }

                    _trackBars.Add(name, trackBar);
                    Controls.Add(trackBar);
                }

                ResumeLayout();

                // Show the form now that shape/background is applied
                Opacity = 1;

            }
            catch
            {
                // swallow — don't block startup if shaping fails
                Opacity = 1;
            }
        }

        // <summary>
        // Load the SID tune from the specified path
        // </summary>
        private void loadTune()
        {
            if (!string.IsNullOrEmpty(this.pathToTune))
            {
                if (this.tune != null)
                {
                    this.player.stop();
                    this.tune = null;
                }


                using (FileStream file = new FileStream(this.pathToTune, FileMode.Open, FileAccess.Read))
                {
                    this.tune = new SidTune(file);

                    _labels["info"].Text = "Author: " + this.tune.Info.InfoString2;
                    _labels["info"].Text += Environment.NewLine + "Title: " + this.tune.Info.InfoString1;
                    _labels["info"].Text += Environment.NewLine + "Released: " + this.tune.Info.InfoString3;
                }

                this.updateCurrentSong();
            }
        }

        // <summary>
        // Volume slider value changed event handler
        // </summary>
        private void TrackBar1_ValueChanged(object? sender, EventArgs e)
        {
            float vol = (float) _trackBars["volume"].Value;
            if (vol > 0.0)
            {
                if (vol > 9.0) 
                {
                    vol = 9.0f;
                }
                vol = vol / 10;
            }

            player.setVolume(vol);
        }

        // <summary>
        // Play button click event handler
        // </summary>
        private void playButton_Click(object? sender, EventArgs e)
        {
            if (this.tune != null)
            {
                player.stop();
                player.Start(tune);
            }
        }

        // <summary>
        // Settings button click event handler
        // </summary>
        private void settingsButton_Click(object? sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            var result = settingsForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                if (this.currentSkin != settingsForm.newlySelectedSkin)
                {
                    this.currentSkin = settingsForm.newlySelectedSkin;
                 
                    // terminate playback
                    this.player.stop();

                    // Write to skinsettings.json so that new skin is loaded next time  
                    SkinSettings settings = new SkinSettings
                    {
                        skinName = this.currentSkin
                    };
                    string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skinsettings.json"), jsonString);
                    // Restart application to apply new skin
                    Application.Restart();
                }
            }
            settingsForm.Dispose();
        }

        // <summary>
        // Stop button click event handler
        // </summary>
        private void stopButton_Click(object? sender, EventArgs e)
        {
            player.stop();
            if (tune != null)
            {
                tune.Info.currentSong = 1;
            }
            this.updateCurrentSong();
        }

        // <summary>
        // Close button click event handler
        // </summary>
        private void closeButton_Click(object? sender, EventArgs e)
        {
            player.stop();
            this.Close();
        }

        // <summary>
        // Previous button click event handler
        // </summary>
        private void prevButton_Click(object? sender, EventArgs e)
        {
            if (this.tune != null)
            {
                if (tune.Info.currentSong > 1)
                {

                    switch (player.State)
                    {
                        case SID2Types.sid2_player_t.sid2_playing:
                        case SID2Types.sid2_player_t.sid2_paused:
                            player.stop();
                            break;
                    }

                    tune.Info.currentSong--;

                    player.Start(tune, tune.Info.currentSong);

                    this.updateCurrentSong();


                }
            }
        }

        // <summary>
        // Next button click event handler
        // </summary>
        private void nextButton_Click(object? sender, EventArgs e)
        {
            if (this.tune != null)
            {
                if (this.tune.Info.currentSong < this.tune.Info.songs)
                {

                    switch (this.player.State)
                    {
                        case SID2Types.sid2_player_t.sid2_playing:
                        case SID2Types.sid2_player_t.sid2_paused:
                            player.stop();
                            break;
                    }

                    tune.Info.currentSong++;


                    player.Start(tune, tune.Info.currentSong);
                    this.updateCurrentSong();
                }
            }
        }

        // <summary>
        // Playlist button click event handler
        // </summary>
        private void playlistButton_Click(object? sender, EventArgs e)
        {
            ;
        }



        // <summary>
        // Update the current song of SID file label
        // </summary>
        private void updateCurrentSong()
        {

            if (this.tune != null)
            {
                int tmp = this.tune.Info.currentSong;
                if (tmp == 0) { tmp = 1; }

                if (tmp < 10)
                {
                    _labels["currentTrack"].Text = "0" + tmp + " / ";
                }
                else
                {
                    _labels["currentTrack"].Text = tmp + " / ";
                }

                if (this.tune.Info.songs < 10)
                {
                    _labels["currentTrack"].Text += "0" + this.tune.Info.songs;
                }
                else
                {
                    _labels["currentTrack"].Text += this.tune.Info.songs;
                }
            }
            else
            {
                _labels["currentTrack"].Text = "00 / 00";
            }
        }

        // <summary>
        // Open file button click event handler
        // </summary>
        private void openFileButton_Click(object? sender, EventArgs e)
        {
            openFileDialog1.Title = "Select a File";
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog1.Filter = "SID Files (*.sid)|*.sid|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog1.FileName;
                this.pathToTune = filePath;
                _labels["media"].Text = Path.GetFileName(filePath);
                this.loadTune();
                player.Start(tune);
            }
            else
            {
                ;
            }
        }

        // <summary>
        // Custom paint background to draw pre-scaled images
        // Prevent the default background erase to avoid a white flash AND white boarder around the image.
        // </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Bitmap? toDraw = bgScaled ?? bgOriginal;

            if (toDraw == null)
            {
                base.OnPaintBackground(e);
                return;
            }

            // Draw the pre-scaled background image directly (avoids the default background clear)
            e.Graphics.DrawImage(toDraw, 0, 0, toDraw.Width, toDraw.Height);

            // Draw the logo on top if present
            if (logoOriginal != null)
            {
                if (logoScaled != null)
                {
                    e.Graphics.DrawImage(logoScaled, logoPosition.X, logoPosition.Y, logoScaled.Width, logoScaled.Height);
                }
                else
                {
                    e.Graphics.DrawImage(logoOriginal, logoPosition.X, logoPosition.Y, logoOriginal.Width, logoOriginal.Height);
                }
            }
        }

        /// <summary>
        /// Uses the already-loaded background (bgOriginal/bgScaled) to build the window Region and set the BackgroundImage.
        /// This ensures the image is scaled before being assigned/drawn so no runtime resize flicker occurs.
        /// </summary>
        private void ApplyImageShapeFromLoadedBackground(byte alphaThreshold = 10)
        {
            Bitmap? bmp = bgScaled ?? bgOriginal;
            if (bmp == null)
                return;

            // Build region from opaque spans per row using LockBits
            var gp = new GraphicsPath();
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* scan0 = (byte*)data.Scan0;
                    int stride = data.Stride;
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        int x = 0;
                        byte* row = scan0 + y * stride;
                        while (x < bmp.Width)
                        {
                            // skip transparent pixels
                            while (x < bmp.Width)
                            {
                                byte alpha = row[x * 4 + 3];
                                if (alpha > alphaThreshold) break;
                                x++;
                            }
                            if (x >= bmp.Width) break;
                            int xStart = x;
                            // find opaque run end
                            while (x < bmp.Width)
                            {
                                byte alpha = row[x * 4 + 3];
                                if (alpha <= alphaThreshold) break;
                                x++;
                            }
                            int xEnd = x;
                            gp.AddRectangle(new Rectangle(xStart, y, xEnd - xStart, 1));
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            // Apply the image and region to the form (use the pre-scaled image)
            this.BackgroundImage = bmp;
            this.BackgroundImageLayout = ImageLayout.None;
            this.ClientSize = bmp.Size;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Dispose previous region and set new region
            this.Region?.Dispose();
            this.Region = new Region(gp);
        }

        // <summary>
        // Dispose managed resources
        // </summary>
        private void DisposeManagedResources()
        {
            // called by Designer Dispose
            logoOriginal?.Dispose();
            logoScaled?.Dispose();

            bgOriginal?.Dispose();
            bgScaled?.Dispose();
        }
    }
}
