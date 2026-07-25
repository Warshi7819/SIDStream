using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


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

        Dictionary<string, EventHandler> _buttonHandlers;

        Dictionary<string, Action<System.Windows.Forms.ComboBox, string, string>> _comboInitializers;
        Dictionary<string, System.Windows.Forms.ComboBox> _comboBoxes; 




        public SettingsForm()
        {
            InitializeComponent();

            // Define button handlers to match the ones in the skin settings. 
            _buttonHandlers = new Dictionary<string, EventHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { "close", closeButton_Click },
                { "ok", okButton_Click },
                { "cancel", cancelButton_Click }
            };

            
            _comboInitializers = new Dictionary<string, Action<System.Windows.Forms.ComboBox, string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "skin", InitializeSkinCombo }
            };
            _comboBoxes = new Dictionary<string, System.Windows.Forms.ComboBox>(StringComparer.OrdinalIgnoreCase);


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

            // Select and display current skin in combo box
            

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

                // Parse skin JSON file to get skin parameters
                var skinParser = new UiSkinParser(Path.Combine(skinDir, "settings-skin.json"));

                // Suspend layout and apply shape before first paint
                SuspendLayout();

                // Load background into managed fields so we can scale before draw/region creation
                string imagePath = Path.Combine(skinDir, skinParser.BgImage);
                LoadBackground(imagePath);

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
                    var label = new Label();
                    label.Text = UiSkinParser.Str(lbl, "text");
                    label.Location = new Point(UiSkinParser.Int(lbl, "x-pos"), UiSkinParser.Int(lbl, "y-pos"));
                    label.BackColor = this.hexToColor(UiSkinParser.Str(lbl, "bg-color"));
                    label.ForeColor = this.hexToColor(UiSkinParser.Str(lbl, "fg-color"));
                    label.AutoSize = true;
                    scaleLabelForResolution(label);

                    Controls.Add(label);
                }

                // ComboBoxes
                foreach (var (name, cb) in skinParser.GetComboBoxes())
                {
                    var comboBox = new System.Windows.Forms.ComboBox();
                    comboBox.Location = new Point(UiSkinParser.Int(cb, "x-pos"), UiSkinParser.Int(cb, "y-pos"));
                    comboBox.Size = new Size(UiSkinParser.Int(cb, "width"), UiSkinParser.Int(cb, "height"));
                    comboBox.BackColor = this.hexToColor(UiSkinParser.Str(cb, "bg-color"));
                    comboBox.ForeColor = this.hexToColor(UiSkinParser.Str(cb, "fg-color"));


                    // initializer based on combo box name
                    if (_comboInitializers.TryGetValue(name, out var init))
                    {
                        init(comboBox, skinsDir, currentSkin);
                    }

                    _comboBoxes[name] = comboBox;
                    Controls.Add(comboBox);
                }

                // Images
                foreach (var (name, img) in skinParser.GetImages())
                {
                    LoadLogo(Path.Combine(skinDir, UiSkinParser.Str(img, "file")));
                    SetLogoPosition(UiSkinParser.Int(img, "x-pos"), UiSkinParser.Int(img, "y-pos"));
                    SetLogoSize(UiSkinParser.Int(img, "width"), UiSkinParser.Int(img, "height")); // preserve aspect by width
                }

                ResumeLayout();
                // Show the form now that shape/background is applied
                Opacity = 1;
            }
            catch (Exception ex) 
            {
                // swallow — don't block startup if shaping fails
                Opacity = 1;
            }
        }


        private void InitializeSkinCombo(System.Windows.Forms.ComboBox cb, string skinsDir, string currentSkin)
        {
            // Populate and display skinComboBox
                var dirs = getBaseDirectories(skinsDir);

            foreach (var d in dirs)
            {
                cb.Items.Add(Path.GetFileName(d));
            }

            int index = cb.FindStringExact(currentSkin);

            // If found, select it
            if (index != -1)
            {
                cb.SelectedIndex = index;
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
            var cb = _comboBoxes["skin"];
            if (!string.IsNullOrEmpty(cb.Text))
            {
                this.DialogResult = DialogResult.OK;
                this.newlySelectedSkin = cb.Text;
                this.Close();
            }
            else
            {
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

        private void skinComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ;
        }
    }
}
