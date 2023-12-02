namespace FileToImg
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            ToPictureButton = new Button();
            ToFileButton = new Button();
            infolistBox = new ListBox();
            progressTextBox = new TextBox();
            selectoutputfolderbutton = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            openFileDialog1 = new OpenFileDialog();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(259, 351);
            button1.Name = "button1";
            button1.Size = new Size(241, 34);
            button1.TabIndex = 0;
            button1.Text = "変換スタート";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // ToPictureButton
            // 
            ToPictureButton.AllowDrop = true;
            ToPictureButton.Location = new Point(6, 6);
            ToPictureButton.Name = "ToPictureButton";
            ToPictureButton.Size = new Size(475, 93);
            ToPictureButton.TabIndex = 1;
            ToPictureButton.Text = "画像に変換(D && D)";
            ToPictureButton.UseVisualStyleBackColor = true;
            ToPictureButton.Click += InputFIlePathSelectMenuOpen;
            ToPictureButton.DragDrop += ToPictureButton_DragDrop;
            ToPictureButton.DragEnter += ToPictureButton_DragEnter;
            // 
            // ToFileButton
            // 
            ToFileButton.AllowDrop = true;
            ToFileButton.Location = new Point(6, 6);
            ToFileButton.Name = "ToFileButton";
            ToFileButton.Size = new Size(475, 93);
            ToFileButton.TabIndex = 2;
            ToFileButton.Text = "ファイルに変換 (D && D)";
            ToFileButton.UseVisualStyleBackColor = true;
            ToFileButton.Click += InputFIlePathSelectMenuOpen;
            ToFileButton.DragDrop += ToFileButton_DragDrop;
            ToFileButton.DragEnter += ToFileButton_DragEnter;
            // 
            // infolistBox
            // 
            infolistBox.FormattingEnabled = true;
            infolistBox.ItemHeight = 15;
            infolistBox.Location = new Point(12, 146);
            infolistBox.Name = "infolistBox";
            infolistBox.Size = new Size(488, 199);
            infolistBox.TabIndex = 3;
            // 
            // progressTextBox
            // 
            progressTextBox.Location = new Point(12, 391);
            progressTextBox.Name = "progressTextBox";
            progressTextBox.Size = new Size(482, 23);
            progressTextBox.TabIndex = 4;
            progressTextBox.Text = "準備完了";
            // 
            // selectoutputfolderbutton
            // 
            selectoutputfolderbutton.Location = new Point(12, 351);
            selectoutputfolderbutton.Name = "selectoutputfolderbutton";
            selectoutputfolderbutton.Size = new Size(241, 34);
            selectoutputfolderbutton.TabIndex = 5;
            selectoutputfolderbutton.Text = "出力フォルダ選択";
            selectoutputfolderbutton.UseVisualStyleBackColor = true;
            selectoutputfolderbutton.Click += selectoutputfolderbutton_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new Point(5, 7);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(495, 133);
            tabControl1.TabIndex = 6;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(ToPictureButton);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(487, 105);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "ファイル→画像";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(ToFileButton);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(487, 105);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "画像→ファイル";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(506, 426);
            Controls.Add(tabControl1);
            Controls.Add(selectoutputfolderbutton);
            Controls.Add(progressTextBox);
            Controls.Add(infolistBox);
            Controls.Add(button1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "MainWindow";
            Text = "FileToImg";
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button ToPictureButton;
        private Button ToFileButton;
        private ListBox infolistBox;
        private TextBox progressTextBox;
        private Button selectoutputfolderbutton;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private OpenFileDialog openFileDialog1;
    }
}
