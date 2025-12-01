using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIDStreamer.Controls
{
    public class ImageButton : Control
    {
        private Image? normalImage;
        private Image? hoverImage;
        private Image? pressedImage;
        private bool isPressed;
        private bool isHover;

        // Prevent the designer from attempting to serialize Image contents.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? NormalImage { get => normalImage; set { normalImage = value; Invalidate(); } }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? HoverImage { get => hoverImage; set { hoverImage = value; Invalidate(); } }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? PressedImage { get => pressedImage; set { pressedImage = value; Invalidate(); } }

        public ImageButton()
        {
            // Support transparent backgrounds and custom painting
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);

            Cursor = Cursors.Hand;
            BackColor = Color.Transparent; // important for transparency
            Size = new Size(32, 32);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            // Ensure control does not get an opaque window background from parent
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHover = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Keep default processing for MouseUp (raise MouseUp etc.)
            base.OnMouseUp(e);

            // Only update visual pressed state here. Do NOT call OnClick explicitly:
            // the WinForms message loop / base implementation will raise Click once.
            if (isPressed && e.Button == MouseButtons.Left)
            {
                isPressed = false;
                Invalidate();
                // removed explicit OnClick(EventArgs.Empty) to avoid duplicate Click invocations
            }
        }

        // Correct transparent painting by asking the parent to paint into this control's background.
        // This prevents leftover artifacts (white rectangle) when parent/other controls update.
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (Parent == null)
            {
                base.OnPaintBackground(pevent);
                return;
            }

            // Translate the graphics so parent's drawing aligns with this control
            var g = pevent.Graphics;
            var state = g.Save();
            try
            {
                g.TranslateTransform(-Left, -Top);
                var pe = new PaintEventArgs(g, new Rectangle(Location, Parent.ClientSize));
                // Let the parent paint its background and content into our surface
                this.InvokePaintBackground(Parent, pe);
                this.InvokePaint(Parent, pe);
            }
            finally
            {
                g.Restore(state);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Don't call base.OnPaint to avoid any default focus drawing
            Image? img = normalImage;
            if (isPressed && pressedImage != null) img = pressedImage;
            else if (isHover && hoverImage != null) img = hoverImage;
            else img = normalImage;

            if (img != null)
            {
                e.Graphics.CompositingMode = CompositingMode.SourceOver;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.DrawImage(img, new Rectangle(0, 0, Width, Height));
            }
            // otherwise leave transparent
        }

        // Ensure the control is focusable and keyboard-activatable
        protected override bool IsInputKey(Keys keyData) => true;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate(); // ensure no stale focus artifacts
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate(); // refresh when focus leaves
        }
    }
}