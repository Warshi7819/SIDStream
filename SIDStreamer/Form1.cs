using sidplay;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

        public SIDstreamer()
        {
            InitializeComponent();

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

        private void SIDstreamer_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        private void SIDstreamer_Load(object? sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string imagePath = Path.Combine(baseDir, "skins", "christmas2.png");
                this.label2.Text = "No media selected ...";

                this.labelInfo.Text = "SIDstreamer v.1.0";

                // fallback when running from IDE
                if (!File.Exists(imagePath))
                    imagePath = Path.Combine(Directory.GetCurrentDirectory(), "skins", "christmas2.png");

                // Suspend layout and apply shape before first paint
                SuspendLayout();

                // Load background into managed fields so we can scale before draw/region creation
                LoadBackground(Path.Combine("skins", "christmas2.png"));

                // you can call SetBackgroundSize(...) here before ApplyImageShape if you want to pre-scale:
                // e.g. SetBackgroundSize(800, 0); // preserve aspect by width

                ApplyImageShapeFromLoadedBackground();

                // Load default logo if present
                LoadLogo(Path.Combine("logo", "logo.png"));
                SetLogoPosition(200, 160);
                SetLogoSize(200, 0); // preserve aspect by width

                ResumeLayout();

                // Show the form now that shape/background is applied
                Opacity = 1;

                // after LoadBackground/ApplyImageShape and before showing form add the image buttons
                var btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(370, 763);
                btn.Size = new Size(48, 48);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "play.png"));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "play.png"));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "play.png"));
                btn.Click += playButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(310, 763);
                btn.Size = new Size(48, 48);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "stop.png"));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "stop.png"));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "stop.png"));
                btn.Click += stopButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(210, 750);
                btn.Size = new Size(62, 62);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "openw.png"));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "openw.png"));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "openw.png"));
                btn.Click += openFileButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(950, 165);
                btn.Size = new Size(62, 62);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "close.png"));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "close.png"));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "close.png"));
                btn.Click += closeButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(230, 450);
                btn.Size = new Size(32, 32);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "prev.png"));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "prev.png"));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "prev.png"));
                btn.Click += prevButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(365, 450);
                btn.Size = new Size(32, 32);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "next.png"));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "next.png"));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "next.png"));
                btn.Click += nextButton_Click;
                Controls.Add(btn);
                this.noFocusTrackBar1.ValueChanged += TrackBar1_ValueChanged;
            }
            catch
            {
                // swallow — don't block startup if shaping fails
                Opacity = 1;
            }
        }


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
                    this.labelInfo.Text = "Author: " + this.tune.Info.InfoString2;
                    this.labelInfo.Text += Environment.NewLine + "Title: " + this.tune.Info.InfoString1;
                    this.labelInfo.Text += Environment.NewLine + "Released: " + this.tune.Info.InfoString3;
                }

                this.updateCurrentSong();
            }
        }


        private void TrackBar1_ValueChanged(object? sender, EventArgs e)
        {
            float vol = (float)noFocusTrackBar1.Value;
            if (vol > 0.0)
            {
                vol = vol / 10;
            }

            player.setVolume(vol);
        }

        private void playButton_Click(object? sender, EventArgs e)
        {
            if (this.tune != null)
            {
                player.stop();
                player.Start(tune);
            }
        }

        private void stopButton_Click(object? sender, EventArgs e)
        {
            player.stop();
        }

        private void closeButton_Click(object? sender, EventArgs e)
        {
            player.stop();
            this.Close();
        }

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

        private void updateCurrentSong()
        {

            if (this.tune != null)
            {
                int tmp = this.tune.Info.currentSong;
                if (tmp == 0) { tmp = 1; }

                if (tmp < 10)
                {
                    this.label3.Text = "0" + tmp + " / ";
                }
                else
                {
                    this.label3.Text = tmp + " / ";
                }

                if (this.tune.Info.songs < 10)
                {
                    this.label3.Text += "0" + this.tune.Info.songs;
                }
                else
                {
                    this.label3.Text += this.tune.Info.songs;
                }
            }
            else
            {
                this.label3.Text = "00 / 00";
            }
        }
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
                this.label2.Text = Path.GetFileName(filePath);
                this.loadTune();
                player.Start(tune);
            }
            else
            {
                ;
            }
        }

        // Prevent the default background erase to avoid a white flash.
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
