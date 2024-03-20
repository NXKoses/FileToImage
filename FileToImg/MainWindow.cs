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
        //Listboxに表示する情報をまとめた構造体
        ConvertInfo ConvertInfo = new();
        //変換中かどうか
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
                MessageBox.Show("変換中です。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ConvertInfo.OutputFolderPath.Length == 0)
            {
                MessageBox.Show("出力フォルダーが指定されていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (ConvertInfo.InputFilePath.Length == 0)
            {
                MessageBox.Show("入力ファイルが指定されていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IsCoverting = true;

            //ファイル→画像　モードの時
            if (tabControl1.SelectedIndex == 0)
            {
                //ファイルをバイナリに変換
                ChangeProgressText(1);
                byte[] bytes = await Task.Run(() => FileToBinary(ConvertInfo.InputFilePath));

                //バイト配列が空の場合は何もしない = エラー
                if (bytes.Length <= 0)
                {
                    IsCoverting = false;
                    ChangeProgressText(-1);
                    return;
                }

                //バイナリデータを圧縮
                ChangeProgressText(2);
                bytes = await Task.Run(() => ByteCompress(bytes));

                //バイナリデータを画像に変換
                ChangeProgressText(3);

                //ファイル名を設定
                string savePath = SetSaveFileName(ConvertInfo.OutputFolderPath, "バイナリ画像", ".png");
                await Task.Run(() => BinaryToImage(bytes, savePath));
            }

            //画像→ファイル　モードの時
            else if (tabControl1.SelectedIndex == 1)
            {
                //画像バイナリデータをバイナリデータに直す
                ChangeProgressText(4);
                byte[] bytes = await Task.Run(() => BinaryImageToBinary(ConvertInfo.InputFilePath));

                //バイト配列が空の場合は何もしない = エラー
                if (bytes.Length <= 0)
                {
                    MessageBox.Show("ファイルの復元に失敗しました。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    IsCoverting = false;
                    return;
                }

                //圧縮されているバイナリデータを解凍
                ChangeProgressText(5);
                bytes = await Task.Run(() => ByteDecompress(bytes));

                //バイナリデータをファイルに書き込み
                ChangeProgressText(6);

                //ファイル名を設定
                string savePath = SetSaveFileName(ConvertInfo.OutputFolderPath);
                await Task.Run(() => File.WriteAllBytes(savePath, bytes));
            }

            ChangeProgressText(0);
            IsCoverting = false;
        }

        /// <summary>
        /// 保存ディレクトリに同名のファイルがあった場合、連番をつけてファイルパスを返します。
        /// </summary>
        /// <param name="filePath">保存先</param>
        /// <param name="fileName">保存名（拡張子を含む）</param>
        /// <param name="ext">拡張子</param>
        /// <returns>保存ファイルパス</returns>
        private static string SetSaveFileName(string filePath, string fileName = "復元", string ext = ".bin")
        {
            string savePath = filePath + "\\" + fileName + ext;

            //もし保存先に同じファイル名があったら連番をつける
            int i = 1;
            while (File.Exists(savePath))
            {
                savePath = filePath + "\\" + fileName + "(" + i + ")" + ext;
                i++;
            }

            return savePath;
        }

        /// <summary>
        /// ファイルをバイナリに変換します。
        /// </summary>
        /// <param name="filePath">入力ファイル</param>
        /// <remarks>2GB以上のファイルは読み込めません。</remarks>
        private byte[] FileToBinary(string filePath)
        {
            //バイナリデータを格納する配列
            byte[] bs;

            try
            {
                //バイナリデータを読み込み
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);

                bs = new byte[fs.Length];
                fs.Read(bs, 0, bs.Length);
                fs.Close();
                fs.Dispose();
            }
            catch (Exception e)
            {
                MessageBox.Show("バイナリ変換でエラーが発生しました。" + Environment.NewLine + e, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return [];
            }

            return bs;
        }

        /// <summary>
        /// 16進数のバイト配列をカラーコードに変換、その後画像として保存します。
        /// </summary>
        /// <param name="bs">バイナリデータ</param>
        private static void BinaryToImage(byte[] bs, string savePath)
        {
            //バイナリデータから画像のサイズを決定
            SixLabors.ImageSharp.Point size = GetXYSize(bs.Length);
            Debug.WriteLine("画像のサイズ: " + size.X + "x" + size.Y);

            //空の画像を作成
            using Image<Rgb24> image = new(size.X, size.Y);

            //カラーコードを画像に反映
            image.ProcessPixelRows(accessor =>
            {
                int cnt = 0;
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgb24> pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        ref Rgb24 pixel = ref pixelRow[x];

                        //あまりの部分は0埋めする
                        if (bs.Length <= cnt)
                        {
                            pixel = SixLabors.ImageSharp.Color.FromRgb(0, 0, 0);

                            cnt++;
                            continue;
                        }

                        pixel = SixLabors.ImageSharp.Color.FromRgb(bs[cnt], bs[cnt], bs[cnt]);

                        //Debug.WriteLine("x: " + x + ", y: " + y + ", colorcode: " + bs[cnt] + ", cnt: " + cnt);
                        cnt++;
                    }
                }
            });

            //画像を保存
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
        /// バイナリデータの画像を読み込み、Byteを返します。
        /// </summary>
        /// <param name="binaryImgPath"></param>
        private byte[] BinaryImageToBinary(string binaryImgPath)
        {
            Debug.WriteLine("ファイルの復元中");

            ChangeProgressText(4);

            Image<Rgb24> image;

            try
            {
                image = Image.Load<Rgb24>(binaryImgPath);
            }
            catch (UnknownImageFormatException)
            {
                MessageBox.Show("復元できないファイルです。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return [];
            }

            //画像のサイズを取得
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
                        MessageBox.Show("ファイルの復元に失敗しました。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return [];
                    }
                }
            }
            image.Dispose();

            return bytes;
        }

        /// <summary>
        /// バイナリデータの量を見て、画像のサイズを決定します。
        /// </summary>
        /// <param name="cnt">バイナリデータの量 ("1A" で１つ)</param>
        /// <returns>XとYの情報が入ったImageSharp.Point</returns>
        private static SixLabors.ImageSharp.Point GetXYSize(int cnt)
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
        /// ビット配列を圧縮します。
        /// </summary>
        /// <param name="buffer">圧縮したいビット列</param>
        /// <returns>圧縮されたビット配列</returns>
        public static byte[] ByteCompress(byte[] buffer)
        {
            return LZ4Frame.Encode(buffer.AsSpan(), new ArrayBufferWriter<byte>(), K4os.Compression.LZ4.LZ4Level.L12_MAX).WrittenMemory.ToArray();
        }

        /// <summary>
        /// 圧縮したビット配列を解凍します。
        /// </summary>
        /// <param name="buffer">圧縮したビット配列</param>
        /// <returns>減圧・解凍されたビット配列</returns>
        public static byte[] ByteDecompress(byte[] buffer)
        {
            return LZ4Frame.Decode(buffer.AsSpan(), new ArrayBufferWriter<byte>()).WrittenMemory.ToArray();
        }

        /// <summary>
        /// DragEnterイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToPictureButton_DragEnter(object sender, DragEventArgs e)
        {
            //nullチェック
            if (e.Data is null) return;

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
            string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            //nullチェック
            if (files is null) return;

            if (files.Length > 1)
            {
                MessageBox.Show("複数入力出来ません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await ToPictureButton_FormInfoUpdate(files[0]);
        }

        private void ToFileButton_DragEnter(object sender, DragEventArgs e)
        {
            //nullチェック
            if (e.Data is null) return;

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
            string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            //nullチェック
            if (files is null) return;

            if (files.Length > 1)
            {
                MessageBox.Show("複数入力出来ません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //情報更新
            ToFileButton_FormInfoUpdate(files[0]);
        }

        /// <summary>
        /// ファイルから画像に変換するボタンをクリックした際に、Formのファイルサイズなどの情報を更新します。
        /// </summary>
        /// <param name="filepath">変換ファイルパス</param>
        private async Task ToPictureButton_FormInfoUpdate(string filepath)
        {
            FileAttributes attr;

            attr = File.GetAttributes(filepath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("フォルダーは入力出来ません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //ファイルパス設定
            ConvertInfo.InputFilePath = filepath;

            //ファイル情報を取得
            byte[]? bytes = await Task.Run(() => FileToBinary(filepath));
            ConvertInfo.OutputImageSize = GetXYSize(bytes.Length);
            ConvertInfo.InputFileSize = bytes.Length;

            bytes = null;
            GC.Collect();

            //infoを更新
            ShowInfoBox(ConvertInfo);
        }

        /// <summary>
        /// 画像からファイルに変換するボタンをクリックした際に、Formのファイルサイズなどの情報を更新します。
        /// </summary>
        /// <param name="filepath"></param>
        private void ToFileButton_FormInfoUpdate(string filepath)
        {
            FileAttributes attr;

            attr = File.GetAttributes(filepath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("フォルダーは入力出来ません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //infoを更新
            ConvertInfo.InputFilePath = filepath;
            ShowInfoBox(ConvertInfo);
        }

        /// <summary>
        /// 入力ファイル選択ダイアログを開きます。
        /// </summary>
        private async void InputFIlePathSelectMenuOpen(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                ConvertInfo.InputFilePath = openFileDialog1.FileName;
                ShowInfoBox(ConvertInfo);

                //ファイルサイズなどの情報を設定 (ファイル→画像モードの時)
                if (tabControl1.SelectedIndex == 0)
                {
                    await ToPictureButton_FormInfoUpdate(openFileDialog1.FileName);
                }
            }
        }

        /// <summary>
        /// ListBoxに情報を表示します。
        /// </summary>
        /// <param name="convertInfo"></param>
        private void ShowInfoBox(ConvertInfo convertInfo)
        {
            infolistBox.Items.Clear();
            infolistBox.Items.Add("/-----------モード---------/");
            infolistBox.Items.Add("変換モード： " + convertInfo.Mode);
            infolistBox.Items.Add("");
            infolistBox.Items.Add("/------選択ファイル情報-----/");
            infolistBox.Items.Add("入力ファイル： " + convertInfo.InputFilePath);
            infolistBox.Items.Add("出力フォルダ： " + convertInfo.OutputFolderPath);
            infolistBox.Items.Add("");
            infolistBox.Items.Add("/--------サイズ情報--------/");
            infolistBox.Items.Add("出力解像度： " + convertInfo.OutputImageSize);
            infolistBox.Items.Add("入力サイズ： " + convertInfo.InputFileSize);
        }

        /// <summary>
        /// 進捗を表示します。
        /// </summary>
        /// <param name="stat"></param>
        private void ChangeProgressText(int stat)
        {
            switch (stat)
            {
                case 0:
                    progressTextBox.Text = "--完了--";
                    break;


                case 1:
                    progressTextBox.Text = "1/3 バイナリに変換中";
                    break;
                case 2:
                    progressTextBox.Text = "2/3 バイナリを圧縮中";
                    break;
                case 3:
                    progressTextBox.Text = "3/3 バイナリを画像に変換中";
                    break;


                case 4:
                    progressTextBox.Text = "1/3 画像を読み取り中";
                    break;
                case 5:
                    progressTextBox.Text = "2/3 バイナリを解凍中";
                    break;
                case 6:
                    progressTextBox.Text = "3/3 ファイル書き込み中";
                    break;

                default:
                    break;
            }
        }

        private void Selectoutputfolderbutton_Click(object sender, EventArgs e)
        {
            // ダイアログのインスタンスを生成
            using CommonOpenFileDialog dialog = new("フォルダーの選択")
            {
                // 選択形式をフォルダースタイルに
                IsFolderPicker = true,
            };

            // ダイアログを表示
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (dialog.FileName is null) return;

                //infoを更新
                ConvertInfo.OutputFolderPath = dialog.FileName;
                ShowInfoBox(ConvertInfo);
            }
        }

        /// <summary>
        /// TabControlの選択が変更されたときに呼び出されます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConvertInfo.Mode = tabControl1.SelectedIndex == 0 ? "ファイル→画像" : "画像→ファイル";
            ShowInfoBox(ConvertInfo);
        }
    }

    /// <summary>
    /// Listboxに表示する情報をまとめた構造体
    /// </summary>
    struct ConvertInfo
    {
        public ConvertInfo()
        {
        }

        /// <summary>
        /// 変換モード 
        /// </summary>
        public string Mode { get; set; } = "ファイル→画像";

        /// <summary>
        /// 入力ファイルのパス
        /// </summary>
        public string InputFilePath { get; set; } = "";

        /// <summary>
        /// 出力フォルダのパス
        /// </summary>
        public string OutputFolderPath { get; set; } = "";

        /// <summary>
        /// 入力ファイルのサイズ
        /// </summary>
        public long InputFileSize { get; set; } = 0;

        /// <summary>
        /// 出力画像の解像度
        /// </summary>
        public SixLabors.ImageSharp.Point OutputImageSize { get; set; } = new SixLabors.ImageSharp.Point(0, 0);
    }
}