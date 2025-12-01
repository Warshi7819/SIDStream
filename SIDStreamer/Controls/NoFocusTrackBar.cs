using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SIDStreamer.Controls
{
    public class NoFocusTrackBar : TrackBar
    {
        public NoFocusTrackBar()
        {
            TabStop = false; // hindrer tab-fokus
            SetStyle(ControlStyles.Selectable, false); // gjør kontrollen ikke-fokusbar
        }

        protected override void OnGotFocus(EventArgs e)
        {
            // ikke kall base.OnGotFocus, så tegnes ikke fokusrektangelet
        }

        protected override bool ShowFocusCues => false;

        protected override void WndProc(ref Message m)
        {
            const int WM_SETFOCUS = 0x0007;
            const int WM_KILLFOCUS = 0x0008;

            if (m.Msg == WM_SETFOCUS || m.Msg == WM_KILLFOCUS)
            {
                return; // ignorer fokusmeldinger
            }

            base.WndProc(ref m);
        }
    }
}