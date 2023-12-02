using K4os.Compression.LZ4.Streams;
using Microsoft.WindowsAPICodePack.Dialogs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Diagnostics;
using Image = SixLabors.ImageSharp.Image;

namespace FileToImg
{
    public partial class MainWindow : Form
    {
        ConvertInfo ConvertInfo = new();
        bool IsCoverting = false;

        public MainWindow()
        {
            InitializeComponent();
            ShowInfoBox(ConvertInfo);
            ChangeProgressText(0);
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            if (IsCoverting)
            {
                MessageBox.Show("�ϊ����ł��B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ConvertInfo.OutputFolderPath.Length == 0)
            {
                MessageBox.Show("�o�̓t�H���_�[���w�肳��Ă��܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (ConvertInfo.InputFilePath.Length == 0)
            {
                MessageBox.Show("���̓t�@�C�����w�肳��Ă��܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IsCoverting = true;

            //�t�@�C�����摜�@���[�h�̎�
            if (tabControl1.SelectedIndex == 0)
            {
                //�t�@�C�����o�C�i���ɕϊ�
                ChangeProgressText(1);
                byte[] bytes = await Task.Run(() => FileToBinary(ConvertInfo.InputFilePath));

                //�o�C�g�z�񂪋�̏ꍇ�͉������Ȃ� = �G���[
                if (bytes.Length <= 0)
                {
                    IsCoverting = false;
                    ChangeProgressText(-1);
                    return;
                }

                //�o�C�i���f�[�^�����k
                ChangeProgressText(2);
                byte[] compbyte = await Task.Run(() => ByteCompress(bytes));

                //�o�C�i���f�[�^���摜�ɕϊ�
                ChangeProgressText(3);
                await Task.Run(() => BinaryToImage(compbyte, ConvertInfo.OutputFolderPath + "\\�o�C�i���摜.png"));
            }

            //�摜���t�@�C���@���[�h�̎�
            else if (tabControl1.SelectedIndex == 1)
            {
                //�摜�o�C�i���f�[�^���o�C�i���f�[�^�ɒ���
                ChangeProgressText(4);
                byte[] bytes = await Task.Run(() => BinaryImageToBinary(ConvertInfo.InputFilePath));

                //�o�C�g�z�񂪋�̏ꍇ�͉������Ȃ� = �G���[
                if (bytes.Length <= 0)
                {
                    ChangeProgressText(-1);
                    IsCoverting = false;
                    return;
                }

                //���k����Ă���o�C�i���f�[�^����
                ChangeProgressText(5);
                bytes = await Task.Run(() => ByteDecompress(bytes));

                //�o�C�i���f�[�^���t�@�C���ɏ�������
                ChangeProgressText(6);
                await Task.Run(() => File.WriteAllBytes(ConvertInfo.OutputFolderPath + "\\����.bin", bytes));
            }

            ChangeProgressText(0);
            IsCoverting = false;
        }

        /// <summary>
        /// �t�@�C�����o�C�i���ɕϊ����܂��B
        /// </summary>
        /// <param name="filePath">���̓t�@�C��</param>
        /// <remarks>2GB�ȏ�̃t�@�C���͓ǂݍ��߂܂���B</remarks>
        private byte[] FileToBinary(string filePath)
        {
            //�o�C�i���f�[�^���i�[����z��
            byte[] bs;

            try
            {
                bs = File.ReadAllBytes(filePath);
                Debug.WriteLine("�ǂݍ��񂾃o�C�g��: " + bs.Length);
            }
            catch (IOException)
            {
                ChangeProgressText(-1);
                MessageBox.Show("2GB�ȏ�̃t�@�C���͓ǂݍ��߂܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return [];
            }

            return bs;
        }

        /// <summary>
        /// 16�i���̃o�C�g�z����J���[�R�[�h�ɕϊ��A���̌�摜�Ƃ��ĕۑ����܂��B
        /// </summary>
        /// <param name="bs">�o�C�i���f�[�^</param>
        private void BinaryToImage(byte[] bs, string savePath)
        {
            //�o�C�i���f�[�^����摜�̃T�C�Y������
            SixLabors.ImageSharp.Point size = GetXYSize(bs.Length);
            Debug.WriteLine("�摜�̃T�C�Y: " + size.X + "x" + size.Y);

            //��̉摜���쐬
            using Image<Rgb24> image = new(size.X, size.Y);

            //�J���[�R�[�h���摜�ɔ��f
            image.ProcessPixelRows(accessor =>
            {
                int cnt = 0;
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgb24> pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        ref Rgb24 pixel = ref pixelRow[x];
                        Rgb24 transparent;

                        //���܂�̕�����0���߂���
                        if (bs.Length <= cnt)
                        {
                            transparent = SixLabors.ImageSharp.Color.FromRgb(0, 0, 0);
                            pixel = transparent;

                            cnt++;
                            continue;
                        }

                        transparent = SixLabors.ImageSharp.Color.FromRgb(bs[cnt], bs[cnt], bs[cnt]);
                        pixel = transparent;

                        //Debug.WriteLine("x: " + x + ", y: " + y + ", colorcode: " + bs[cnt] + ", cnt: " + cnt);
                        cnt++;
                    }
                }
            });

            //�摜��ۑ�
            var encoder = new PngEncoder()
            {
                BitDepth = PngBitDepth.Bit8,
                CompressionLevel = PngCompressionLevel.Level9,
                ColorType = PngColorType.Grayscale
            };

            image.Save(savePath, encoder);
            image.Dispose();
        }

        /// <summary>
        /// �o�C�i���f�[�^�̉摜��ǂݍ��݁AByte��Ԃ��܂��B
        /// </summary>
        /// <param name="binaryImgPath"></param>
        private byte[] BinaryImageToBinary(string binaryImgPath)
        {
            Debug.WriteLine("�t�@�C���̕�����");

            ChangeProgressText(4);

            Image<Rgb24> image;

            try
            {
                image = Image.Load<Rgb24>(binaryImgPath);
            }
            catch (UnknownImageFormatException)
            {
                MessageBox.Show("�����ł��Ȃ��t�@�C���ł��B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return [];
            }

            //�摜�̃T�C�Y���擾
            int w = image.Width;
            int h = image.Height;

            byte[] bytes = new byte[w * h];

            int cnt = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    //Debug.WriteLine("x: " + x + ", y: " + y + ", colorcode: " + image[x, y].R + ", " + image[x, y].G + ", " + image[x, y].B);
                    try
                    {
                        bytes[cnt] = image[x, y].R;
                        cnt++;
                    }
                    catch
                    {
                        MessageBox.Show("�t�@�C���̕����Ɏ��s���܂����B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return [];
                    }
                }
            }
            image.Dispose();

            return bytes;
        }

        /// <summary>
        /// �o�C�i���f�[�^�̗ʂ����āA�摜�̃T�C�Y�����肵�܂��B
        /// </summary>
        /// <param name="cnt">�o�C�i���f�[�^�̗� ("1A" �łP��)</param>
        /// <returns>X��Y�̏�񂪓�����ImageSharp.Point</returns>
        private SixLabors.ImageSharp.Point GetXYSize(int cnt)
        {
            cnt = (int)((cnt / 2.0) + 0.9);

            int i = 1;
            while ((int)Math.Pow(2, i) < cnt)
            {
                cnt = (int)((cnt / 2.0) + 0.9);
                i++;
            }

            return new SixLabors.ImageSharp.Point((int)Math.Pow(2, i), cnt);
        }

        /// <summary>
        /// �r�b�g�z������k���܂��B
        /// </summary>
        /// <param name="buffer">���k�������r�b�g��</param>
        /// <returns>���k���ꂽ�r�b�g�z��</returns>
        public static byte[] ByteCompress(byte[] buffer)
        {
            return LZ4Frame.Encode(buffer.AsSpan(), new ArrayBufferWriter<byte>(), K4os.Compression.LZ4.LZ4Level.L12_MAX).WrittenMemory.ToArray();
        }

        /// <summary>
        /// ���k�����r�b�g�z����𓀂��܂��B
        /// </summary>
        /// <param name="buffer">���k�����r�b�g�z��</param>
        /// <returns>�����E�𓀂��ꂽ�r�b�g�z��</returns>
        public static byte[] ByteDecompress(byte[] buffer)
        {
            return LZ4Frame.Decode(buffer.AsSpan(), new ArrayBufferWriter<byte>()).WrittenMemory.ToArray();
        }

        /// <summary>
        /// DragEnter�C�x���g�n���h��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToPictureButton_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void ToPictureButton_DragDrop(object sender, DragEventArgs e)
        {
            FileAttributes attr;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            //null�`�F�b�N
            if (files is null) return;

            if (files.Length > 1)
            {
                MessageBox.Show("�������͏o���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            attr = File.GetAttributes(files[0]);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("�t�H���_�[�͓��͏o���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //�t�@�C���p�X�ݒ�
            ConvertInfo.InputFilePath = files[0];

            //�t�@�C�������擾
            byte[] bytes = await Task.Run(() => FileToBinary(files[0]));
            ConvertInfo.OutputImageSize = GetXYSize(bytes.Length);
            ConvertInfo.InputFileSize = bytes.Length;

            //info���X�V
            ShowInfoBox(ConvertInfo);
        }

        /// <summary>
        /// ���̓t�@�C���I���_�C�A���O���J���܂��B
        /// </summary>
        private void InputFIlePathSelectMenuOpen(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                ConvertInfo.InputFilePath = openFileDialog1.FileName;
                ShowInfoBox(ConvertInfo);
            }
        }

        private void ToFileButton_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ToFileButton_DragDrop(object sender, DragEventArgs e)
        {
            FileAttributes attr;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            //null�`�F�b�N
            if (files is null) return;

            if (files.Length > 1)
            {
                MessageBox.Show("�������͏o���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            attr = File.GetAttributes(files[0]);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("�t�H���_�[�͓��͏o���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //info���X�V
            ConvertInfo.InputFilePath = files[0];
            ShowInfoBox(ConvertInfo);
        }

        /// <summary>
        /// ListBox�ɏ���\�����܂��B
        /// </summary>
        /// <param name="convertInfo"></param>
        private void ShowInfoBox(ConvertInfo convertInfo)
        {
            infolistBox.Items.Clear();
            infolistBox.Items.Add("/-----------���[�h---------/");
            infolistBox.Items.Add("�ϊ����[�h�F " + convertInfo.Mode);
            infolistBox.Items.Add("");
            infolistBox.Items.Add("/------�I���t�@�C�����-----/");
            infolistBox.Items.Add("���̓t�@�C���F " + convertInfo.InputFilePath);
            infolistBox.Items.Add("�o�̓t�H���_�F " + convertInfo.OutputFolderPath);
            infolistBox.Items.Add("");
            infolistBox.Items.Add("/--------�T�C�Y���--------/");
            infolistBox.Items.Add("�o�͉𑜓x�F " + convertInfo.OutputImageSize);
            infolistBox.Items.Add("���̓T�C�Y�F " + convertInfo.InputFileSize);
        }

        /// <summary>
        /// �i����\�����܂��B
        /// </summary>
        /// <param name="stat"></param>
        private void ChangeProgressText(int stat)
        {
            switch (stat)
            {
                case 0:
                    progressTextBox.Text = "--����--";
                    break;


                case 1:
                    progressTextBox.Text = "1/3 �o�C�i���ɕϊ���";
                    break;
                case 2:
                    progressTextBox.Text = "2/3 �o�C�i�������k��";
                    break;
                case 3:
                    progressTextBox.Text = "3/3 �o�C�i�����摜�ɕϊ���";
                    break;


                case 4:
                    progressTextBox.Text = "1/3 �摜��ǂݎ�蒆";
                    break;
                case 5:
                    progressTextBox.Text = "2/3 �o�C�i�����𓀒�";
                    break;
                case 6:
                    progressTextBox.Text = "3/3 �t�@�C���������ݒ�";
                    break;


                case -1:
                    progressTextBox.Text = "!!! �G���[���������܂����B";
                    break;

                default:
                    break;
            }
        }

        private void selectoutputfolderbutton_Click(object sender, EventArgs e)
        {
            // �_�C�A���O�̃C���X�^���X�𐶐�
            using CommonOpenFileDialog dialog = new("�t�H���_�[�̑I��")
            {
                // �I���`�����t�H���_�[�X�^�C����
                IsFolderPicker = true,
            };

            // �_�C�A���O��\��
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (dialog.FileName is null) return;

                //info���X�V
                ConvertInfo.OutputFolderPath = dialog.FileName;
                ShowInfoBox(ConvertInfo);
            }
        }

        /// <summary>
        /// TabControl�̑I�����ύX���ꂽ�Ƃ��ɌĂяo����܂��B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConvertInfo.Mode = tabControl1.SelectedIndex == 0 ? "�t�@�C�����摜" : "�摜���t�@�C��";
            ShowInfoBox(ConvertInfo);
        }
    }

    /// <summary>
    /// Listbox�ɕ\����������܂Ƃ߂��\����
    /// </summary>
    struct ConvertInfo
    {
        public ConvertInfo()
        {
        }

        /// <summary>
        /// �ϊ����[�h 
        /// </summary>
        public string Mode { get; set; } = "�t�@�C�����摜";

        /// <summary>
        /// ���̓t�@�C���̃p�X
        /// </summary>
        public string InputFilePath { get; set; } = "";

        /// <summary>
        /// �o�̓t�H���_�̃p�X
        /// </summary>
        public string OutputFolderPath { get; set; } = "";

        /// <summary>
        /// ���̓t�@�C���̃T�C�Y
        /// </summary>
        public long InputFileSize { get; set; } = 0;

        /// <summary>
        /// �o�͉摜�̉𑜓x
        /// </summary>
        public SixLabors.ImageSharp.Point OutputImageSize { get; set; } = new SixLabors.ImageSharp.Point(0, 0);
    }
}