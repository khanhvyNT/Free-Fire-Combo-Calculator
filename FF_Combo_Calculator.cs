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

    public static class I18n
    {
        public static string CurrentLang = "vi";

        public static string T(string vi, string en)
        {
            return (CurrentLang == "en") ? en : vi;
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

        public const byte SEP = 23;
        public static readonly byte[] NONE_SEG = new byte[] { 1 };
        public static readonly byte[] SHOP_SEG = new byte[] { 4, 1, 1, 1, 1, 1, 1, 1, 5 };
        public static readonly byte[] FIXED_SEG = new byte[] { 0, 1, 4 };
        public static readonly Dictionary<int, byte[]> PET_SEG = new Dictionary<int, byte[]>() {
            { 0, new byte[] { 1 } },
            { 1, new byte[] { 0, 2, 0, 4, 1, 1, 1, 1, 1, 8 } },
            { 2, new byte[] { 0, 2, 0, 4, 1, 1, 1, 1, 0, 1 } }
        };

        private Dictionary<string, byte[]> blocks = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        // -------------------------------------------------------------
        // Giao diện người dùng (Win32 Native Cổ Điển)
        // -------------------------------------------------------------
        // Thanh chọn ngôn ngữ (Language bar)
        private Panel pnlHeader;
        private Label lblLang;
        private ComboBox cbLanguage;

        // Phần 1: Nhập dữ liệu nhân vật
        private GroupBox grpPart1;
        private Label lblSlot1;
        private TextBox txtSlot1;
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
        private Label lblLog;
        private TextBox txtLogDetails;

        private Timer timerProgress;
        private int targetProgress = 0;
        private Action onProgressComplete = null;

        public MainForm()
        {
            InitializeComponent();
            LoadCharacterDatabase();
            PopulateChoiceboxes();
            SetLanguage("vi");
        }

        private void InitializeComponent()
        {
            this.Text = "Free Fire Combo Calculator";
            this.Size = new Size(820, 725);
            this.MinimumSize = new Size(800, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            // ---------------------------------------------------------
            // THANH CHỌN NGÔN NGỮ (LANGUAGE BAR)
            // ---------------------------------------------------------
            pnlHeader = new Panel();
            pnlHeader.Location = new Point(12, 6);
            pnlHeader.Size = new Size(780, 26);
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblLang = new Label();
            lblLang.Text = "Ngôn ngữ / Language:";
            lblLang.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLang.Location = new Point(480, 3);
            lblLang.Size = new Size(180, 20);
            lblLang.TextAlign = ContentAlignment.MiddleRight;
            lblLang.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            cbLanguage = new ComboBox();
            cbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLanguage.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            cbLanguage.Location = new Point(665, 1);
            cbLanguage.Size = new Size(115, 23);
            cbLanguage.Items.Add("Tiếng Việt");
            cbLanguage.Items.Add("English");
            cbLanguage.SelectedIndex = 0;
            cbLanguage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbLanguage.SelectedIndexChanged += (s, e) => {
                SetLanguage(cbLanguage.SelectedIndex == 1 ? "en" : "vi");
            };

            pnlHeader.Controls.Add(lblLang);
            pnlHeader.Controls.Add(cbLanguage);

            // ---------------------------------------------------------
            // 1. NHẬP DỮ LIỆU NHÂN VẬT
            // ---------------------------------------------------------
            grpPart1 = new GroupBox();
            grpPart1.Text = "1. Nhập dữ liệu nhân vật";
            grpPart1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grpPart1.Location = new Point(12, 34);
            grpPart1.Size = new Size(780, 92);
            grpPart1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Font normalFont = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Slot 1 Code Combo
            lblSlot1 = new Label() { Text = "Slot 1 Code Combo:", Location = new Point(15, 26), Size = new Size(130, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtSlot1 = new TextBox() { Location = new Point(150, 23), Size = new Size(610, 23), Font = normalFont, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Nút Đăng ký nhân vật ở dưới
            btnImport = new Button();
            btnImport.Text = "Đăng ký nhân vật";
            btnImport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnImport.Location = new Point(150, 54);
            btnImport.Size = new Size(140, 26);
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += new EventHandler(BtnImport_Click);

            btnClearInputs = new Button();
            btnClearInputs.Text = "Xóa trắng";
            btnClearInputs.Font = normalFont;
            btnClearInputs.Location = new Point(298, 54);
            btnClearInputs.Size = new Size(82, 26);
            btnClearInputs.UseVisualStyleBackColor = true;
            btnClearInputs.Click += (s, e) => {
                txtSlot1.Clear();
            };

            btnGuide = new Button();
            btnGuide.Text = "Hướng dẫn";
            btnGuide.Font = normalFont;
            btnGuide.Location = new Point(388, 54);
            btnGuide.Size = new Size(100, 26);
            btnGuide.UseVisualStyleBackColor = true;
            btnGuide.Click += (s, e) => GuideDialog.ShowGuide(this);

            lblGuideHint = new Label();
            lblGuideHint.Text = "(* Bấm 'Hướng dẫn' để xem các bước lấy mã từ game)";
            lblGuideHint.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
            lblGuideHint.ForeColor = Color.FromArgb(85, 85, 85);
            lblGuideHint.Location = new Point(496, 58);
            lblGuideHint.Size = new Size(264, 20);
            lblGuideHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            grpPart1.Controls.Add(lblSlot1);
            grpPart1.Controls.Add(txtSlot1);
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
            grpPart2.Location = new Point(12, 132);
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
            grpPart3.Location = new Point(12, 253);
            grpPart3.Size = new Size(780, 420);
            grpPart3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lblProgressText = new Label() { Text = "Tiến độ sẵn sàng", Location = new Point(15, 23), Size = new Size(300, 16), Font = subHeaderFont };
            progressBar = new ProgressBar() { Location = new Point(15, 40), Size = new Size(750, 18), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            lblResultHeader = new Label() { Text = "Mã Combo Free Fire (#FF#...):", Location = new Point(15, 66), Size = new Size(200, 18), Font = normalFont };
            txtResultCode = new TextBox() {
                Location = new Point(15, 87),
                Size = new Size(540, 25),
                Font = new Font("Consolas", 10F, FontStyle.Bold),
                ReadOnly = false,
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
            btnVerifyResult.Text = "Giải Combo";
            btnVerifyResult.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnVerifyResult.Location = new Point(670, 86);
            btnVerifyResult.Size = new Size(95, 27);
            btnVerifyResult.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnVerifyResult.UseVisualStyleBackColor = true;
            btnVerifyResult.Click += new EventHandler(BtnAnalyzeCombo_Click);

            lblLog = new Label() { Text = "Chi tiết phân tích & Nhật ký:", Location = new Point(15, 120), Size = new Size(250, 18), Font = normalFont };
            txtLogDetails = new TextBox() {
                Location = new Point(15, 140),
                Size = new Size(750, 195),
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
            this.Controls.Add(pnlHeader);
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

        public void SetLanguage(string lang)
        {
            I18n.CurrentLang = lang;

            // Header
            lblLang.Text = "Ngôn ngữ / Language:";

            // Phần 1
            grpPart1.Text = I18n.T("1. Nhập dữ liệu nhân vật", "1. Character Data Input");
            lblSlot1.Text = I18n.T("Slot 1 Code Combo:", "Slot 1 Code Combo:");
            btnImport.Text = I18n.T("Đăng ký nhân vật", "Register Character");
            btnClearInputs.Text = I18n.T("Xóa trắng", "Clear");
            btnGuide.Text = I18n.T("Hướng dẫn", "Guide");
            lblGuideHint.Text = I18n.T("(* Bấm 'Hướng dẫn' để xem các bước lấy mã từ game)",
                                      "(* Click 'Guide' to view steps on getting code from game)");

            // Phần 2
            grpPart2.Text = I18n.T("2. Ghép thành combo", "2. Assemble Combo");
            lblH1.Text = I18n.T("Slot 1 (Chủ động)", "Slot 1 (Active)");
            lblH2.Text = I18n.T("Slot 2 (Bị động)", "Slot 2 (Passive)");
            lblH3.Text = I18n.T("Slot 3 (Bị động)", "Slot 3 (Passive)");
            lblH4.Text = I18n.T("Slot 4 (Bị động)", "Slot 4 (Passive)");
            lblH5.Text = "Shop Tool";
            lblH6.Text = I18n.T("Trợ Thủ (Pet)", "Pet Companion");

            int oldShop = cbChoice5.SelectedIndex;
            cbChoice5.Items.Clear();
            if (lang == "en")
            {
                cbChoice5.Items.Add("No Shop Tool");
                cbChoice5.Items.Add("With Shop Tool");
            }
            else
            {
                cbChoice5.Items.Add("Không Shop");
                cbChoice5.Items.Add("Có Shop Tool");
            }
            cbChoice5.SelectedIndex = (oldShop >= 0 && oldShop < cbChoice5.Items.Count) ? oldShop : 0;

            int oldPet = cbChoice6.SelectedIndex;
            cbChoice6.Items.Clear();
            if (lang == "en")
            {
                cbChoice6.Items.Add("No Pet (0)");
                cbChoice6.Items.Add("Mr. Waggor (Penguin)");
                cbChoice6.Items.Add("Ottero");
            }
            else
            {
                cbChoice6.Items.Add("Không Pet (0)");
                cbChoice6.Items.Add("Cánh cụt");
                cbChoice6.Items.Add("Ottero");
            }
            cbChoice6.SelectedIndex = (oldPet >= 0 && oldPet < cbChoice6.Items.Count) ? oldPet : 0;

            btnBuildCombo.Text = I18n.T("Ghép Combo", "Assemble Combo");
            btnResetCombo.Text = I18n.T("Đặt lại", "Reset");

            // Phần 3
            grpPart3.Text = I18n.T("3. Thanh progress và kết quả tính combo", "3. Progress & Calculation Result");
            if (progressBar.Value == 0 || progressBar.Value == 100)
            {
                lblProgressText.Text = I18n.T("Tiến độ sẵn sàng", "Ready");
            }
            lblResultHeader.Text = I18n.T("Mã Combo Free Fire (#FF#...):", "Free Fire Combo Code (#FF#...):");
            btnCopyResult.Text = I18n.T("Sao chép mã", "Copy Code");
            btnVerifyResult.Text = I18n.T("Giải Combo", "Solve Combo");
            if (lblLog != null)
            {
                lblLog.Text = I18n.T("Chi tiết phân tích & Nhật ký:", "Analysis Details & Activity Log:");
            }
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
        // Xử lý Phần 1: Đăng ký nhân vật (tính toán từ 1 mã Slot 1)
        // -------------------------------------------------------------
        private void BtnImport_Click(object sender, EventArgs e)
        {
            string text = txtSlot1.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show(
                    I18n.T("Vui lòng dán mã combo (#FF#...) của nhân vật vào ô Slot 1 Code Combo!",
                           "Please paste character combo code (#FF#...) into Slot 1 Code Combo!"),
                    I18n.T("Chưa có dữ liệu", "No Input Data"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string detectedName = "";
            Match nameMatch = Regex.Match(text, @"^([A-Za-z0-9_\s\(\)\p{L}]+?)[\s\:\;\(]+#FF#");
            if (nameMatch.Success)
            {
                string candidate = nameMatch.Groups[1].Value.Trim();
                candidate = Regex.Replace(candidate, @"\(\d+\)", "").Trim();
                if (!string.IsNullOrEmpty(candidate)) detectedName = candidate;
            }

            Match m = Regex.Match(text, @"#FF#([A-Za-z0-9+/=]+)");
            string code = m.Success ? ("#FF#" + m.Groups[1].Value) : text;

            byte[] targetBlock = null;
            try
            {
                byte[] p = PlainOf(code);
                if (p.Length < 2 || p[0] != 0 || p[1] != SEP)
                {
                    MessageBox.Show(
                        I18n.T("Mã combo có header không đúng định dạng Free Fire!", "Invalid combo code header format!"),
                        I18n.T("Lỗi định dạng", "Format Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                List<byte[]> segs = SplitSegments(p);
                if (segs.Count < 4)
                {
                    MessageBox.Show(
                        I18n.T("Mã combo không đủ các đoạn dữ liệu nhân vật!", "Combo code lacks character data segments!"),
                        I18n.T("Lỗi cấu trúc", "Structure Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Lấy slot 1 trước, nếu slot 1 trống thì tìm slot không trống đầu tiên
                if (!BytesEqual(segs[0], NONE_SEG))
                {
                    targetBlock = segs[0];
                }
                else
                {
                    for (int i = 1; i < 4; i++)
                    {
                        if (!BytesEqual(segs[i], NONE_SEG))
                        {
                            targetBlock = segs[i];
                            break;
                        }
                    }
                }

                if (targetBlock == null)
                {
                    MessageBox.Show(
                        I18n.T("Mã combo nhập vào là combo trống (cả 4 slot đều trống, không có nhân vật nào)!",
                               "The entered combo code is empty (all 4 slots are empty, no character found)!"),
                        I18n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(I18n.T("Không thể giải mã combo: {0}", "Cannot decode combo: {0}"), ex.Message),
                    I18n.T("Lỗi giải mã", "Decode Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            StartProgress(I18n.T("Đang phân tích và đăng ký nhân vật...", "Analyzing and registering character..."), () => {
                string existingName = FindCharacterByBlock(targetBlock);
                string blockStr = "[" + string.Join(", ", targetBlock) + "]";

                if (!string.IsNullOrEmpty(existingName))
                {
                    AppendLog(string.Format(I18n.T(
                        "==> ĐĂNG KÝ NHÂN VẬT: Nhân vật đã tồn tại!\r\n- Tên: {0}\r\n- Khối byte: {1}\r\n- Đã tự động chọn vào Choicebox 1.\r\n",
                        "==> REGISTER CHARACTER: Character already exists!\r\n- Name: {0}\r\n- Byte block: {1}\r\n- Automatically selected in Choicebox 1.\r\n"),
                        existingName, blockStr));
                    txtResultCode.Text = existingName;
                    PopulateChoiceboxes(existingName);
                    lblProgressText.Text = string.Format(I18n.T("Đã nhận diện: {0}", "Identified: {0}"), existingName);
                    string existBoxMsg = string.Format(I18n.T(
                        "Nhân vật này đã có sẵn trong cơ sở dữ liệu:\n- Tên: {0}\n- Khối byte: {1}",
                        "This character already exists in the database:\n- Name: {0}\n- Byte block: {1}"), existingName, blockStr);
                    MessageBox.Show(existBoxMsg, I18n.T("Đã nhận diện", "Identified"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string promptMsg = string.IsNullOrEmpty(detectedName)
                        ? string.Format(I18n.T("Phát hiện nhân vật mới!\nKhối byte: {0}\nVui lòng nhập tên nhân vật:",
                                              "New character detected!\nByte block: {0}\nPlease enter character name:"), blockStr)
                        : string.Format(I18n.T("Phát hiện nhân vật mới!\nKhối byte: {0}\nXác nhận hoặc chỉnh sửa tên nhân vật:",
                                              "New character detected!\nByte block: {0}\nConfirm or edit character name:"), blockStr);
                    string promptTitle = string.IsNullOrEmpty(detectedName)
                        ? I18n.T("Đăng ký nhân vật mới", "Register New Character")
                        : I18n.T("Xác nhận tên nhân vật", "Confirm Character Name");

                    string newName = PromptDialog.Show(promptMsg, promptTitle, detectedName ?? "");

                    if (string.IsNullOrEmpty(newName))
                    {
                        AppendLog(string.Format(I18n.T("Đã hủy đăng ký nhân vật mới (khối byte: {0}).",
                                                      "Cancelled registering new character (byte block: {0})."), blockStr));
                        lblProgressText.Text = I18n.T("Đã hủy đăng ký.", "Registration cancelled.");
                        return;
                    }

                    SaveCharacter(newName, targetBlock);
                    PopulateChoiceboxes(newName);
                    txtResultCode.Text = newName;
                    AppendLog(string.Format(I18n.T(
                        "==> THÀNH CÔNG: Đã đăng ký nhân vật mới!\r\n- Tên: {0}\r\n- Khối byte: {1}\r\n- Đã lưu vào characters.json\r\n- Cập nhật danh sách Choicebox.\r\n",
                        "==> SUCCESS: Registered new character!\r\n- Name: {0}\r\n- Byte block: {1}\r\n- Saved to characters.json\r\n- Updated Choicebox list.\r\n"),
                        newName, blockStr));
                    lblProgressText.Text = string.Format(I18n.T("Đã đăng ký thành công: {0}", "Successfully registered: {0}"), newName);
                    MessageBox.Show(string.Format(I18n.T("Đã đăng ký thành công nhân vật '{0}' vào hệ thống!",
                                                        "Successfully registered character '{0}' into the system!"), newName),
                        I18n.T("Thành công", "Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            StartProgress(I18n.T("Đang tính toán mã combo...", "Calculating combo code..."), () => {
                try
                {
                    string code = Encode(new string[] { s1, s2, s3, s4 }, shop, pet);
                    txtResultCode.Text = code;

                    var dec = Decode(code);
                    bool verifyOk = (dec.Slots[0] == s1 && dec.Slots[1] == s2 && dec.Slots[2] == s3 && dec.Slots[3] == s4
                                     && dec.Shop == shop && dec.Pet == pet);

                    bool isInferred = (shop && pet > 0);
                    string status = isInferred
                        ? I18n.T("SUY LUẬN (Bật đồng thời cả Pet lẫn Shop Tool)", "INFERRED (Simultaneous Pet & Shop Tool)")
                        : I18n.T("ĐÃ KIỂM CHỨNG (Khớp cấu trúc chuẩn)", "VERIFIED (Standard verified structure)");

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine(I18n.T("KẾT QUẢ TÍNH COMBO FREE FIRE:", "FREE FIRE COMBO CALCULATION RESULT:"));
                    sb.AppendLine(I18n.T("Mã: ", "Code: ") + code);
                    sb.AppendLine(I18n.T("Tình trạng: ", "Status: ") + status);
                    sb.AppendLine(I18n.T("Giải ngược tự động: ", "Auto-Verification: ") + (verifyOk ? I18n.T("PASS (100% Khớp)", "PASS (100% Match)") : "FAIL"));
                    string petName = (pet == 0 ? I18n.T("Không Pet", "No Pet") : (pet == 1 ? I18n.T("Cánh cụt", "Mr. Waggor (Penguin)") : "Ottero"));
                    sb.AppendLine(string.Format(I18n.T("Tổ hợp: {0} + {1} + {2} + {3} | Shop: {4} | Trợ thủ: {5}", "Combination: {0} + {1} + {2} + {3} | Shop: {4} | Pet: {5}"),
                        s1, s2, s3, s4, (shop ? I18n.T("Có", "Yes") : I18n.T("Không", "No")), petName));
                    sb.AppendLine(I18n.T("Độ dài byte: ", "Byte length: ") + dec.Length);
                    sb.AppendLine("==================================================");

                    AppendLog(sb.ToString());
                    lblProgressText.Text = I18n.T("Tính toán hoàn tất! (", "Calculation complete! (") + (verifyOk ? "PASS" : I18n.T("Cảnh báo", "Warning")) + ")";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(I18n.T("Lỗi khi ghép combo: ", "Error assembling combo: ") + ex.Message,
                        I18n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblProgressText.Text = I18n.T("Lỗi khi ghép combo!", "Error assembling combo!");
                }
            });
        }

        public void FillSlot1(string s1)
        {
            txtSlot1.Text = s1 ?? "";
        }

        public void FillSlots(string s1, string s2, string s3, string s4)
        {
            txtSlot1.Text = s1 ?? "";
        }

        // -------------------------------------------------------------
        // Xử lý Phần 3: Sao chép & Giải Combo
        // -------------------------------------------------------------
        private void BtnCopyResult_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResultCode.Text))
            {
                Clipboard.SetText(txtResultCode.Text);
                lblProgressText.Text = I18n.T("Đã sao chép mã combo vào bộ nhớ tạm (Clipboard)!", "Combo code copied to Clipboard!");
            }
        }

        private void BtnAnalyzeCombo_Click(object sender, EventArgs e)
        {
            string code = txtResultCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                code = PromptDialog.Show(I18n.T("Vui lòng dán hoặc nhập mã combo (#FF#...):", "Please paste or enter a combo code (#FF#...):"),
                    I18n.T("Giải Combo", "Solve Combo"), "");
                if (string.IsNullOrEmpty(code)) return;
                txtResultCode.Text = code;
            }

            Match m = Regex.Match(code, @"#FF#([A-Za-z0-9+/=]+)");
            if (!m.Success)
            {
                MessageBox.Show(I18n.T("Mã combo không đúng định dạng (cần chứa tiền tố #FF#)!", "Invalid combo code format (must contain #FF# prefix)!"),
                    I18n.T("Lỗi định dạng", "Format Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string cleanCode = "#FF#" + m.Groups[1].Value;

            StartProgress(I18n.T("Đang phân tích và tổng hợp combo...", "Analyzing and synthesizing combo..."), () => {
                try
                {
                    byte[] p = PlainOf(cleanCode);
                    if (p.Length < 2 || p[0] != 0 || p[1] != SEP)
                        throw new Exception(I18n.T("Header sai - mã không đúng định dạng!", "Invalid header - bad format!"));

                    List<byte[]> segs = SplitSegments(p);
                    if (segs.Count != 7)
                        throw new Exception(I18n.T("Số đoạn byte không chuẩn (cần 7 đoạn, nhận " + segs.Count + ")!", "Incorrect segment count (expected 7, received " + segs.Count + ")!"));

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine(I18n.T("KẾT QUẢ PHÂN TÍCH TỔNG HỢP COMBO FREE FIRE:", "FREE FIRE COMBO ANALYSIS & SYNTHESIS RESULT:"));
                    sb.AppendLine(I18n.T("Mã nguồn: ", "Source code: ") + cleanCode);
                    sb.AppendLine(string.Format(I18n.T("Độ dài dữ liệu: {0} byte", "Data length: {0} bytes"), p.Length));

                    for (int i = 0; i < 4; i++)
                    {
                        byte[] seg = segs[i];
                        string name;
                        if (BytesEqual(seg, NONE_SEG))
                        {
                            name = "none";
                        }
                        else
                        {
                            name = FindCharacterByBlock(seg);
                            if (string.IsNullOrEmpty(name)) name = "Unknown [" + string.Join(", ", seg) + "]";
                        }
                        sb.AppendLine(string.Format("Slot {0}: {1} (" + I18n.T("khối", "block") + ": [{2}])", i + 1, name, string.Join(", ", seg)));
                    }

                    string shopStatus = BytesEqual(segs[4], SHOP_SEG) ? I18n.T("Có Shop Tool", "With Shop Tool")
                        : (BytesEqual(segs[4], NONE_SEG) ? I18n.T("Không Shop", "No Shop Tool") : ("Unknown [" + string.Join(", ", segs[4]) + "]"));
                    sb.AppendLine("Shop Tool: " + shopStatus);

                    string petStatus;
                    if (BytesEqual(segs[6], PET_SEG[0])) petStatus = I18n.T("Không Pet", "No Pet");
                    else if (BytesEqual(segs[6], PET_SEG[1])) petStatus = I18n.T("Cánh cụt", "Mr. Waggor (Penguin)");
                    else if (BytesEqual(segs[6], PET_SEG[2])) petStatus = "Ottero";
                    else petStatus = "Unknown [" + string.Join(", ", segs[6]) + "]";
                    sb.AppendLine(I18n.T("Trợ thủ (Pet): ", "Pet Companion: ") + petStatus);
                    sb.AppendLine("==================================================");

                    AppendLog(sb.ToString());
                    lblProgressText.Text = I18n.T("Phân tích combo hoàn tất!", "Combo analysis complete!");

                    // Mở cửa sổ phân tích chi tiết (Detail)
                    ComboAnalysisDialog.ShowAnalysis(this, cleanCode, segs, p.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(I18n.T("Lỗi giải combo: ", "Error solving combo: ") + ex.Message,
                        I18n.T("Lỗi", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblProgressText.Text = I18n.T("Lỗi giải combo!", "Error solving combo!");
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

        public struct DecodeResult
        {
            public string[] Slots;
            public bool Shop;
            public int Pet;
            public int Length;
            public List<byte[]> Segments;
        }

        public DecodeResult Decode(string code)
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
            res.Segments = segs;
            return res;
        }

        public string FindCharacterByBlock(byte[] block)
        {
            if (block == null || (block.Length == 1 && block[0] == 1))
                return "none";

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

        public static string EncodeRawBlocks(byte[][] slots, byte[] shop, byte[] pet)
        {
            List<byte> p = new List<byte>();
            p.Add(0); // header
            for (int i = 0; i < 4; i++)
            {
                p.Add(SEP);
                byte[] s = (slots != null && i < slots.Length && slots[i] != null && slots[i].Length > 0)
                    ? slots[i]
                    : NONE_SEG;
                p.AddRange(s);
            }
            p.Add(SEP);
            p.AddRange((shop != null && shop.Length > 0) ? shop : NONE_SEG);
            p.Add(SEP);
            p.AddRange(FIXED_SEG);
            p.Add(SEP);
            p.AddRange((pet != null && pet.Length > 0) ? pet : NONE_SEG);

            byte[] raw = XorKeyStatic(p.ToArray());
            return "#FF#" + Convert.ToBase64String(raw);
        }

        public static byte[] XorKeyStatic(byte[] data)
        {
            int n = data.Length;
            byte[] res = new byte[n];
            for (int i = 0; i < n; i++)
            {
                res[i] = (byte)(data[i] ^ KEY[(n - 1 - i) % 22]);
            }
            return res;
        }

        public static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
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

            // Kiểm tra EncodeRawBlocks
            string s1Xayne = MainForm.EncodeRawBlocks(
                new byte[][] { new byte[] { 5, 5, 1, 7 }, MainForm.NONE_SEG, MainForm.NONE_SEG, MainForm.NONE_SEG },
                MainForm.NONE_SEG, MainForm.NONE_SEG);
            if (s1Xayne != "#FF#dG1rcXR5ZW9jYmd4dW9yZH9xZ38=")
            {
                Console.WriteLine("FAIL EncodeRawBlocks s1Xayne: " + s1Xayne);
                return false;
            }

            // Kiểm tra đa ngôn ngữ Tiếng Việt & English
            m.SetLanguage("en");
            if (I18n.T("Đăng ký nhân vật", "Register Character") != "Register Character") return false;
            if (m.cbChoice5.Items[0].ToString() != "No Shop Tool") return false;
            if (m.cbChoice6.Items[1].ToString() != "Mr. Waggor (Penguin)") return false;

            m.SetLanguage("vi");
            if (I18n.T("Đăng ký nhân vật", "Register Character") != "Đăng ký nhân vật") return false;
            if (m.cbChoice5.Items[0].ToString() != "Không Shop") return false;
            if (m.cbChoice6.Items[1].ToString() != "Cánh cụt") return false;

            using (GuideDialog gd = new GuideDialog())
            {
                if (gd.Controls.Count < 3) return false;
            }

            using (ComboAnalysisDialog cad = new ComboAnalysisDialog(m, c1, d1.Segments, d1.Length))
            {
                if (cad.Controls.Count < 4) return false;
            }

            Console.WriteLine("FF_Combo_Calculator Win32 Core: PASS (Verified with combo_codec)");
            return true;
        }
    }

    // -----------------------------------------------------------------
    // Hộp thoại Win32 Native cổ điển phân tích chi tiết combo (Detail)
    // -----------------------------------------------------------------
    public class ComboAnalysisDialog : Form
    {
        private MainForm mainForm;
        private string originalCode;
        private List<byte[]> segments;
        private int totalLength;

        private GroupBox grpSource;
        private TextBox txtSource;
        private Button btnCopySource;

        private GroupBox grpItems;
        private Label[] lblItemPositions;
        private Label[] lblItemNames;
        private Label[] lblItemBytes;
        private Button[] btnItemDetails;

        private GroupBox grpDetail;
        private Label lblDetailHeading;
        private Label lblDetailBadge;

        // Character detail panel
        private Panel pnlChar;
        private TextBox txtSlotCode1, txtSlotCode2, txtSlotCode3, txtSlotCode4;
        private Button btnCopyS1, btnCopyS2, btnCopyS3, btnCopyS4;
        private Button btnCopyAll4, btnFillPart1;
        private Label lblCharHint;

        // Single detail panel (Shop / Pet)
        private Panel pnlSingle;
        private Label lblSinglePrompt;
        private TextBox txtSingleCode;
        private Button btnCopySingle;
        private Label lblSingleDesc;

        private Button btnClose;

        public static void ShowAnalysis(MainForm owner, string code, List<byte[]> segs, int length)
        {
            using (ComboAnalysisDialog dlg = new ComboAnalysisDialog(owner, code, segs, length))
            {
                dlg.ShowDialog(owner);
            }
        }

        public ComboAnalysisDialog(MainForm owner, string code, List<byte[]> segs, int length)
        {
            this.mainForm = owner;
            this.originalCode = code;
            this.segments = segs;
            this.totalLength = length;

            InitializeComponent();
            SelectComponent(0);
        }

        private void InitializeComponent()
        {
            this.Text = I18n.T("Giải Combo - Phân tích chi tiết thành phần", "Solve Combo - Component Analysis & Details");
            this.Size = new Size(760, 710);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            Font boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            Font monoFont = new Font("Consolas", 9F, FontStyle.Regular);

            // GroupBox 1: Mã combo nguồn
            grpSource = new GroupBox();
            grpSource.Text = string.Format(I18n.T("Mã combo được phân tích ({0} bytes giải mã):", "Analyzed combo code ({0} decoded bytes):"), totalLength);
            grpSource.Font = boldFont;
            grpSource.Location = new Point(12, 8);
            grpSource.Size = new Size(720, 54);

            txtSource = new TextBox();
            txtSource.Location = new Point(12, 20);
            txtSource.Size = new Size(590, 23);
            txtSource.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
            txtSource.ReadOnly = true;
            txtSource.BackColor = Color.White;
            txtSource.Text = originalCode;

            btnCopySource = new Button();
            btnCopySource.Text = I18n.T("Sao chép", "Copy");
            btnCopySource.Font = boldFont;
            btnCopySource.Location = new Point(610, 19);
            btnCopySource.Size = new Size(98, 25);
            btnCopySource.UseVisualStyleBackColor = true;
            btnCopySource.Click += (s, e) => {
                Clipboard.SetText(originalCode);
                MessageBox.Show(I18n.T("Đã sao chép mã combo vào Clipboard!", "Combo code copied to Clipboard!"),
                    I18n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            grpSource.Controls.Add(txtSource);
            grpSource.Controls.Add(btnCopySource);

            // GroupBox 2: Danh sách 6 thành phần
            grpItems = new GroupBox();
            grpItems.Text = I18n.T("Các thành phần nhận diện bên trong Combo:", "Detected Components in Combo:");
            grpItems.Font = boldFont;
            grpItems.Location = new Point(12, 66);
            grpItems.Size = new Size(720, 222);

            Label col1 = new Label() { Text = I18n.T("Vị trí", "Position"), Location = new Point(15, 18), Size = new Size(125, 18), Font = boldFont };
            Label col2 = new Label() { Text = I18n.T("Tên nhận diện / Trạng thái", "Detected Name / Status"), Location = new Point(145, 18), Size = new Size(190, 18), Font = boldFont };
            Label col3 = new Label() { Text = I18n.T("Khối byte dữ liệu", "Raw Byte Block"), Location = new Point(340, 18), Size = new Size(260, 18), Font = boldFont };
            Label col4 = new Label() { Text = I18n.T("Tác vụ", "Action"), Location = new Point(610, 18), Size = new Size(98, 18), Font = boldFont };
            grpItems.Controls.Add(col1);
            grpItems.Controls.Add(col2);
            grpItems.Controls.Add(col3);
            grpItems.Controls.Add(col4);

            string[] posTitles = new string[] {
                I18n.T("Slot 1 (Chủ động)", "Slot 1 (Active)"),
                I18n.T("Slot 2 (Bị động)", "Slot 2 (Passive)"),
                I18n.T("Slot 3 (Bị động)", "Slot 3 (Passive)"),
                I18n.T("Slot 4 (Bị động)", "Slot 4 (Passive)"),
                "Shop Tool",
                I18n.T("Trợ thủ (Pet)", "Pet Companion")
            };

            lblItemPositions = new Label[6];
            lblItemNames = new Label[6];
            lblItemBytes = new Label[6];
            btnItemDetails = new Button[6];

            for (int i = 0; i < 6; i++)
            {
                int y = 38 + i * 29;
                int idx = i;

                lblItemPositions[i] = new Label() {
                    Text = posTitles[i],
                    Location = new Point(15, y + 3),
                    Size = new Size(125, 18),
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                lblItemNames[i] = new Label() {
                    Location = new Point(145, y + 3),
                    Size = new Size(190, 18),
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                lblItemBytes[i] = new Label() {
                    Location = new Point(340, y + 3),
                    Size = new Size(260, 18),
                    Font = monoFont
                };

                btnItemDetails[i] = new Button() {
                    Text = "Detail ->",
                    Location = new Point(610, y),
                    Size = new Size(98, 25),
                    Font = boldFont,
                    UseVisualStyleBackColor = true
                };
                btnItemDetails[i].Click += (s, e) => SelectComponent(idx);

                if (i < 4)
                {
                    byte[] seg = (segments != null && i < segments.Count) ? segments[i] : MainForm.NONE_SEG;
                    lblItemBytes[i].Text = "[" + string.Join(", ", seg) + "]";
                    if (MainForm.BytesEqual(seg, MainForm.NONE_SEG))
                    {
                        lblItemNames[i].Text = I18n.T("none (Trống)", "none (Empty)");
                        lblItemNames[i].ForeColor = Color.Gray;
                    }
                    else
                    {
                        string found = (mainForm != null) ? mainForm.FindCharacterByBlock(seg) : null;
                        if (!string.IsNullOrEmpty(found))
                        {
                            lblItemNames[i].Text = found;
                            lblItemNames[i].ForeColor = Color.Black;
                        }
                        else
                        {
                            lblItemNames[i].Text = "Unknown";
                            lblItemNames[i].ForeColor = Color.DarkRed;
                            lblItemNames[i].Font = boldFont;
                        }
                    }
                }
                else if (i == 4)
                {
                    byte[] seg = (segments != null && segments.Count > 4) ? segments[4] : MainForm.NONE_SEG;
                    lblItemBytes[i].Text = "[" + string.Join(", ", seg) + "]";
                    if (MainForm.BytesEqual(seg, MainForm.SHOP_SEG))
                    {
                        lblItemNames[i].Text = I18n.T("Có Shop Tool", "With Shop Tool");
                        lblItemNames[i].ForeColor = Color.Black;
                    }
                    else if (MainForm.BytesEqual(seg, MainForm.NONE_SEG))
                    {
                        lblItemNames[i].Text = I18n.T("Không Shop", "No Shop Tool");
                        lblItemNames[i].ForeColor = Color.Gray;
                    }
                    else
                    {
                        lblItemNames[i].Text = "Unknown";
                        lblItemNames[i].ForeColor = Color.DarkRed;
                        lblItemNames[i].Font = boldFont;
                    }
                }
                else if (i == 5)
                {
                    byte[] seg = (segments != null && segments.Count > 6) ? segments[6] : MainForm.NONE_SEG;
                    lblItemBytes[i].Text = "[" + string.Join(", ", seg) + "]";
                    if (MainForm.BytesEqual(seg, MainForm.PET_SEG[1]))
                    {
                        lblItemNames[i].Text = I18n.T("Cánh cụt", "Mr. Waggor (Penguin)");
                        lblItemNames[i].ForeColor = Color.Black;
                    }
                    else if (MainForm.BytesEqual(seg, MainForm.PET_SEG[2]))
                    {
                        lblItemNames[i].Text = "Ottero";
                        lblItemNames[i].ForeColor = Color.Black;
                    }
                    else if (MainForm.BytesEqual(seg, MainForm.PET_SEG[0]))
                    {
                        lblItemNames[i].Text = I18n.T("Không Pet", "No Pet");
                        lblItemNames[i].ForeColor = Color.Gray;
                    }
                    else
                    {
                        lblItemNames[i].Text = "Unknown";
                        lblItemNames[i].ForeColor = Color.DarkRed;
                        lblItemNames[i].Font = boldFont;
                    }
                }

                grpItems.Controls.Add(lblItemPositions[i]);
                grpItems.Controls.Add(lblItemNames[i]);
                grpItems.Controls.Add(lblItemBytes[i]);
                grpItems.Controls.Add(btnItemDetails[i]);
            }

            // GroupBox 3: Khung chi tiết (Detail)
            grpDetail = new GroupBox();
            grpDetail.Text = I18n.T("Thông tin chi tiết (Detail):", "Component Details (Detail):");
            grpDetail.Font = boldFont;
            grpDetail.Location = new Point(12, 292);
            grpDetail.Size = new Size(720, 336);

            lblDetailHeading = new Label();
            lblDetailHeading.Location = new Point(15, 20);
            lblDetailHeading.Size = new Size(530, 22);
            lblDetailHeading.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            lblDetailBadge = new Label();
            lblDetailBadge.Location = new Point(550, 20);
            lblDetailBadge.Size = new Size(158, 22);
            lblDetailBadge.Font = boldFont;
            lblDetailBadge.TextAlign = ContentAlignment.MiddleRight;

            // Panel dành cho Nhân vật (Full 4 slot codes)
            pnlChar = new Panel();
            pnlChar.Location = new Point(15, 45);
            pnlChar.Size = new Size(693, 282);

            // Slot 1
            Label lblS1 = new Label() { Text = I18n.T("Mã combo khi ở Slot 1 (Chủ động):", "Combo code when equipped in Slot 1 (Active):"), Location = new Point(0, 2), Size = new Size(580, 16), Font = new Font("Segoe UI", 8.5F) };
            txtSlotCode1 = new TextBox() { Location = new Point(0, 19), Size = new Size(580, 23), Font = monoFont, ReadOnly = true, BackColor = Color.White };
            btnCopyS1 = new Button() { Text = I18n.T("Sao chép", "Copy"), Location = new Point(590, 18), Size = new Size(95, 25), Font = boldFont, UseVisualStyleBackColor = true };
            btnCopyS1.Click += (s, e) => { Clipboard.SetText(txtSlotCode1.Text); };

            // Slot 2
            Label lblS2 = new Label() { Text = I18n.T("Mã combo khi ở Slot 2 (Bị động):", "Combo code when equipped in Slot 2 (Passive):"), Location = new Point(0, 47), Size = new Size(580, 16), Font = new Font("Segoe UI", 8.5F) };
            txtSlotCode2 = new TextBox() { Location = new Point(0, 64), Size = new Size(580, 23), Font = monoFont, ReadOnly = true, BackColor = Color.White };
            btnCopyS2 = new Button() { Text = I18n.T("Sao chép", "Copy"), Location = new Point(590, 63), Size = new Size(95, 25), Font = boldFont, UseVisualStyleBackColor = true };
            btnCopyS2.Click += (s, e) => { Clipboard.SetText(txtSlotCode2.Text); };

            // Slot 3
            Label lblS3 = new Label() { Text = I18n.T("Mã combo khi ở Slot 3 (Bị động):", "Combo code when equipped in Slot 3 (Passive):"), Location = new Point(0, 92), Size = new Size(580, 16), Font = new Font("Segoe UI", 8.5F) };
            txtSlotCode3 = new TextBox() { Location = new Point(0, 109), Size = new Size(580, 23), Font = monoFont, ReadOnly = true, BackColor = Color.White };
            btnCopyS3 = new Button() { Text = I18n.T("Sao chép", "Copy"), Location = new Point(590, 108), Size = new Size(95, 25), Font = boldFont, UseVisualStyleBackColor = true };
            btnCopyS3.Click += (s, e) => { Clipboard.SetText(txtSlotCode3.Text); };

            // Slot 4
            Label lblS4 = new Label() { Text = I18n.T("Mã combo khi ở Slot 4 (Bị động):", "Combo code when equipped in Slot 4 (Passive):"), Location = new Point(0, 137), Size = new Size(580, 16), Font = new Font("Segoe UI", 8.5F) };
            txtSlotCode4 = new TextBox() { Location = new Point(0, 154), Size = new Size(580, 23), Font = monoFont, ReadOnly = true, BackColor = Color.White };
            btnCopyS4 = new Button() { Text = I18n.T("Sao chép", "Copy"), Location = new Point(590, 153), Size = new Size(95, 25), Font = boldFont, UseVisualStyleBackColor = true };
            btnCopyS4.Click += (s, e) => { Clipboard.SetText(txtSlotCode4.Text); };

            // Tác vụ nạp & sao chép cả 4
            btnCopyAll4 = new Button();
            btnCopyAll4.Text = I18n.T("Sao chép cả 4 mã", "Copy All 4 Codes");
            btnCopyAll4.Location = new Point(0, 188);
            btnCopyAll4.Size = new Size(150, 28);
            btnCopyAll4.Font = boldFont;
            btnCopyAll4.UseVisualStyleBackColor = true;
            btnCopyAll4.Click += (s, e) => {
                string all = string.Format("Slot 1: {0}\r\nSlot 2: {1}\r\nSlot 3: {2}\r\nSlot 4: {3}",
                    txtSlotCode1.Text, txtSlotCode2.Text, txtSlotCode3.Text, txtSlotCode4.Text);
                Clipboard.SetText(all);
                MessageBox.Show(I18n.T("Đã sao chép toàn bộ 4 mã của 4 slot vào Clipboard!", "All 4 slot codes copied to Clipboard!"),
                    I18n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnFillPart1 = new Button();
            btnFillPart1.Text = I18n.T("Nạp 4 mã vào Phần 1", "Load 4 Codes to Part 1");
            btnFillPart1.Location = new Point(160, 188);
            btnFillPart1.Size = new Size(180, 28);
            btnFillPart1.Font = boldFont;
            btnFillPart1.Text = I18n.T("Nạp mã vào Phần 1", "Load Code to Part 1");
            btnFillPart1.Location = new Point(160, 188);
            btnFillPart1.Size = new Size(180, 28);
            btnFillPart1.Font = boldFont;
            btnFillPart1.UseVisualStyleBackColor = true;
            btnFillPart1.Click += (s, e) => {
                if (mainForm != null)
                {
                    mainForm.FillSlot1(txtSlotCode1.Text);
                    string fillSuccess = string.Format(I18n.T(
                        "Đã nạp mã Slot 1 vào ô 'Slot 1 Code Combo' ở Phần 1 giao diện chính!\nBạn chỉ cần bấm nút '{0}' tại Phần 1 để lưu nhân vật này.",
                        "Successfully loaded Slot 1 code into 'Slot 1 Code Combo' in Part 1!\nSimply click '{0}' in Part 1 to register this character."),
                        I18n.T("Đăng ký nhân vật", "Register Character"));
                    MessageBox.Show(fillSuccess, I18n.T("Đã nạp thành công", "Loaded Successfully"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            };

            lblCharHint = new Label();
            lblCharHint.Text = I18n.T(
                "(* Gợi ý: Bấm 'Nạp mã vào Phần 1' để tự động điền mã Slot 1 của nhân vật này vào ô Slot 1 ở Phần 1 rồi bấm 'Đăng ký nhân vật' mà không cần copy paste thủ công)",
                "(* Tip: Click 'Load Code to Part 1' to autofill the Slot 1 code into Part 1, then click 'Register Character' without manual copy-pasting)");
            lblCharHint.Location = new Point(0, 224);
            lblCharHint.Size = new Size(685, 45);
            lblCharHint.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
            lblCharHint.ForeColor = Color.FromArgb(80, 80, 80);

            pnlChar.Controls.Add(lblS1); pnlChar.Controls.Add(txtSlotCode1); pnlChar.Controls.Add(btnCopyS1);
            pnlChar.Controls.Add(lblS2); pnlChar.Controls.Add(txtSlotCode2); pnlChar.Controls.Add(btnCopyS2);
            pnlChar.Controls.Add(lblS3); pnlChar.Controls.Add(txtSlotCode3); pnlChar.Controls.Add(btnCopyS3);
            pnlChar.Controls.Add(lblS4); pnlChar.Controls.Add(txtSlotCode4); pnlChar.Controls.Add(btnCopyS4);
            pnlChar.Controls.Add(btnCopyAll4);
            pnlChar.Controls.Add(btnFillPart1);
            pnlChar.Controls.Add(lblCharHint);

            // Panel dành cho Shop Tool / Pet (Single isolated code)
            pnlSingle = new Panel();
            pnlSingle.Location = new Point(15, 45);
            pnlSingle.Size = new Size(693, 282);

            lblSinglePrompt = new Label();
            lblSinglePrompt.Location = new Point(0, 10);
            lblSinglePrompt.Size = new Size(680, 20);
            lblSinglePrompt.Font = new Font("Segoe UI", 9F);

            txtSingleCode = new TextBox();
            txtSingleCode.Location = new Point(0, 35);
            txtSingleCode.Size = new Size(580, 23);
            txtSingleCode.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
            txtSingleCode.ReadOnly = true;
            txtSingleCode.BackColor = Color.White;

            btnCopySingle = new Button();
            btnCopySingle.Text = I18n.T("Sao chép", "Copy");
            btnCopySingle.Location = new Point(590, 34);
            btnCopySingle.Size = new Size(95, 25);
            btnCopySingle.Font = boldFont;
            btnCopySingle.UseVisualStyleBackColor = true;
            btnCopySingle.Click += (s, e) => {
                Clipboard.SetText(txtSingleCode.Text);
                MessageBox.Show(I18n.T("Đã sao chép mã độc lập vào Clipboard!", "Standalone code copied to Clipboard!"),
                    I18n.T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            lblSingleDesc = new Label();
            lblSingleDesc.Location = new Point(0, 75);
            lblSingleDesc.Size = new Size(680, 100);
            lblSingleDesc.Font = new Font("Segoe UI", 9F);
            lblSingleDesc.ForeColor = Color.FromArgb(60, 60, 60);

            pnlSingle.Controls.Add(lblSinglePrompt);
            pnlSingle.Controls.Add(txtSingleCode);
            pnlSingle.Controls.Add(btnCopySingle);
            pnlSingle.Controls.Add(lblSingleDesc);

            grpDetail.Controls.Add(lblDetailHeading);
            grpDetail.Controls.Add(lblDetailBadge);
            grpDetail.Controls.Add(pnlChar);
            grpDetail.Controls.Add(pnlSingle);

            // Nút Đóng
            btnClose = new Button();
            btnClose.Text = I18n.T("Đóng", "Close");
            btnClose.DialogResult = DialogResult.OK;
            btnClose.Location = new Point(622, 634);
            btnClose.Size = new Size(110, 28);
            btnClose.Font = boldFont;
            btnClose.UseVisualStyleBackColor = true;

            this.Controls.Add(grpSource);
            this.Controls.Add(grpItems);
            this.Controls.Add(grpDetail);
            this.Controls.Add(btnClose);
            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;
        }

        public void SelectComponent(int idx)
        {
            if (segments == null || segments.Count < 7) return;

            for (int i = 0; i < btnItemDetails.Length; i++)
            {
                if (btnItemDetails[i] != null)
                {
                    btnItemDetails[i].Text = (i == idx) ? I18n.T(">> Đang xem <<", ">> Viewing <<") : "Detail ->";
                }
            }

            if (idx >= 0 && idx < 4)
            {
                pnlChar.Visible = true;
                pnlSingle.Visible = false;

                byte[] seg = segments[idx];
                bool isNone = MainForm.BytesEqual(seg, MainForm.NONE_SEG);
                string cname = isNone ? I18n.T("none (Trống)", "none (Empty)") : (mainForm != null ? mainForm.FindCharacterByBlock(seg) : null);
                bool isUnknown = !isNone && string.IsNullOrEmpty(cname);
                if (isUnknown) cname = "Unknown";

                lblDetailHeading.Text = string.Format(I18n.T("Chi tiết Slot {0}: {1} (Khối byte: [{2}])", "Slot {0} Details: {1} (Raw bytes: [{2}])"),
                    idx + 1, cname, string.Join(", ", seg));

                if (isUnknown)
                {
                    lblDetailBadge.Text = "UNKNOWN";
                    lblDetailBadge.ForeColor = Color.DarkRed;
                }
                else if (isNone)
                {
                    lblDetailBadge.Text = I18n.T("SLOT TRỐNG", "EMPTY SLOT");
                    lblDetailBadge.ForeColor = Color.Gray;
                }
                else
                {
                    lblDetailBadge.Text = I18n.T("ĐÃ NHẬN DIỆN", "IDENTIFIED");
                    lblDetailBadge.ForeColor = Color.FromArgb(0, 130, 0);
                }

                byte[] n = MainForm.NONE_SEG;
                txtSlotCode1.Text = MainForm.EncodeRawBlocks(new byte[][] { seg, n, n, n }, n, n);
                txtSlotCode2.Text = MainForm.EncodeRawBlocks(new byte[][] { n, seg, n, n }, n, n);
                txtSlotCode3.Text = MainForm.EncodeRawBlocks(new byte[][] { n, n, seg, n }, n, n);
                txtSlotCode4.Text = MainForm.EncodeRawBlocks(new byte[][] { n, n, n, seg }, n, n);
            }
            else if (idx == 4)
            {
                pnlChar.Visible = false;
                pnlSingle.Visible = true;

                byte[] seg = segments[4];
                bool isStandard = MainForm.BytesEqual(seg, MainForm.SHOP_SEG);
                bool isNone = MainForm.BytesEqual(seg, MainForm.NONE_SEG);
                bool isUnknown = !isStandard && !isNone;
                string sname = isStandard ? I18n.T("Có Shop Tool", "With Shop Tool") : (isNone ? I18n.T("Không Shop", "No Shop Tool") : "Unknown");

                lblDetailHeading.Text = string.Format(I18n.T("Chi tiết Shop Tool: {0} (Khối byte: [{1}])", "Shop Tool Details: {0} (Raw bytes: [{1}])"),
                    sname, string.Join(", ", seg));

                if (isUnknown)
                {
                    lblDetailBadge.Text = "UNKNOWN";
                    lblDetailBadge.ForeColor = Color.DarkRed;
                }
                else
                {
                    lblDetailBadge.Text = "SHOP TOOL";
                    lblDetailBadge.ForeColor = Color.FromArgb(0, 90, 180);
                }

                lblSinglePrompt.Text = I18n.T("Mã combo riêng biệt khi chỉ kích hoạt Shop Tool này (các slot khác để trống):",
                    "Standalone combo code when only this Shop Tool is equipped (all other slots empty):");
                byte[] n = MainForm.NONE_SEG;
                txtSingleCode.Text = MainForm.EncodeRawBlocks(new byte[][] { n, n, n, n }, seg, n);
                lblSingleDesc.Text = I18n.T(
                    "Mã trên là mã độc lập đại diện cho cấu hình Shop Tool được trích xuất từ combo nguồn.\n"
                    + "Khối byte dữ liệu gốc của Shop: [" + string.Join(", ", seg) + "]\n\n"
                    + "Bạn có thể sao chép mã này để chia sẻ hoặc lưu trữ độc lập.",
                    "This code is the standalone representation of the Shop Tool extracted from the source combo.\n"
                    + "Raw byte block of Shop: [" + string.Join(", ", seg) + "]\n\n"
                    + "You can copy this code to share or store independently.");
            }
            else if (idx == 5)
            {
                pnlChar.Visible = false;
                pnlSingle.Visible = true;

                byte[] seg = segments[6];
                bool isNone = MainForm.BytesEqual(seg, MainForm.PET_SEG[0]);
                bool isPenguin = MainForm.BytesEqual(seg, MainForm.PET_SEG[1]);
                bool isOttero = MainForm.BytesEqual(seg, MainForm.PET_SEG[2]);
                bool isUnknown = !isNone && !isPenguin && !isOttero;
                string pname = isPenguin ? I18n.T("Cánh cụt", "Mr. Waggor (Penguin)") : (isOttero ? "Ottero" : (isNone ? I18n.T("Không Pet", "No Pet") : "Unknown"));

                lblDetailHeading.Text = string.Format(I18n.T("Chi tiết Trợ thủ (Pet): {0} (Khối byte: [{1}])", "Pet Companion Details: {0} (Raw bytes: [{1}])"),
                    pname, string.Join(", ", seg));

                if (isUnknown)
                {
                    lblDetailBadge.Text = "UNKNOWN";
                    lblDetailBadge.ForeColor = Color.DarkRed;
                }
                else
                {
                    lblDetailBadge.Text = I18n.T("TRỢ THỦ (PET)", "PET COMPANION");
                    lblDetailBadge.ForeColor = Color.FromArgb(0, 90, 180);
                }

                lblSinglePrompt.Text = I18n.T("Mã combo riêng biệt khi chỉ trang bị Trợ thủ này (các slot khác để trống):",
                    "Standalone combo code when only this Pet is equipped (all other slots empty):");
                byte[] n = MainForm.NONE_SEG;
                txtSingleCode.Text = MainForm.EncodeRawBlocks(new byte[][] { n, n, n, n }, n, seg);
                lblSingleDesc.Text = I18n.T(
                    "Mã trên là mã độc lập đại diện cho cấu hình Trợ thủ (Pet) được trích xuất từ combo nguồn.\n"
                    + "Khối byte dữ liệu gốc của Pet: [" + string.Join(", ", seg) + "]\n\n"
                    + "Bạn có thể sao chép mã này để chia sẻ hoặc lưu trữ độc lập.",
                    "This code is the standalone representation of the Pet extracted from the source combo.\n"
                    + "Raw byte block of Pet: [" + string.Join(", ", seg) + "]\n\n"
                    + "You can copy this code to share or store independently.");
            }
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

            btnOk = new Button() { Text = I18n.T("Đồng ý", "OK"), DialogResult = DialogResult.OK, Location = new Point(220, 105), Size = new Size(80, 25) };
            btnCancel = new Button() { Text = I18n.T("Hủy bỏ", "Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(310, 105), Size = new Size(80, 25) };

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
            this.Text = I18n.T("Hướng dẫn: Lấy mã nhân vật để đăng ký", "Guide: Extract Character Code for Registration");
            this.Size = new Size(680, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F);

            string guideTextVi =
                "Hướng dẫn: Lấy mã nhân vật để đăng ký\r\n\r\n" +
                "Mã nhân vật là gì? Đó là một dòng chữ bắt đầu bằng #FF#. Công cụ dùng dòng chữ này để nhận ra bạn đang chọn nhân vật nào. Bạn không cần hiểu nó nghĩa là gì, chỉ cần sao chép đúng.\r\n\r\n" +
                "Lưu ý: Mỗi nhân vật cần một mã riêng. Muốn đăng ký 5 nhân vật thì làm 5 lần.\r\n\r\n" +
                "Các bước\r\n\r\n" +
                "Bước 1. Đưa game về trạng thái trống.\r\n" +
                "Sao chép dòng mã combo dưới đây và dán nó vào mục combo game:\r\n" +
                "#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=\r\n" +
                "Combo này sẽ xóa sạch combo hiện tại: 4 ô trống, không pet, không công cụ hỗ trợ (CCHT).\r\n\r\n" +
                "Bước 2. Chọn nhân vật cần đăng ký.\r\n" +
                "Chọn đúng 1 nhân vật vào ô đầu tiên. Ba ô còn lại để trống. Không bật Pet hay CCHT.\r\n\r\n" +
                "Bước 3. Lấy mã.\r\n" +
                "Bấm [Sao chép mã] để copy mã cho việc đăng ký.\r\n\r\n" +
                "Bước 4. Đăng ký.\r\n" +
                "Quay lại công cụ này, dán mã vào ô nhập của Phần 1 và bấm Đăng ký.";

            string guideTextEn =
                "Guide: Extract Character Code for Registration\r\n\r\n" +
                "What is a character code? It is a text string starting with #FF#. The tool uses this code to identify which character you selected. You do not need to understand what it means, simply copy it accurately.\r\n\r\n" +
                "Note: Each character requires a unique code. If you want to register 5 characters, repeat this 5 times.\r\n\r\n" +
                "Steps\r\n\r\n" +
                "Step 1. Reset game to empty state.\r\n" +
                "Copy the combo code below and paste it into the in-game combo section:\r\n" +
                "#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=\r\n" +
                "This combo clears your active combo: 4 empty slots, no pet, no loadout/shop tool.\r\n\r\n" +
                "Step 2. Select the character to register.\r\n" +
                "Equip exactly 1 character in the first slot. Leave the other 3 slots empty. Do not equip Pet or loadout tools.\r\n\r\n" +
                "Step 3. Copy code.\r\n" +
                "Click [Copy Code] in-game to copy the code for registration.\r\n\r\n" +
                "Step 4. Register.\r\n" +
                "Return to this tool, paste the code into the input field in Part 1 and click Register Character.";

            TextBox txtGuide = new TextBox() {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Location = new Point(16, 14),
                Size = new Size(632, 435),
                Text = I18n.T(guideTextVi, guideTextEn)
            };

            Button btnCopyReset = new Button() {
                Text = I18n.T("Sao chép mã xóa trống (Bước 1)", "Copy Reset Code (Step 1)"),
                Location = new Point(16, 460),
                Size = new Size(250, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };
            btnCopyReset.Click += (s, e) => {
                Clipboard.SetText("#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=");
                MessageBox.Show(
                    I18n.T("Đã sao chép mã combo xóa trống (#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=) vào Clipboard!\nDán mã này vào mục combo trong game để xóa sạch trạng thái trước khi chọn nhân vật.",
                           "Reset combo code (#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=) copied to Clipboard!\nPaste it in game to clear active combo before selecting your character."),
                    I18n.T("Đã sao chép", "Copied"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            Button btnScreenshot = new Button() {
                Text = I18n.T("Xem ảnh minh họa", "View Screenshot"),
                Location = new Point(276, 460),
                Size = new Size(160, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                UseVisualStyleBackColor = true
            };
            btnScreenshot.Click += (s, e) => ShowScreenshot(this);

            Button btnClose = new Button() {
                Text = I18n.T("Đóng", "Close"),
                DialogResult = DialogResult.OK,
                Location = new Point(538, 460),
                Size = new Size(110, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };

            this.Controls.Add(txtGuide);
            this.Controls.Add(btnCopyReset);
            this.Controls.Add(btnScreenshot);
            this.Controls.Add(btnClose);
            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;
        }

        private static void ShowScreenshot(IWin32Window owner)
        {
            using (Form imgForm = new Form())
            {
                imgForm.Text = I18n.T("Ảnh minh họa lấy mã slot trong game", "In-Game Slot Code Screenshot");
                imgForm.Size = new Size(700, 720);
                imgForm.StartPosition = FormStartPosition.CenterParent;
                imgForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                imgForm.MaximizeBox = false;
                imgForm.MinimizeBox = false;
                imgForm.BackColor = Color.Black;

                PictureBox pb = new PictureBox() {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
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
                imgForm.Controls.Add(pb);
                imgForm.ShowDialog(owner);
            }
        }
    }
}
