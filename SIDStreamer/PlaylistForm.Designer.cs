namespace SIDStreamer
{
    partial class PlaylistForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            splitContainer = new SplitContainer();
            browserPanel = new Panel();
            browserTree = new TreeView();
            addButton = new Button();
            addFileButton = new Button();
            searchPanel = new Panel();
            searchBox = new TextBox();
            searchButton = new Button();
            browserLabel = new Label();
            playlistPanel = new Panel();
            playlistList = new ListBox();
            removeButton = new Button();
            moveUpButton = new Button();
            moveDownButton = new Button();
            clearButton = new Button();
            playlistLabel = new Label();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            menuStrip = new MenuStrip();
            fileMenu = new ToolStripMenuItem();
            loadMenuItem = new ToolStripMenuItem();
            saveMenuItem = new ToolStripMenuItem();
            saveAsMenuItem = new ToolStripMenuItem();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            browserPanel.SuspendLayout();
            searchPanel.SuspendLayout();
            playlistPanel.SuspendLayout();
            statusStrip.SuspendLayout();
            menuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 44);
            splitContainer.Margin = new Padding(6);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(browserPanel);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(playlistPanel);
            splitContainer.Size = new Size(1671, 1194);
            splitContainer.SplitterDistance = 1347;
            splitContainer.SplitterWidth = 7;
            splitContainer.TabIndex = 0;
            // 
            // browserPanel
            // 
            browserPanel.Controls.Add(browserTree);
            browserPanel.Controls.Add(addButton);
            browserPanel.Controls.Add(addFileButton);
            browserPanel.Controls.Add(searchPanel);
            browserPanel.Controls.Add(browserLabel);
            browserPanel.Dock = DockStyle.Fill;
            browserPanel.Location = new Point(0, 0);
            browserPanel.Margin = new Padding(6);
            browserPanel.Name = "browserPanel";
            browserPanel.Padding = new Padding(7, 9, 7, 9);
            browserPanel.Size = new Size(1347, 1194);
            browserPanel.TabIndex = 0;
            // 
            // browserTree
            // 
            browserTree.CheckBoxes = true;
            browserTree.Dock = DockStyle.Fill;
            browserTree.HideSelection = false;
            browserTree.Location = new Point(7, 98);
            browserTree.Margin = new Padding(6);
            browserTree.Name = "browserTree";
            browserTree.Size = new Size(1333, 959);
            browserTree.TabIndex = 0;
            // 
            // addButton
            // 
            addButton.Dock = DockStyle.Bottom;
            addButton.Enabled = false;
            addButton.Location = new Point(7, 1057);
            addButton.Margin = new Padding(6);
            addButton.Name = "addButton";
            addButton.Size = new Size(1333, 64);
            addButton.TabIndex = 1;
            addButton.Text = "Add >>";
            // 
            // addFileButton
            // 
            addFileButton.Dock = DockStyle.Bottom;
            addFileButton.Location = new Point(7, 1121);
            addFileButton.Margin = new Padding(6);
            addFileButton.Name = "addFileButton";
            addFileButton.Size = new Size(1333, 64);
            addFileButton.TabIndex = 2;
            addFileButton.Text = "Add File...";
            // 
            // searchPanel
            // 
            searchPanel.Controls.Add(searchBox);
            searchPanel.Controls.Add(searchButton);
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Location = new Point(7, 60);
            searchPanel.Name = "searchPanel";
            searchPanel.Size = new Size(1333, 38);
            searchPanel.TabIndex = 4;
            // 
            // searchBox
            // 
            searchBox.Dock = DockStyle.Fill;
            searchBox.Location = new Point(0, 0);
            searchBox.Margin = new Padding(6);
            searchBox.Name = "searchBox";
            searchBox.Size = new Size(1233, 39);
            searchBox.TabIndex = 0;
            // 
            // searchButton
            // 
            searchButton.Dock = DockStyle.Right;
            searchButton.Location = new Point(1233, 0);
            searchButton.Margin = new Padding(6);
            searchButton.Name = "searchButton";
            searchButton.Size = new Size(100, 38);
            searchButton.TabIndex = 1;
            searchButton.Text = "Search";
            // 
            // browserLabel
            // 
            browserLabel.Dock = DockStyle.Top;
            browserLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            browserLabel.Location = new Point(7, 9);
            browserLabel.Margin = new Padding(6, 0, 6, 0);
            browserLabel.Name = "browserLabel";
            browserLabel.Size = new Size(1333, 51);
            browserLabel.TabIndex = 3;
            browserLabel.Text = "HVSC Browser";
            browserLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // playlistPanel
            // 
            playlistPanel.Controls.Add(playlistList);
            playlistPanel.Controls.Add(removeButton);
            playlistPanel.Controls.Add(moveUpButton);
            playlistPanel.Controls.Add(moveDownButton);
            playlistPanel.Controls.Add(clearButton);
            playlistPanel.Controls.Add(playlistLabel);
            playlistPanel.Dock = DockStyle.Fill;
            playlistPanel.Location = new Point(0, 0);
            playlistPanel.Margin = new Padding(6);
            playlistPanel.Name = "playlistPanel";
            playlistPanel.Padding = new Padding(7, 9, 7, 9);
            playlistPanel.Size = new Size(317, 1194);
            playlistPanel.TabIndex = 0;
            // 
            // playlistList
            // 
            playlistList.Dock = DockStyle.Fill;
            playlistList.Font = new Font("Segoe UI", 9F);
            playlistList.IntegralHeight = false;
            playlistList.Location = new Point(7, 60);
            playlistList.Margin = new Padding(6);
            playlistList.Name = "playlistList";
            playlistList.SelectionMode = SelectionMode.MultiExtended;
            playlistList.Size = new Size(303, 869);
            playlistList.TabIndex = 0;
            // 
            // removeButton
            // 
            removeButton.Dock = DockStyle.Bottom;
            removeButton.Enabled = false;
            removeButton.Location = new Point(7, 929);
            removeButton.Margin = new Padding(6);
            removeButton.Name = "removeButton";
            removeButton.Size = new Size(303, 64);
            removeButton.TabIndex = 1;
            removeButton.Text = "<< Remove";
            // 
            // moveUpButton
            // 
            moveUpButton.Dock = DockStyle.Bottom;
            moveUpButton.Enabled = false;
            moveUpButton.Location = new Point(7, 993);
            moveUpButton.Margin = new Padding(6);
            moveUpButton.Name = "moveUpButton";
            moveUpButton.Size = new Size(303, 64);
            moveUpButton.TabIndex = 2;
            moveUpButton.Text = "Move Up";
            // 
            // moveDownButton
            // 
            moveDownButton.Dock = DockStyle.Bottom;
            moveDownButton.Enabled = false;
            moveDownButton.Location = new Point(7, 1057);
            moveDownButton.Margin = new Padding(6);
            moveDownButton.Name = "moveDownButton";
            moveDownButton.Size = new Size(303, 64);
            moveDownButton.TabIndex = 3;
            moveDownButton.Text = "Move Down";
            // 
            // clearButton
            // 
            clearButton.Dock = DockStyle.Bottom;
            clearButton.Enabled = false;
            clearButton.Location = new Point(7, 1121);
            clearButton.Margin = new Padding(6);
            clearButton.Name = "clearButton";
            clearButton.Size = new Size(303, 64);
            clearButton.TabIndex = 4;
            clearButton.Text = "Clear All";
            // 
            // playlistLabel
            // 
            playlistLabel.Dock = DockStyle.Top;
            playlistLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            playlistLabel.Location = new Point(7, 9);
            playlistLabel.Margin = new Padding(6, 0, 6, 0);
            playlistLabel.Name = "playlistLabel";
            playlistLabel.Size = new Size(303, 51);
            playlistLabel.TabIndex = 6;
            playlistLabel.Text = "Current Playlist";
            playlistLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(32, 32);
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip.Location = new Point(0, 1238);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(2, 0, 26, 0);
            statusStrip.Size = new Size(1671, 42);
            statusStrip.TabIndex = 5;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(207, 32);
            statusLabel.Text = "No playlist loaded";
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(32, 32);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(11, 4, 0, 4);
            menuStrip.Size = new Size(1671, 44);
            menuStrip.TabIndex = 6;
            // 
            // fileMenu
            // 
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { loadMenuItem, saveMenuItem, saveAsMenuItem });
            fileMenu.Name = "fileMenu";
            fileMenu.Size = new Size(71, 36);
            fileMenu.Text = "&File";
            // 
            // loadMenuItem
            // 
            loadMenuItem.Name = "loadMenuItem";
            loadMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            loadMenuItem.Size = new Size(298, 44);
            loadMenuItem.Text = "&Load...";
            // 
            // saveMenuItem
            // 
            saveMenuItem.Name = "saveMenuItem";
            saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveMenuItem.Size = new Size(298, 44);
            saveMenuItem.Text = "&Save";
            // 
            // saveAsMenuItem
            // 
            saveAsMenuItem.Name = "saveAsMenuItem";
            saveAsMenuItem.Size = new Size(298, 44);
            saveAsMenuItem.Text = "Save &As...";
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = "PLS Playlist (*.pls)|*.pls|All Files (*.*)|*.*";
            openFileDialog.Title = "Load Playlist";
            // 
            // saveFileDialog
            // 
            saveFileDialog.Filter = "PLS Playlist (*.pls)|*.pls";
            saveFileDialog.Title = "Save Playlist As";
            // 
            // PlaylistForm
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1671, 1280);
            Controls.Add(splitContainer);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Margin = new Padding(6);
            MinimumSize = new Size(1278, 880);
            Name = "PlaylistForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SIDstream - Playlist Manager";
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            browserPanel.ResumeLayout(false);
            searchPanel.ResumeLayout(false);
            searchPanel.PerformLayout();
            playlistPanel.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private SplitContainer splitContainer;
        private Panel browserPanel;
        private Label browserLabel;
        private TreeView browserTree;
        private Button addButton;
        private Button addFileButton;
        private Panel searchPanel;
        private TextBox searchBox;
        private Button searchButton;
        private Panel playlistPanel;
        private Label playlistLabel;
        private ListBox playlistList;
        private Button removeButton;
        private Button moveUpButton;
        private Button moveDownButton;
        private Button clearButton;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem loadMenuItem;
        private ToolStripMenuItem saveMenuItem;
        private ToolStripMenuItem saveAsMenuItem;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
    }
}
