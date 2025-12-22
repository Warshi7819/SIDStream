using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SIDStream
{
    internal class Skin
    {
        // Transparent background image for player
        public string bgImage { get; set; }

        // Default value of media label on startup
        public string mediaLabel { get; set; }
        public string mediaLabelBGColor { get; set; }
        public string mediaLabelFGColor { get; set; }
        public int mediaLabelX { get; set; }
        public int mediaLabelY { get; set; }


        // Value of info label e.g. SIDstreamer v.1.0.
        public string infoLabel { get; set; }
        public string infoLabelBGColor { get; set; }
        public string infoLabelFGColor { get; set; }
        public int infoLabelX { get; set; }
        public int infoLabelY { get; set; }
        public string copyrightLabel { get; set; }
        public string copyrightLabelBGColor { get; set; }
        public string copyrightLabelFGColor { get; set; }

        public int copyrightLabelX { get; set; }
        public int copyrightLabelY { get; set; }


        // Set position of volume slider
        public int volumeSliderX { get; set; }
        public int volumeSliderY { get; set; }
        public string volumeSliderBGColor { get; set; }
        public string volumeSliderFGColor { get; set; }

        // Set position of Current track label
        public int currentTrackLabelX { get; set; }
        public int currentTrackLabelY { get; set; }
        public string currentTrackLabelBGColor { get; set; }
        public string currentTrackLabelFGColor { get; set; }

        // Logo settings
        public string logoImage { get; set; }
        public int logoX { get; set; }
        public int logoY { get; set; }
        public int logoWidth { get; set; }
        public int logoHeight { get; set; }

        // Play button
        public string playButtonImage { get; set; }
        public string playButtonHoverImage { get; set; }
        public string playButtonPressedImage { get; set; }
        public int playButtonX { get; set; }
        public int playButtonY { get; set; }
        public int playButtonWidth { get; set; }
        public int playButtonHeight { get; set; }

        // Stop button
        public string stopButtonImage { get; set; }
        public string stopButtonHoverImage { get; set; }
        public string stopButtonPressedImage { get; set; }
        public int stopButtonX { get; set; }
        public int stopButtonY { get; set; }
        public int stopButtonWidth { get; set; }
        public int stopButtonHeight { get; set; }

        // Open Button
        public string openButtonImage { get; set; }
        public string openButtonHoverImage { get; set; }
        public string openButtonPressedImage { get; set; }
        public int openButtonX { get; set; }
        public int openButtonY { get; set; }
        public int openButtonWidth { get; set; }
        public int openButtonHeight { get; set; }

        // Close Button
        public string closeButtonImage { get; set; }
        public string closeButtonHoverImage { get; set; }
        public string closeButtonPressedImage { get; set; }
        public int closeButtonX { get; set; }
        public int closeButtonY { get; set; }
        public int closeButtonWidth { get; set; }
        public int closeButtonHeight { get; set; }

        // previous button
        public string previousButtonImage { get; set; }
        public string previousButtonHoverImage { get; set; }
        public string previousButtonPressedImage { get; set; }
        public int previousButtonX { get; set; }
        public int previousButtonY { get; set; }
        public int previousButtonWidth { get; set; }
        public int previousButtonHeight { get; set; }

        // next button
        public string nextButtonImage { get; set; }
        public string nextButtonHoverImage { get; set; }
        public string nextButtonPressedImage { get; set; }
        public int nextButtonX { get; set; }
        public int nextButtonY { get; set; }
        public int nextButtonWidth { get; set; }
        public int nextButtonHeight { get; set; }
    }
}