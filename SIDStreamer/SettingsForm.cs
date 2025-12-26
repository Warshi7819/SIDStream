using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;


namespace SIDStream
{
    public partial class SettingsForm : Form
    {
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

        public string newlySelectedSkin = "";

        public SettingsForm()
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

        // <summary>
        // Allow dragging the window by holding left mouse button anywhere on the form client area.
        // (controls will still receive their own mouse events and won't trigger this).
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
        // Load skin data from JSON file
        // </summary>
        private Skin? loadSkinData(string skinPath)
        {

            Skin? deserializedSkin = JsonSerializer.Deserialize<Skin>(File.ReadAllText(skinPath));
            return deserializedSkin;
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
        // Get base directories in a given path
        // </summary>
        public static List<string> getBaseDirectories(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return new List<string>();

            return new List<string>(Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly));
        }

        // <summary>
        // Get current skin from skinsettings.json
        // </summary>
        private string getCurrentSkin()
        {
            SkinSettings? settings = loadSkinSettings(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skinsettings.json"));
            return settings.skinName;
        }

        // <summary>
        // Load and apply skin during form load
        // </summary>
        private void SIDstreamer_Load(object? sender, EventArgs e)
        {
            try
            {
                string currentSkin = this.getCurrentSkin();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string skinsDir = Path.Combine(baseDir, "skins");
                string skinDir = Path.Combine(baseDir, "skins", currentSkin);
                var skin = this.loadSkinData(Path.Combine(skinDir, "settings-skin.json"));
                string imagePath = Path.Combine(skinDir, skin.bgSettingsImage);

                // Suspend layout and apply shape before first paint
                SuspendLayout();

                // Load background into managed fields so we can scale before draw/region creation
                LoadBackground(Path.Combine(skinDir, skin.bgSettingsImage));

                // you can call SetBackgroundSize(...) here before ApplyImageShape if you want to pre-scale:
                // e.g. SetBackgroundSize(800, 0); // preserve aspect by width

                ApplyImageShapeFromLoadedBackground();

                // Load default logo if present
                LoadLogo(Path.Combine(skinDir, skin.logoImage));
                SetLogoPosition(skin.logoX, skin.logoY);
                SetLogoSize(skin.logoWidth, skin.logoHeight); // preserve aspect by width

                ResumeLayout();

                // Show the form now that shape/background is applied
                Opacity = 1;


                var btn = new SIDStreamer.Controls.ImageButton();

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.closeButtonX, skin.closeButtonY);
                btn.Size = new Size(skin.closeButtonWidth, skin.closeButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(skinDir, skin.closeButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(skinDir, skin.closeButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(skinDir, skin.closeButtonPressedImage));
                btn.Click += closeButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.okButtonX, skin.okButtonY);
                btn.Size = new Size(skin.okButtonWidth, skin.okButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(skinDir, skin.okButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(skinDir, skin.okButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(skinDir, skin.okButtonPressedImage));
                btn.Click += okButton_Click;
                Controls.Add(btn);

                btn = new SIDStreamer.Controls.ImageButton();
                btn.Location = new Point(skin.cancelButtonX, skin.cancelButtonY);
                btn.Size = new Size(skin.cancelButtonWidth, skin.cancelButtonHeight);
                btn.NormalImage = Image.FromFile(Path.Combine(skinDir, skin.cancelButtonImage));
                btn.HoverImage = Image.FromFile(Path.Combine(skinDir, skin.cancelButtonHoverImage));
                btn.PressedImage = Image.FromFile(Path.Combine(skinDir, skin.cancelButtonPressedImage));
                btn.Click += cancelButton_Click;
                Controls.Add(btn);

                this.skinLabel.Text = skin.selectSkinLabel;
                this.skinLabel.Location = new Point(skin.selectSkinLabelX, skin.selectSkinLabelY);
                this.skinLabel.BackColor = this.hexToColor(skin.selectSkinLabelBGColor);
                this.skinLabel.ForeColor = this.hexToColor(skin.selectSkinLabelFGColor);
                
                
                
                // Position and style skincombobox
                this.skinComboBox.Location = new Point(skin.skinComboBoxX, skin.skinComboBoxY);
                this.skinComboBox.Size = new Size(skin.skinComboBoxWidth, skin.skinComboBoxHeight);
                // TODO: Select current skin by default
                this.skinComboBox.TabIndex = 1;
                this.skinComboBox.BackColor = this.hexToColor(skin.skinComboBoxBGColor);
                this.skinComboBox.ForeColor = this.hexToColor(skin.skinComboBoxFGColor);

                // Populate and display skinComboBox
                var dirs = getBaseDirectories(skinsDir);

                foreach (var d in dirs)
                {
                    this.skinComboBox.Items.Add(Path.GetFileName(d));
                }


            }
            catch
            {
                // swallow — don't block startup if shaping fails
                Opacity = 1;
            }
        }

        // <summary>
        // Close button click handler
        // </summary>
        private void closeButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // <summary>
        // OK button click handler
        // </summary>
        private void okButton_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.skinComboBox.Text))
            {
                this.DialogResult = DialogResult.OK;
                this.newlySelectedSkin = this.skinComboBox.Text;
                this.Close();
            }
            else { 
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        // <summary>
        // Cancel button click handler
        // </summary>
        private void cancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // <summary>
        // Custom background paint to draw pre-scaled images
        // Prevent the default background erase to avoid a white flash AND white boarder around the image
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
