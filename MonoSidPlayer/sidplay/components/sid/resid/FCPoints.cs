using System;

namespace sidplay
{
    // ----------------------------------------------------------------------------
    // Return array of default spline interpolation points to map FC to
    // filter cutoff frequency.
    // ----------------------------------------------------------------------------
    public class FCPoints
    {
        public int[][] points = null!;

        public int count;
    }
}