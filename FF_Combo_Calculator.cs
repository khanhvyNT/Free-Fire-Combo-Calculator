using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FFComboCalculator
{
    static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && (args[0] == "--selftest" || args[0] == "--test"))
            {
                AttachConsole(-1);
                bool ok = MainForm.RunSelfTest();
                Environment.Exit(ok ? 0 : 1);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // -------------------------------------------------------------
        // Thuật toán cốt lõi (theo combo_codec.py)
        // -------------------------------------------------------------
        private static readonly int[] KEY = new int[] {
            126, 112, 117, 126, 100, 101, 110, 98, 121, 112, 99,
            116, 110, 114, 126, 117, 116, 110, 122, 116, 104, 125
        };

        private const byte SEP = 23;
        private static readonly byte[] NONE_SEG = new byte[] { 1 };
        private static readonly byte[] SHOP_SEG = new byte[] { 4, 1, 1, 1, 1, 1, 1, 1, 5 };
        private static readonly byte[] FIXED_SEG = new byte[] { 0, 1, 4 };
        private static readonly Dictionary<int, byte[]> PET_SEG = new Dictionary<int, byte[]>() {
            { 0, new byte[] { 1 } },
            { 1, new byte[] { 0, 2, 0, 4, 1, 1, 1, 1, 1, 8 } },
            { 2, new byte[] { 0, 2, 0, 4, 1, 1, 1, 1, 0, 1 } }
        };

        private Dictionary<string, byte[]> blocks = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        // -------------------------------------------------------------
        // Giao diện người dùng (Win32 Native Cổ Điển)
        // -------------------------------------------------------------
        // Phần 1: Nhập dữ liệu nhân vật
        private GroupBox grpPart1;
        private Label lblSlot1, lblSlot2, lblSlot3, lblSlot4;
        private TextBox txtSlot1, txtSlot2, txtSlot3, txtSlot4;
        private Button btnImport, btnClearInputs, btnGuide;
        private Label lblGuideHint;

        // Phần 2: Ghép thành combo
        private GroupBox grpPart2;
        private ComboBox cbChoice1, cbChoice2, cbChoice3, cbChoice4, cbChoice5, cbChoice6;
        private Label lblPlus1, lblPlus2, lblPlus3, lblPlus4, lblPlus5;
        private Label lblH1, lblH2, lblH3, lblH4, lblH5, lblH6;
        private Button btnBuildCombo, btnResetCombo;

        // Phần 3: Thanh progress và kết quả tính combo
        private GroupBox grpPart3;
        private ProgressBar progressBar;
        private Label lblProgressText;
        private Label lblResultHeader;
        private TextBox txtResultCode;
        private Button btnCopyResult, btnVerifyResult;
        private TextBox txtLogDetails;

        private Timer timerProgress;
        private int targetProgress = 0;
        private Action onProgressComplete = null;

        public MainForm()
        {
            InitializeComponent();
            LoadCharacterDatabase();
            PopulateChoiceboxes();
        }

        private void InitializeComponent()
        {
            this.Text = "Free Fire Combo Calculator";
            this.Size = new Size(820, 680);
            this.MinimumSize = new Size(800, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            // ---------------------------------------------------------
            // 1. NHẬP DỮ LIỆU NHÂN VẬT
            // ---------------------------------------------------------
            grpPart1 = new GroupBox();
            grpPart1.Text = "1. Nhập dữ liệu nhân vật";
            grpPart1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpPart1.Location = new Point(12, 10);
            grpPart1.Size = new Size(780, 160);
            grpPart1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Font normalFont = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Slot 1
            lblSlot1 = new Label() { Text = "Slot 1:", Location = new Point(15, 26), Size = new Size(50, 20), Font = normalFont };
            txtSlot1 = new TextBox() { Location = new Point(70, 23), Size = new Size(690, 23), Font = normalFont, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Slot 2
            lblSlot2 = new Label() { Text = "Slot 2:", Location = new Point(15, 54), Size = new Size(50, 20), Font = normalFont };
            txtSlot2 = new TextBox() { Location = new Point(70, 51), Size = new Size(690, 23), Font = normalFont, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Slot 3
            lblSlot3 = new Label() { Text = "Slot 3:", Location = new Point(15, 82), Size = new Size(50, 20), Font = normalFont };
            txtSlot3 = new TextBox() { Location = new Point(70, 79), Size = new Size(690, 23), Font = normalFont, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Slot 4
            lblSlot4 = new Label() { Text = "Slot 4:", Location = new Point(15, 110), Size = new Size(50, 20), Font = normalFont };
            txtSlot4 = new TextBox() { Location = new Point(70, 107), Size = new Size(690, 23), Font = normalFont, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Nút Nhập ở dưới
            btnImport = new Button();
            btnImport.Text = "Nhập";
            btnImport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnImport.Location = new Point(70, 134);
            btnImport.Size = new Size(110, 24);
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += new EventHandler(BtnImport_Click);

            btnClearInputs = new Button();
            btnClearInputs.Text = "Xóa trắng";
            btnClearInputs.Font = normalFont;
            btnClearInputs.Location = new Point(190, 134);
            btnClearInputs.Size = new Size(85, 24);
            btnClearInputs.UseVisualStyleBackColor = true;
            btnClearInputs.Click += (s, e) => {
                txtSlot1.Clear();
                txtSlot2.Clear();
                txtSlot3.Clear();
                txtSlot4.Clear();
            };

            btnGuide = new Button();
            btnGuide.Text = "Cách lấy mã slot";
            btnGuide.Font = normalFont;
            btnGuide.Location = new Point(285, 134);
            btnGuide.Size = new Size(130, 24);
            btnGuide.UseVisualStyleBackColor = true;
            btnGuide.Click += (s, e) => GuideDialog.ShowGuide(this);

            lblGuideHint = new Label();
            lblGuideHint.Text = "(* Bấm 'Cách lấy mã slot' để xem ảnh hướng dẫn trích xuất mã từ game)";
            lblGuideHint.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
            lblGuideHint.ForeColor = Color.FromArgb(85, 85, 85);
            lblGuideHint.Location = new Point(425, 138);
            lblGuideHint.Size = new Size(345, 18);
            lblGuideHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            grpPart1.Controls.Add(lblSlot1); grpPart1.Controls.Add(txtSlot1);
            grpPart1.Controls.Add(lblSlot2); grpPart1.Controls.Add(txtSlot2);
            grpPart1.Controls.Add(lblSlot3); grpPart1.Controls.Add(txtSlot3);
            grpPart1.Controls.Add(lblSlot4); grpPart1.Controls.Add(txtSlot4);
            grpPart1.Controls.Add(btnImport);
            grpPart1.Controls.Add(btnClearInputs);
            grpPart1.Controls.Add(btnGuide);
            grpPart1.Controls.Add(lblGuideHint);

            // ---------------------------------------------------------
            // 2. GHÉP THÀNH COMBO
            // Format: {Choicebox1} + {Choicebox2} + {Choicebox3} + {Choicebox4} + {Choicebox5} + {Choicebox6}
            // ---------------------------------------------------------
            grpPart2 = new GroupBox();
            grpPart2.Text = "2. Ghép thành combo";
            grpPart2.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpPart2.Location = new Point(12, 175);
            grpPart2.Size = new Size(780, 115);
            grpPart2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Labels tiêu đề cột nhỏ
            Font subHeaderFont = new Font("Segoe UI", 8F, FontStyle.Regular);
            lblH1 = new Label() { Text = "Slot 1 (Chủ động)", Location = new Point(15, 22), Size = new Size(105, 15), Font = subHeaderFont };
            lblH2 = new Label() { Text = "Slot 2 (Bị động)", Location = new Point(135, 22), Size = new Size(105, 15), Font = subHeaderFont };
            lblH3 = new Label() { Text = "Slot 3 (Bị động)", Location = new Point(255, 22), Size = new Size(105, 15), Font = subHeaderFont };
            lblH4 = new Label() { Text = "Slot 4 (Bị động)", Location = new Point(375, 22), Size = new Size(105, 15), Font = subHeaderFont };
            lblH5 = new Label() { Text = "Shop Tool", Location = new Point(495, 22), Size = new Size(105, 15), Font = subHeaderFont };
            lblH6 = new Label() { Text = "Trợ Thủ (Pet)", Location = new Point(625, 22), Size = new Size(125, 15), Font = subHeaderFont };

            // 6 Choicebox
            cbChoice1 = new ComboBox() { Location = new Point(15, 38), Size = new Size(105, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = normalFont };
            lblPlus1 = new Label() { Text = "+", Location = new Point(122, 40), Size = new Size(12, 23), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            cbChoice2 = new ComboBox() { Location = new Point(135, 38), Size = new Size(105, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = normalFont };
            lblPlus2 = new Label() { Text = "+", Location = new Point(242, 40), Size = new Size(12, 23), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            cbChoice3 = new ComboBox() { Location = new Point(255, 38), Size = new Size(105, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = normalFont };
            lblPlus3 = new Label() { Text = "+", Location = new Point(362, 40), Size = new Size(12, 23), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            cbChoice4 = new ComboBox() { Location = new Point(375, 38), Size = new Size(105, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = normalFont };
            lblPlus4 = new Label() { Text = "+", Location = new Point(482, 40), Size = new Size(12, 23), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            cbChoice5 = new ComboBox() { Location = new Point(495, 38), Size = new Size(115, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = normalFont };
            cbChoice5.Items.Add("Không Shop");
            cbChoice5.Items.Add("Có Shop Tool");
            cbChoice5.SelectedIndex = 0;

            lblPlus5 = new Label() { Text = "+", Location = new Point(612, 40), Size = new Size(12, 23), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            cbChoice6 = new ComboBox() { Location = new Point(625, 38), Size = new Size(135, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = normalFont };
            cbChoice6.Items.Add("Không Pet (0)");
            cbChoice6.Items.Add("Cánh cụt");
            cbChoice6.Items.Add("Ottero");
            cbChoice6.SelectedIndex = 0;

            // Nút Ghép Combo ở dưới
            btnBuildCombo = new Button();
            btnBuildCombo.Text = "Ghép Combo";
            btnBuildCombo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnBuildCombo.Location = new Point(15, 75);
            btnBuildCombo.Size = new Size(130, 28);
            btnBuildCombo.UseVisualStyleBackColor = true;
            btnBuildCombo.Click += new EventHandler(BtnBuildCombo_Click);

            btnResetCombo = new Button();
            btnResetCombo.Text = "Đặt lại";
            btnResetCombo.Font = normalFont;
            btnResetCombo.Location = new Point(155, 75);
            btnResetCombo.Size = new Size(80, 28);
            btnResetCombo.UseVisualStyleBackColor = true;
            btnResetCombo.Click += (s, e) => {
                cbChoice1.SelectedIndex = 0;
                cbChoice2.SelectedIndex = 0;
                cbChoice3.SelectedIndex = 0;
                cbChoice4.SelectedIndex = 0;
                cbChoice5.SelectedIndex = 0;
                cbChoice6.SelectedIndex = 0;
            };

            grpPart2.Controls.Add(lblH1); grpPart2.Controls.Add(cbChoice1); grpPart2.Controls.Add(lblPlus1);
            grpPart2.Controls.Add(lblH2); grpPart2.Controls.Add(cbChoice2); grpPart2.Controls.Add(lblPlus2);
            grpPart2.Controls.Add(lblH3); grpPart2.Controls.Add(cbChoice3); grpPart2.Controls.Add(lblPlus3);
            grpPart2.Controls.Add(lblH4); grpPart2.Controls.Add(cbChoice4); grpPart2.Controls.Add(lblPlus4);
            grpPart2.Controls.Add(lblH5); grpPart2.Controls.Add(cbChoice5); grpPart2.Controls.Add(lblPlus5);
            grpPart2.Controls.Add(lblH6); grpPart2.Controls.Add(cbChoice6);
            grpPart2.Controls.Add(btnBuildCombo);
            grpPart2.Controls.Add(btnResetCombo);

            // ---------------------------------------------------------
            // 3. THANH PROGRESS VÀ KẾT QUẢ TÍNH COMBO
            // ---------------------------------------------------------
            grpPart3 = new GroupBox();
            grpPart3.Text = "3. Thanh progress và kết quả tính combo";
            grpPart3.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpPart3.Location = new Point(12, 295);
            grpPart3.Size = new Size(780, 335);
            grpPart3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lblProgressText = new Label() { Text = "Tiến độ sẵn sàng", Location = new Point(15, 23), Size = new Size(300, 16), Font = subHeaderFont };
            progressBar = new ProgressBar() { Location = new Point(15, 40), Size = new Size(750, 18), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            lblResultHeader = new Label() { Text = "Mã Combo Free Fire (#FF#...):", Location = new Point(15, 66), Size = new Size(200, 18), Font = normalFont };
            txtResultCode = new TextBox() {
                Location = new Point(15, 87),
                Size = new Size(540, 25),
                Font = new Font("Consolas", 10F, FontStyle.Bold),
                ReadOnly = true,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnCopyResult = new Button();
            btnCopyResult.Text = "Sao chép mã";
            btnCopyResult.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCopyResult.Location = new Point(565, 86);
            btnCopyResult.Size = new Size(100, 27);
            btnCopyResult.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyResult.UseVisualStyleBackColor = true;
            btnCopyResult.Click += new EventHandler(BtnCopyResult_Click);

            btnVerifyResult = new Button();
            btnVerifyResult.Text = "Giải ngược";
            btnVerifyResult.Font = normalFont;
            btnVerifyResult.Location = new Point(670, 86);
            btnVerifyResult.Size = new Size(95, 27);
            btnVerifyResult.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnVerifyResult.UseVisualStyleBackColor = true;
            btnVerifyResult.Click += new EventHandler(BtnVerifyResult_Click);

            Label lblLog = new Label() { Text = "Chi tiết phân tích & Nhật ký:", Location = new Point(15, 120), Size = new Size(250, 18), Font = normalFont };
            txtLogDetails = new TextBox() {
                Location = new Point(15, 140),
                Size = new Size(750, 180),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                ReadOnly = true,
                BackColor = SystemColors.Window,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            grpPart3.Controls.Add(lblProgressText);
            grpPart3.Controls.Add(progressBar);
            grpPart3.Controls.Add(lblResultHeader);
            grpPart3.Controls.Add(txtResultCode);
            grpPart3.Controls.Add(btnCopyResult);
            grpPart3.Controls.Add(btnVerifyResult);
            grpPart3.Controls.Add(lblLog);
            grpPart3.Controls.Add(txtLogDetails);

            // Controls của Form
            this.Controls.Add(grpPart1);
            this.Controls.Add(grpPart2);
            this.Controls.Add(grpPart3);

            // Timer cho hiệu ứng ProgressBar
            timerProgress = new Timer();
            timerProgress.Interval = 15;
            timerProgress.Tick += (s, e) => {
                if (progressBar.Value < targetProgress)
                {
                    progressBar.Value = Math.Min(progressBar.Maximum, progressBar.Value + 5);
                }
                else
                {
                    timerProgress.Stop();
                    if (onProgressComplete != null)
                    {
                        var action = onProgressComplete;
                        onProgressComplete = null;
                        action();
                    }
                }
            };
        }

        private void StartProgress(string status, Action onDone)
        {
            lblProgressText.Text = status;
            progressBar.Value = 0;
            targetProgress = 100;
            onProgressComplete = onDone;
            timerProgress.Start();
        }

        // -------------------------------------------------------------
        // Nạp và lưu dữ liệu nhân vật
        // -------------------------------------------------------------
        private void LoadCharacterDatabase()
        {
            // Danh sách cơ sở từ combo_codec.py
            blocks.Clear();
            blocks["Xayne"] = new byte[] { 5, 5, 1, 7 };
            blocks["Kelly"] = new byte[] { 3, 4, 1, 0 };
            blocks["Rin"] = new byte[] { 6, 4, 1, 7 };
            blocks["Ignis"] = new byte[] { 7, 6, 1, 7 };
            blocks["Dasha"] = new byte[] { 2, 4, 1, 7 };
            blocks["Alok"] = new byte[] { 3, 3, 1, 0, 7 };
            blocks["Tatsuya"] = new byte[] { 9, 4, 1, 1, 4 };
            blocks["Tat (Pre)"] = new byte[] { 9, 4, 1, 1, 4 };
            blocks["Alok (Pre)"] = new byte[] { 7, 5, 1, 1, 5 };
            blocks["BHXK"] = new byte[] { 0, 6, 1, 1, 8 };
            blocks["HoaCau"] = new byte[] { 0, 6, 1, 0, 1 };
            blocks["Phun lửa"] = new byte[] { 0, 6, 1, 0, 1 };
            blocks["Chidori"] = new byte[] { 0, 6, 1, 1, 9 };

            // Nạp từ characters.json nếu có
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "characters.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    string content = File.ReadAllText(jsonPath, Encoding.UTF8);
                    MatchCollection matches = Regex.Matches(content, @"\""([^\""]+)\""\s*:\s*\[([^\]]+)\]");
                    foreach (Match m in matches)
                    {
                        string name = m.Groups[1].Value.Trim();
                        string rawArr = m.Groups[2].Value.Trim();
                        string[] parts = rawArr.Split(',');
                        List<byte> bList = new List<byte>();
                        foreach (string p in parts)
                        {
                            byte b;
                            if (byte.TryParse(p.Trim(), out b))
                            {
                                bList.Add(b);
                            }
                        }
                        if (bList.Count > 0)
                        {
                            blocks[name] = bList.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("Lỗi đọc characters.json: " + ex.Message);
                }
            }

            // Loại bỏ các mục dữ liệu bị yêu cầu xóa
            string[] toRemove = new string[] { "Elena", "Alan", "Kepo", "Pre", "Tat", "Metra" };
            foreach (string del in toRemove)
            {
                blocks.Remove(del);
            }
        }

        private void SaveCharacter(string name, byte[] block)
        {
            blocks[name] = block;
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "characters.json");
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"characters\": {");
                int count = 0;
                foreach (var kvp in blocks)
                {
                    count++;
                    sb.AppendFormat("    \"{0}\": [{1}]", kvp.Key, string.Join(", ", kvp.Value));
                    if (count < blocks.Count) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  }");
                sb.AppendLine("}");
                File.WriteAllText(jsonPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AppendLog("Lỗi lưu character: " + ex.Message);
            }
        }

        private void PopulateChoiceboxes(string selectName = null)
        {
            List<string> list = new List<string>();
            list.Add("none");
            foreach (var k in blocks.Keys)
            {
                if (!list.Contains(k)) list.Add(k);
            }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            // Giữ "none" ở đầu
            list.Remove("none");
            list.Insert(0, "none");

            ComboBox[] cbs = new ComboBox[] { cbChoice1, cbChoice2, cbChoice3, cbChoice4 };
            foreach (var cb in cbs)
            {
                string curr = cb.SelectedItem != null ? cb.SelectedItem.ToString() : "none";
                cb.Items.Clear();
                foreach (var item in list) cb.Items.Add(item);
                if (!string.IsNullOrEmpty(selectName) && cb == cbChoice1 && list.Contains(selectName))
                {
                    cb.SelectedItem = selectName;
                }
                else if (list.Contains(curr))
                {
                    cb.SelectedItem = curr;
                }
                else
                {
                    cb.SelectedIndex = 0;
                }
            }
        }

        // -------------------------------------------------------------
        // Xử lý Phần 1: Nhập dữ liệu nhân vật
        // -------------------------------------------------------------
        private void BtnImport_Click(object sender, EventArgs e)
        {
            string[] rawInputs = new string[] { txtSlot1.Text, txtSlot2.Text, txtSlot3.Text, txtSlot4.Text };
            List<int> filledSlots = new List<int>();
            List<byte[]> extractedBlocks = new List<byte[]>();
            string detectedName = "";

            for (int i = 0; i < 4; i++)
            {
                string text = rawInputs[i].Trim();
                if (string.IsNullOrEmpty(text)) continue;

                // Thử trích xuất tên nếu có dạng: Alok (1): #FF#... hoặc Alok: #FF#...
                Match nameMatch = Regex.Match(text, @"^([A-Za-z0-9_\s\(\)\p{L}]+?)[\s\:\;\(]+#FF#");
                if (nameMatch.Success && string.IsNullOrEmpty(detectedName))
                {
                    string candidate = nameMatch.Groups[1].Value.Trim();
                    candidate = Regex.Replace(candidate, @"\(\d+\)", "").Trim();
                    if (!string.IsNullOrEmpty(candidate)) detectedName = candidate;
                }

                // Trích xuất mã base64
                Match m = Regex.Match(text, @"#FF#([A-Za-z0-9+/=]+)");
                string code = m.Success ? ("#FF#" + m.Groups[1].Value) : text;

                try
                {
                    byte[] p = PlainOf(code);
                    if (p.Length < 2 || p[0] != 0 || p[1] != SEP)
                    {
                        MessageBox.Show("Mã ở Slot " + (i + 1) + " có header sai!", "Lỗi định dạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    List<byte[]> segs = SplitSegments(p);
                    if (segs.Count < 4)
                    {
                        MessageBox.Show("Mã ở Slot " + (i + 1) + " không đủ 4 slot nhân vật!", "Lỗi cấu trúc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    byte[] targetSeg = segs[i];
                    if (targetSeg.Length == 1 && targetSeg[0] == 1)
                    {
                        // Đây là slot trống (none)
                        MessageBox.Show("Mã ở Slot " + (i + 1) + " là slot trống (none)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    filledSlots.Add(i + 1);
                    extractedBlocks.Add(targetSeg);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể giải mã Slot " + (i + 1) + ": " + ex.Message, "Lỗi giải mã", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (filledSlots.Count == 0)
            {
                MessageBox.Show("Vui lòng dán ít nhất 1 mã (#FF#...) vào các ô Slot 1, 2, 3 hoặc 4!", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Kiểm tra tính nhất quán giữa các slot
            byte[] firstBlock = extractedBlocks[0];
            for (int i = 1; i < extractedBlocks.Count; i++)
            {
                if (!BytesEqual(firstBlock, extractedBlocks[i]))
                {
                    MessageBox.Show("Cảnh báo: Khối trích xuất ở Slot " + filledSlots[i] + " không khớp với Slot " + filledSlots[0] + "!\n"
                        + "Slot " + filledSlots[0] + ": [" + string.Join(", ", firstBlock) + "]\n"
                        + "Slot " + filledSlots[i] + ": [" + string.Join(", ", extractedBlocks[i]) + "]",
                        "Không đồng nhất", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Bắt đầu chạy Progress Bar
            StartProgress("Đang phân tích và nhập dữ liệu nhân vật...", () => {
                // Kiểm tra xem khối đã có trong database chưa
                string existingName = FindCharacterByBlock(firstBlock);
                string blockStr = "[" + string.Join(", ", firstBlock) + "]";

                if (!string.IsNullOrEmpty(existingName))
                {
                    AppendLog(string.Format("==> NHẬP DỮ LIỆU: Nhân vật đã tồn tại!\r\n- Tên: {0}\r\n- Khối byte: {1}\r\n- Đã kiểm tra qua các slot: {2}\r\n",
                        existingName, blockStr, string.Join(", ", filledSlots)));
                    txtResultCode.Text = existingName;
                    PopulateChoiceboxes(existingName);
                    MessageBox.Show("Nhân vật đã có sẵn trong cơ sở dữ liệu:\n- Tên: " + existingName + "\n- Khối: " + blockStr, "Đã nhận diện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Nhân vật mới!
                    string newName = detectedName;
                    if (string.IsNullOrEmpty(newName))
                    {
                        newName = PromptDialog.Show("Phát hiện nhân vật mới!\nKhối byte: " + blockStr + "\nVui lòng nhập tên nhân vật:", "Nhập tên nhân vật mới", "");
                    }
                    else
                    {
                        newName = PromptDialog.Show("Phát hiện nhân vật mới!\nKhối byte: " + blockStr + "\nXác nhận hoặc chỉnh sửa tên nhân vật:", "Xác nhận tên", newName);
                    }

                    if (string.IsNullOrEmpty(newName))
                    {
                        AppendLog("Đã hủy lưu nhân vật mới (khối: " + blockStr + ").");
                        return;
                    }

                    SaveCharacter(newName, firstBlock);
                    PopulateChoiceboxes(newName);
                    txtResultCode.Text = newName;
                    AppendLog(string.Format("==> THÀNH CÔNG: Đã thêm nhân vật mới!\r\n- Tên: {0}\r\n- Khối byte: {1}\r\n- Đã lưu vào characters.json\r\n- Cập nhật danh sách Choicebox.\r\n",
                        newName, blockStr));
                    MessageBox.Show("Đã lưu thành công nhân vật '" + newName + "' vào hệ thống!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        // -------------------------------------------------------------
        // Xử lý Phần 2: Ghép thành combo
        // -------------------------------------------------------------
        private void BtnBuildCombo_Click(object sender, EventArgs e)
        {
            string s1 = cbChoice1.SelectedItem != null ? cbChoice1.SelectedItem.ToString() : "none";
            string s2 = cbChoice2.SelectedItem != null ? cbChoice2.SelectedItem.ToString() : "none";
            string s3 = cbChoice3.SelectedItem != null ? cbChoice3.SelectedItem.ToString() : "none";
            string s4 = cbChoice4.SelectedItem != null ? cbChoice4.SelectedItem.ToString() : "none";
            bool shop = cbChoice5.SelectedIndex == 1;
            int pet = cbChoice6.SelectedIndex;

            StartProgress("Đang tính toán mã combo...", () => {
                try
                {
                    string code = Encode(new string[] { s1, s2, s3, s4 }, shop, pet);
                    txtResultCode.Text = code;

                    // Tự động kiểm tra giải ngược (Reverse Verification)
                    var dec = Decode(code);
                    bool verifyOk = (dec.Slots[0] == s1 && dec.Slots[1] == s2 && dec.Slots[2] == s3 && dec.Slots[3] == s4
                                     && dec.Shop == shop && dec.Pet == pet);

                    // Phân loại: Kiểm chứng hay Suy luận
                    bool isInferred = (shop && pet > 0);
                    string status = isInferred ? "SUY LUẬN (Bật đồng thời cả Pet lẫn Shop Tool)" : "ĐÃ KIỂM CHỨNG (Khớp cấu trúc chuẩn)";

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine("KẾT QUẢ TÍNH COMBO FREE FIRE:");
                    sb.AppendLine("Mã: " + code);
                    sb.AppendLine("Tình trạng: " + status);
                    sb.AppendLine("Giải ngược tự động: " + (verifyOk ? "PASS (100% Khớp)" : "FAIL"));
                    string petName = (pet == 0 ? "Không Pet" : (pet == 1 ? "Cánh cụt" : "Ottero"));
                    sb.AppendLine(string.Format("Tổ hợp: {0} + {1} + {2} + {3} | Shop: {4} | Trợ thủ: {5}",
                        s1, s2, s3, s4, (shop ? "Có" : "Không"), petName));
                    sb.AppendLine("Độ dài byte: " + dec.Length);
                    sb.AppendLine("==================================================");

                    AppendLog(sb.ToString());
                    lblProgressText.Text = "Tính toán hoàn tất! (" + (verifyOk ? "PASS" : "Cảnh báo") + ")";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi ghép combo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblProgressText.Text = "Lỗi khi ghép combo!";
                }
            });
        }

        // -------------------------------------------------------------
        // Xử lý Phần 3: Sao chép & Kiểm tra giải ngược
        // -------------------------------------------------------------
        private void BtnCopyResult_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResultCode.Text))
            {
                Clipboard.SetText(txtResultCode.Text);
                lblProgressText.Text = "Đã sao chép mã combo vào bộ nhớ tạm (Clipboard)!";
            }
        }

        private void BtnVerifyResult_Click(object sender, EventArgs e)
        {
            string code = txtResultCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Chưa có mã để giải ngược!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StartProgress("Đang giải ngược và kiểm tra mã...", () => {
                try
                {
                    var dec = Decode(code);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("--- KẾT QUẢ GIẢI NGƯỢC CHI TIẾT ---");
                    sb.AppendLine("Mã nguồn: " + code);
                    sb.AppendLine("Độ dài: " + dec.Length + " byte");
                    sb.AppendLine("Slot 1: " + dec.Slots[0]);
                    sb.AppendLine("Slot 2: " + dec.Slots[1]);
                    sb.AppendLine("Slot 3: " + dec.Slots[2]);
                    sb.AppendLine("Slot 4: " + dec.Slots[3]);
                    sb.AppendLine("Shop: " + (dec.Shop ? "Có Shop Tool" : "Không Shop"));
                    string petName = (dec.Pet == 0 ? "Không Pet" : (dec.Pet == 1 ? "Cánh cụt" : "Ottero"));
                    sb.AppendLine("Trợ thủ (Pet): " + petName);
                    sb.AppendLine("-----------------------------------");
                    AppendLog(sb.ToString());
                    lblProgressText.Text = "Giải ngược thành công!";
                    MessageBox.Show(sb.ToString(), "Kết quả giải ngược", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi giải ngược: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void AppendLog(string text)
        {
            txtLogDetails.AppendText(text + Environment.NewLine);
        }

        // -------------------------------------------------------------
        // Các hàm giải mã / mã hóa logic
        // -------------------------------------------------------------
        private byte[] XorKey(byte[] data)
        {
            int n = data.Length;
            byte[] res = new byte[n];
            for (int i = 0; i < n; i++)
            {
                res[i] = (byte)(data[i] ^ KEY[(n - 1 - i) % 22]);
            }
            return res;
        }

        private byte[] BuildPlain(string[] slots, bool shop, int pet)
        {
            List<byte> p = new List<byte>();
            p.Add(0); // header
            for (int i = 0; i < 4; i++)
            {
                p.Add(SEP);
                string s = (i < slots.Length) ? slots[i] : "none";
                if (string.IsNullOrEmpty(s) || s.ToLower() == "none" || !blocks.ContainsKey(s))
                {
                    p.AddRange(NONE_SEG);
                }
                else
                {
                    p.AddRange(blocks[s]);
                }
            }
            p.Add(SEP);
            p.AddRange(shop ? SHOP_SEG : NONE_SEG);
            p.Add(SEP);
            p.AddRange(FIXED_SEG);
            p.Add(SEP);
            p.AddRange(PET_SEG.ContainsKey(pet) ? PET_SEG[pet] : NONE_SEG);
            return p.ToArray();
        }

        private string Encode(string[] slots, bool shop, int pet)
        {
            byte[] plain = BuildPlain(slots, shop, pet);
            byte[] raw = XorKey(plain);
            return "#FF#" + Convert.ToBase64String(raw);
        }

        private byte[] PlainOf(string code)
        {
            string s = code.Trim();
            if (s.StartsWith("#FF#")) s = s.Substring(4);
            byte[] raw = Convert.FromBase64String(s);
            return XorKey(raw);
        }

        private List<byte[]> SplitSegments(byte[] plain)
        {
            List<byte[]> segs = new List<byte[]>();
            List<byte> cur = new List<byte>();
            for (int i = 2; i < plain.Length; i++)
            {
                if (plain[i] == SEP)
                {
                    segs.Add(cur.ToArray());
                    cur.Clear();
                }
                else
                {
                    cur.Add(plain[i]);
                }
            }
            segs.Add(cur.ToArray());
            return segs;
        }

        private struct DecodeResult
        {
            public string[] Slots;
            public bool Shop;
            public int Pet;
            public int Length;
        }

        private DecodeResult Decode(string code)
        {
            byte[] p = PlainOf(code);
            if (p.Length < 2 || p[0] != 0 || p[1] != SEP)
                throw new Exception("Header sai - mã không đúng định dạng!");

            List<byte[]> segs = SplitSegments(p);
            if (segs.Count != 7)
                throw new Exception("Số đoạn byte không chuẩn (cần 7 đoạn, nhận " + segs.Count + ")!");

            string[] s = new string[4];
            for (int i = 0; i < 4; i++)
            {
                byte[] seg = segs[i];
                if (BytesEqual(seg, NONE_SEG))
                {
                    s[i] = "none";
                }
                else
                {
                    string found = FindCharacterByBlock(seg);
                    s[i] = !string.IsNullOrEmpty(found) ? found : ("UNKNOWN[" + string.Join(",", seg) + "]");
                }
            }

            bool shop = BytesEqual(segs[4], SHOP_SEG);
            int pet = 0;
            if (BytesEqual(segs[6], PET_SEG[1])) pet = 1;
            else if (BytesEqual(segs[6], PET_SEG[2])) pet = 2;

            DecodeResult res = new DecodeResult();
            res.Slots = s;
            res.Shop = shop;
            res.Pet = pet;
            res.Length = p.Length;
            return res;
        }

        private string FindCharacterByBlock(byte[] block)
        {
            string[] canonical = new string[] { "Tatsuya", "Alok (Pre)", "BHXK", "Phun lửa", "Chidori", "Alok", "Kelly", "Xayne", "Rin", "Ignis", "Dasha" };
            foreach (var c in canonical)
            {
                if (blocks.ContainsKey(c) && BytesEqual(blocks[c], block))
                {
                    return c;
                }
            }
            foreach (var kvp in blocks)
            {
                if (BytesEqual(kvp.Value, block))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public static bool RunSelfTest()
        {
            MainForm m = new MainForm();
            string c1 = m.Encode(new string[] { "BHXK", "Chidori", "Kelly", "Dasha" }, true, 2);
            string exp1 = "#FF#fmJweHxpfG1ucnR/e3l3Z3F5dWxhZXlidH98aXV7b3VwaXJvcHRwe2JqZGV/dHB/";
            if (c1 != exp1) {
                Console.WriteLine("FAIL c1: " + c1);
                return false;
            }
            var d1 = m.Decode(c1);
            if (d1.Slots[0] != "BHXK" || d1.Slots[1] != "Chidori" || d1.Slots[2] != "Kelly" || d1.Slots[3] != "Dasha" || !d1.Shop || d1.Pet != 2) {
                Console.WriteLine("FAIL decode d1");
                return false;
            }

            string c2 = m.Encode(new string[] { "Tatsuya" }, false, 1);
            string exp2 = "#FF#YnlsYH90dGl8f3Vtb2N0aXJvcHRwe2JqZGV/dHF2";
            if (c2 != exp2) {
                Console.WriteLine("FAIL c2: " + c2);
                return false;
            }

            string c3 = m.Encode(new string[] { "Alok" }, false, 0);
            string exp3 = "#FF#aGN5bXV1eWVvY2JneHVvcmR/cWd/";
            if (c3 != exp3) {
                Console.WriteLine("FAIL c3: " + c3);
                return false;
            }

            using (GuideDialog gd = new GuideDialog())
            {
                if (gd.Controls.Count < 3) return false;
            }

            Console.WriteLine("FF_Combo_Calculator Win32 Core: PASS (Verified with combo_codec)");
            return true;
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }

    // -----------------------------------------------------------------
    // Hộp thoại Win32 Native cổ điển để nhập tên nhân vật
    // -----------------------------------------------------------------
    public class PromptDialog : Form
    {
        private TextBox txtInput;
        private Button btnOk, btnCancel;
        private Label lblPrompt;

        public static string Show(string prompt, string title, string defaultValue = "")
        {
            using (PromptDialog dlg = new PromptDialog(prompt, title, defaultValue))
            {
                return dlg.ShowDialog() == DialogResult.OK ? dlg.txtInput.Text.Trim() : "";
            }
        }

        public PromptDialog(string prompt, string title, string defaultValue)
        {
            this.Text = title;
            this.Size = new Size(420, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F);

            lblPrompt = new Label() { Text = prompt, Location = new Point(15, 15), Size = new Size(375, 45) };
            txtInput = new TextBox() { Location = new Point(15, 65), Size = new Size(375, 23), Text = defaultValue };

            btnOk = new Button() { Text = "Đồng ý", DialogResult = DialogResult.OK, Location = new Point(220, 105), Size = new Size(80, 25) };
            btnCancel = new Button() { Text = "Hủy bỏ", DialogResult = DialogResult.Cancel, Location = new Point(310, 105), Size = new Size(80, 25) };

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }

    // -----------------------------------------------------------------
    // Hộp thoại Win32 Native cổ điển hướng dẫn cách lấy mã slot
    // -----------------------------------------------------------------
    public class GuideDialog : Form
    {
        public static void ShowGuide(IWin32Window owner)
        {
            using (GuideDialog dlg = new GuideDialog())
            {
                dlg.ShowDialog(owner);
            }
        }

        public GuideDialog()
        {
            this.Text = "Hướng dẫn: Cách lấy mã slot";
            this.Size = new Size(720, 755);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F);

            Label lblGuide = new Label() {
                Text = "Để lấy mã của các slot để nhập một nhân vật mới vào hộp chọn trong phần ghép combo thì hãy vào game rồi chọn 1 nhân vật duy nhất cần nhập vào rồi làm theo hình ảnh để lấy mã các slot của nhân vật mong muốn.",
                Location = new Point(16, 12),
                Size = new Size(672, 50),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(20, 20, 20)
            };

            PictureBox pb = new PictureBox() {
                Location = new Point(16, 68),
                Size = new Size(672, 600),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black
            };

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "huong_dan_slot.png");
            if (File.Exists(localPath))
            {
                try { pb.Image = Image.FromFile(localPath); } catch { }
            }
            if (pb.Image == null)
            {
                try
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    using (var s = asm.GetManifestResourceStream("huong_dan_slot.png"))
                    {
                        if (s != null) pb.Image = Image.FromStream(s);
                    }
                } catch { }
            }

            Button btnClose = new Button() {
                Text = "Đóng",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 680),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };

            this.Controls.Add(lblGuide);
            this.Controls.Add(pb);
            this.Controls.Add(btnClose);
            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;
        }
    }
}
