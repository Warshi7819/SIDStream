using sidplay;
using SIDStream;
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

        // Auto scale fonts/labels based on the resolution and DPI setting my dev machine
        // had at the time of development. (2560x1600 at 200% scaling → 192 DPI)
        public void scaleLabelForResolution(Label lbl)
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

        private void SIDstreamer_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }


        private Skin loadSkinData(string skinPath = "")
        { 
            Skin skin = new Skin
            {
                bgImage = "christmas.png",
                logoImage = "logo.png",
                logoX = 200,
                logoY = 160,
                logoWidth = 200,
                logoHeight = 0,
                infoLabel = "SIDstreamer v.1.0",
                infoLabelX = 220,
                infoLabelY = 300,   
                mediaLabel = "No media selected ...",
                mediaLabelX = 203,
                mediaLabelY = 700,
                copyrightLabel = "Merry Christmas 2025 - Retro And Gaming ©",
                copyrightLabelX = 310,
                copyrightLabelY = 840,

                // Play Button
                playButtonImage = "play.png",
                playButtonHoverImage = "play.png",
                playButtonPressedImage = "play.png",
                playButtonX = 370,
                playButtonY = 763,
                playButtonWidth = 48,
                playButtonHeight = 48,

                // Stop Button
                stopButtonImage = "stop.png",
                stopButtonHoverImage = "stop.png",
                stopButtonPressedImage = "stop.png",
                stopButtonX = 310,
                stopButtonY = 763,
                stopButtonWidth = 48,
                stopButtonHeight = 48,

                // open button
                openButtonImage = "openw.png",
                openButtonHoverImage = "openw.png",
                openButtonPressedImage = "openw.png",
                openButtonX = 210,
                openButtonY = 750,
                openButtonWidth = 62,
                openButtonHeight = 62,

                // Close button
                closeButtonImage = "close.png",
                closeButtonHoverImage = "close.png",
                closeButtonPressedImage = "close.png",
                closeButtonX = 950,
                closeButtonY = 165,
                closeButtonWidth = 62,
                closeButtonHeight = 62,

                // Previous button
                previousButtonImage = "prev.png",
                previousButtonHoverImage = "prev.png",
                previousButtonPressedImage = "prev.png",
                previousButtonX = 230,
                previousButtonY = 450,
                previousButtonWidth = 32,
                previousButtonHeight = 32,

                // Next button
                nextButtonImage = "next.png",
                nextButtonHoverImage = "next.png",
                nextButtonPressedImage = "next.png",
                nextButtonX = 365,
                nextButtonY = 450,
                nextButtonWidth = 32,
                nextButtonHeight = 32,

                // volume slider position
                volumeSliderX = 600,
                volumeSliderY = 760,

                // Current track label position
                currentTrackLabelX = 270,
                currentTrackLabelY = 450

            };

            return skin;
        }

        private void SIDstreamer_Load(object? sender, EventArgs e)
        {
            try
            {
                var skin = this.loadSkinData();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string imagePath = Path.Combine(baseDir, "skins", skin.bgImage);

                // fallback when running from IDE
                if (!File.Exists(imagePath))
                    imagePath = Path.Combine(Directory.GetCurrentDirectory(), "skins", skin.bgImage);

                // Suspend layout and apply shape before first paint
                SuspendLayout();

                // Load background into managed fields so we can scale before draw/region creation
                LoadBackground(Path.Combine("skins", skin.bgImage));

                // you can call SetBackgroundSize(...) here before ApplyImageShape if you want to pre-scale:
                // e.g. SetBackgroundSize(800, 0); // preserve aspect by width

                ApplyImageShapeFromLoadedBackground();

                // Load default logo if present
                LoadLogo(Path.Combine("skins", skin.logoImage));
                SetLogoPosition(skin.logoX, skin.logoY);
                SetLogoSize(skin.logoWidth, skin.logoHeight); // preserve aspect by width

                ResumeLayout();

                // Show the form now that shape/background is applied
                Opacity = 1;

                // after LoadBackground/ApplyImageShape and before showing form add the image buttons
                var btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.playButtonX, skin.playButtonY);
                btn.Size = new Size(skin.playButtonWidth, skin.playButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.playButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.playButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.playButtonPressedImage));
                btn.Click += playButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.stopButtonX, skin.stopButtonY);
                btn.Size = new Size(skin.stopButtonWidth, skin.stopButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.stopButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.stopButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.stopButtonPressedImage));
                btn.Click += stopButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.openButtonX, skin.openButtonY);
                btn.Size = new Size(skin.openButtonWidth, skin.openButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.openButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.openButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.openButtonPressedImage));
                btn.Click += openFileButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.closeButtonX, skin.closeButtonY);
                btn.Size = new Size(skin.closeButtonWidth, skin.closeButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.closeButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.closeButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.closeButtonPressedImage));
                btn.Click += closeButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.previousButtonX, skin.previousButtonY);
                btn.Size = new Size(skin.previousButtonWidth, skin.previousButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.previousButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.previousButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.previousButtonPressedImage));
                btn.Click += prevButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.nextButtonX, skin.nextButtonY);
                btn.Size = new Size(skin.nextButtonWidth, skin.nextButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.nextButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.nextButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skin.nextButtonPressedImage));
                btn.Click += nextButton_Click;
                Controls.Add(btn);
                this.noFocusTrackBar1.ValueChanged += TrackBar1_ValueChanged;


                this.label1.Text = skin.copyrightLabel;
                this.label2.Text = skin.mediaLabel;
                this.labelInfo.Text = skin.infoLabel;
                
                // Set some absolute positions for labels
                scaleLabelForResolution(this.label1);
                this.label1.Location = new Point(skin.copyrightLabelX, skin.copyrightLabelY);
                scaleLabelForResolution(this.label2);
                this.label2.Location = new Point(skin.mediaLabelX, skin.mediaLabelY);
                scaleLabelForResolution(this.label3);
                this.label3.Location = new Point(skin.currentTrackLabelX, skin.currentTrackLabelY);
                scaleLabelForResolution(this.labelInfo);
                this.labelInfo.Location = new Point(skin.infoLabelX, skin.infoLabelY);
                    
                this.noFocusTrackBar1.Location = new Point(skin.volumeSliderX, skin.volumeSliderY);
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
            if (tune != null)
            {
                tune.Info.currentSong = 1;
            }
            this.updateCurrentSong();
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
