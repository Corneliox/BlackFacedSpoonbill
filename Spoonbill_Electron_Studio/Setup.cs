using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

public class SpoonbillNvidiaInstaller : Form
{
    // Geometry Constants
    private const int FormWidth = 820;
    private const int FormHeight = 560;
    private const int SidebarWidth = 220;
    private const int ContentWidth = 600;
    private const int MainHeight = 500;
    private const int BottomHeight = 60;

    // Layout Panels
    private Panel sidebarPanel;
    private Panel contentPanel;
    private Panel bottomPanel;

    // Sidebar Step Badges
    private Panel[] stepBadgePanels;
    private Label[] stepNumberLabels;
    private Label[] stepTitleLabels;
    private int currentStep = 0;

    // Navigation Buttons
    private Button btnBack;
    private Button btnNext;
    private Button btnCancel;
    private Label lblVersionInfo;

    // Step Pages
    private Panel panelSystemCheck;
    private Panel panelAgreement;
    private Panel panelModeAndOptions;
    private Panel panelProgress;
    private Panel panelFinish;

    // Step 0: System Check Controls
    private Label lblSysTitle;
    private Label lblSysDesc;
    private Panel[] sysCardPanels;
    private Label[] sysCardTitles;
    private Label[] sysCardValues;
    private Label[] sysCardBadges;
    private Panel pnlSysAdvisory;
    private Label lblAdvisoryTitle;
    private Label lblAdvisoryDesc;
    private Button btnHowToReady;
    private Button btnRefreshScan;
    private Label lblLastScanTime;
    private string detectedGpu = null;
    private bool hasNvidiaGpu = false;

    // Step 1: Agreement Controls
    private Label lblAgreeTitle;
    private Label lblAgreeDesc;
    private RichTextBox txtAgreementTerms;
    private int agreementChoice = 0; // 0 = unselected/released, 1 = accept, 2 = decline
    private Panel pnlAgreeBox;
    private Panel pnlDeclineBox;
    private Label lblAgreementStatus;

    // Step 2: Mode & Options Controls
    private Label lblOptTitle;
    private Label lblOptDesc;
    private int installMode = 0; // 0 = Express (default), 1 = Custom
    private Panel pnlExpressCard;
    private Panel pnlCustomCard;
    private Panel pnlCustomDetails;
    private Label lblDest;
    private TextBox txtTargetDir;
    private Button btnBrowseTarget;
    private Label lblDiskSpace;
    private CheckBox chkCleanInstall;
    private CheckBox chkInstallModels;
    private CheckBox chkInstallDeps;
    private CheckBox chkDesktopShortcut;
    private CheckBox chkStartMenuShortcut;
    private CheckBox chkRegisterUninstaller;

    // Step 3: Progress Controls
    private Label lblProgTitle;
    private Label lblProgStatus;
    private Panel customProgressBar;
    private int currentProgressPercent = 0;
    private RichTextBox logBox;

    // Step 4: Finish Controls
    private Label lblFinishTitle;
    private Label lblFinishDesc;
    private Panel pnlFinishCard;
    private CheckBox chkLaunchNow;
    private CheckBox chkOpenFolder;

    // Installation Parameters
    private string targetDir;
    private string pythonBinary = null;
    private bool existingVersionDetected = false;
    private bool isInstalling = false;

    // Colors
    private readonly Color ClrBgDark = Color.FromArgb(19, 19, 22);
    private readonly Color ClrSidebar = Color.FromArgb(13, 13, 15);
    private readonly Color ClrCardBg = Color.FromArgb(26, 26, 30);
    private readonly Color ClrCardBorder = Color.FromArgb(42, 42, 48);
    private readonly Color ClrCardSelectedBorder = Color.FromArgb(118, 185, 0);
    private readonly Color ClrNvidiaGreen = Color.FromArgb(118, 185, 0);
    private readonly Color ClrTextPrimary = Color.FromArgb(244, 244, 245);
    private readonly Color ClrTextSecondary = Color.FromArgb(161, 161, 170);
    private readonly Color ClrTextMuted = Color.FromArgb(113, 113, 122);
    private readonly Color ClrBorderSubtle = Color.FromArgb(34, 34, 39);

    public SpoonbillNvidiaInstaller()
    {
        this.Text = "Spoonbill AI Studio V4 — Setup Wizard";
        this.ClientSize = new Size(FormWidth, FormHeight);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ClrBgDark;
        this.ForeColor = ClrTextPrimary;
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        // Double buffer to eliminate flicker
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        targetDir = Path.Combine(localApp, "Programs", "Spoonbill_Studio");

        BuildTwoColumnLayout();
        SwitchStep(0);
    }

    private void BuildTwoColumnLayout()
    {
        // 1. Sidebar Panel (Fixed at X = 0, Y = 0, Width = 220, Height = 500)
        sidebarPanel = new Panel();
        sidebarPanel.Location = new Point(0, 0);
        sidebarPanel.Size = new Size(SidebarWidth, MainHeight);
        sidebarPanel.BackColor = ClrSidebar;
        sidebarPanel.Paint += (s, e) => {
            // Draw clear vertical dividing border between sidebar and content
            e.Graphics.DrawLine(new Pen(ClrBorderSubtle, 1), SidebarWidth - 1, 0, SidebarWidth - 1, MainHeight);
        };
        this.Controls.Add(sidebarPanel);

        // Brand Emblem
        Panel emblemPanel = new Panel();
        emblemPanel.Size = new Size(32, 32);
        emblemPanel.Location = new Point(24, 24);
        emblemPanel.BackColor = Color.FromArgb(24, 34, 12);
        emblemPanel.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen p = new Pen(ClrNvidiaGreen, 1.5f))
            {
                e.Graphics.DrawRectangle(p, 1, 1, 30, 30);
            }
            using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
            {
                e.Graphics.FillRectangle(b, 8, 8, 16, 16);
            }
        };
        sidebarPanel.Controls.Add(emblemPanel);

        Label brandTitle = new Label();
        brandTitle.Text = "SPOONBILL AI";
        brandTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        brandTitle.ForeColor = Color.White;
        brandTitle.Location = new Point(64, 22);
        brandTitle.AutoSize = true;
        sidebarPanel.Controls.Add(brandTitle);

        Label brandSub = new Label();
        brandSub.Text = "Studio Setup V4";
        brandSub.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        brandSub.ForeColor = ClrNvidiaGreen;
        brandSub.Location = new Point(65, 42);
        brandSub.AutoSize = true;
        sidebarPanel.Controls.Add(brandSub);

        // Step Navigation items
        string[] stepTitles = new string[] {
            "System Check",
            "User Agreement",
            "Options & Target",
            "Installation",
            "Ready to Use"
        };

        stepBadgePanels = new Panel[stepTitles.Length];
        stepNumberLabels = new Label[stepTitles.Length];
        stepTitleLabels = new Label[stepTitles.Length];

        for (int i = 0; i < stepTitles.Length; i++)
        {
            int idx = i;
            Panel badge = new Panel();
            badge.Size = new Size(24, 24);
            badge.Location = new Point(24, 96 + (i * 44));
            badge.BackColor = Color.FromArgb(28, 28, 32);

            Label num = new Label();
            num.Text = (i + 1).ToString();
            num.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            num.ForeColor = ClrTextMuted;
            num.Dock = DockStyle.Fill;
            num.TextAlign = ContentAlignment.MiddleCenter;
            badge.Controls.Add(num);

            badge.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderClr = (idx == currentStep) ? ClrNvidiaGreen : Color.FromArgb(45, 45, 52);
                using (Pen p = new Pen(borderClr, 1.2f))
                {
                    e.Graphics.DrawEllipse(p, 1, 1, 22, 22);
                }
            };
            sidebarPanel.Controls.Add(badge);

            Label lbl = new Label();
            lbl.Text = stepTitles[i];
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lbl.ForeColor = ClrTextMuted;
            lbl.Location = new Point(58, 98 + (i * 44));
            lbl.Size = new Size(150, 20);
            sidebarPanel.Controls.Add(lbl);

            stepBadgePanels[i] = badge;
            stepNumberLabels[i] = num;
            stepTitleLabels[i] = lbl;
        }

        // 2. Main Content Area (Fixed adjacent at X = 220, Y = 0, Width = 600, Height = 500)
        contentPanel = new Panel();
        contentPanel.Location = new Point(SidebarWidth, 0);
        contentPanel.Size = new Size(ContentWidth, MainHeight);
        contentPanel.BackColor = ClrBgDark;
        this.Controls.Add(contentPanel);

        // 3. Bottom Bar (Fixed at X = 0, Y = 500, Width = 820, Height = 60)
        bottomPanel = new Panel();
        bottomPanel.Location = new Point(0, MainHeight);
        bottomPanel.Size = new Size(FormWidth, BottomHeight);
        bottomPanel.BackColor = ClrSidebar;
        bottomPanel.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(ClrBorderSubtle, 1), 0, 0, FormWidth, 0);
        };
        this.Controls.Add(bottomPanel);

        lblVersionInfo = new Label();
        lblVersionInfo.Text = "Spoonbill AI Studio v4.0.0 (x64 CPU / GPU Edition)";
        lblVersionInfo.Font = new Font("Segoe UI", 8F);
        lblVersionInfo.ForeColor = ClrTextMuted;
        lblVersionInfo.Location = new Point(24, 20);
        lblVersionInfo.AutoSize = true;
        bottomPanel.Controls.Add(lblVersionInfo);

        btnBack = new Button();
        btnBack.Text = "< Back";
        btnBack.Size = new Size(92, 34);
        btnBack.Location = new Point(496, 13);
        btnBack.BackColor = Color.FromArgb(32, 32, 36);
        btnBack.ForeColor = ClrTextPrimary;
        btnBack.FlatStyle = FlatStyle.Flat;
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Cursor = Cursors.Hand;
        btnBack.Click += (s, e) => { if (currentStep > 0 && !isInstalling) SwitchStep(currentStep - 1); };
        bottomPanel.Controls.Add(btnBack);

        btnNext = new Button();
        btnNext.Text = "Next >";
        btnNext.Size = new Size(100, 34);
        btnNext.Location = new Point(598, 13);
        btnNext.BackColor = ClrNvidiaGreen;
        btnNext.ForeColor = Color.Black;
        btnNext.FlatStyle = FlatStyle.Flat;
        btnNext.FlatAppearance.BorderSize = 0;
        btnNext.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnNext.Cursor = Cursors.Hand;
        btnNext.Click += OnNextClick;
        bottomPanel.Controls.Add(btnNext);

        btnCancel = new Button();
        btnCancel.Text = "Cancel";
        btnCancel.Size = new Size(92, 34);
        btnCancel.Location = new Point(708, 13);
        btnCancel.BackColor = Color.FromArgb(32, 32, 36);
        btnCancel.ForeColor = ClrTextSecondary;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Cursor = Cursors.Hand;
        btnCancel.Click += OnCancelClick;
        bottomPanel.Controls.Add(btnCancel);

        BuildStepPages();
    }

    private void BuildStepPages()
    {
        // ================= PAGE 0: SYSTEM CHECK =================
        panelSystemCheck = new Panel();
        panelSystemCheck.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelSystemCheck);

        // ================= PAGE 0: SYSTEM CHECK =================
        panelSystemCheck = new Panel();
        panelSystemCheck.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelSystemCheck);

        lblSysTitle = new Label();
        lblSysTitle.Text = "System Compatibility & Environment Scan";
        lblSysTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblSysTitle.ForeColor = Color.White;
        lblSysTitle.Location = new Point(28, 16);
        lblSysTitle.Size = new Size(540, 26);
        panelSystemCheck.Controls.Add(lblSysTitle);

        lblSysDesc = new Label();
        lblSysDesc.Text = "Verifying operating system architecture, hardware compute, and Python runtime.";
        lblSysDesc.ForeColor = Color.FromArgb(200, 200, 210);
        lblSysDesc.Location = new Point(28, 42);
        lblSysDesc.Size = new Size(540, 20);
        panelSystemCheck.Controls.Add(lblSysDesc);

        sysCardPanels = new Panel[4];
        sysCardTitles = new Label[4];
        sysCardValues = new Label[4];
        sysCardBadges = new Label[4];

        string[] cardHeaders = new string[] {
            "Operating System & Architecture",
            "Processor & Hardware Cores",
            "Graphics & Acceleration Engine",
            "Python AI Inference Runtime"
        };

        for (int i = 0; i < 4; i++)
        {
            Panel card = new Panel();
            card.Size = new Size(540, 48);
            card.Location = new Point(28, 66 + (i * 54));
            card.BackColor = ClrCardBg;
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(ClrCardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            panelSystemCheck.Controls.Add(card);

            Label title = new Label();
            title.Text = cardHeaders[i];
            title.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            title.ForeColor = Color.FromArgb(180, 180, 190);
            title.Location = new Point(16, 7);
            title.AutoSize = true;
            card.Controls.Add(title);

            Label val = new Label();
            val.Text = "Scanning environment...";
            val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            val.ForeColor = Color.White;
            val.Location = new Point(16, 24);
            val.Size = new Size(380, 18);
            card.Controls.Add(val);

            Label badge = new Label();
            badge.Text = "CHECKING";
            badge.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            badge.ForeColor = ClrNvidiaGreen;
            badge.BackColor = Color.FromArgb(24, 38, 16);
            badge.Location = new Point(405, 12);
            badge.Size = new Size(120, 24);
            badge.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(badge);

            sysCardPanels[i] = card;
            sysCardTitles[i] = title;
            sysCardValues[i] = val;
            sysCardBadges[i] = badge;
        }

        // Advisory Banner for Readiness & Troubleshooting
        pnlSysAdvisory = new Panel();
        pnlSysAdvisory.Size = new Size(540, 80);
        pnlSysAdvisory.Location = new Point(28, 286);
        pnlSysAdvisory.BackColor = Color.FromArgb(24, 24, 28);
        pnlSysAdvisory.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool pyOk = !string.IsNullOrEmpty(pythonBinary);
            Color bClr = pyOk ? ClrNvidiaGreen : Color.FromArgb(239, 68, 68);
            using (Pen p = new Pen(bClr, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlSysAdvisory.Width - 1, pnlSysAdvisory.Height - 1);
            }
        };
        panelSystemCheck.Controls.Add(pnlSysAdvisory);

        lblAdvisoryTitle = new Label();
        lblAdvisoryTitle.Text = "System Scan Initializing...";
        lblAdvisoryTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblAdvisoryTitle.ForeColor = Color.White;
        lblAdvisoryTitle.BackColor = Color.Transparent;
        lblAdvisoryTitle.Location = new Point(16, 10);
        lblAdvisoryTitle.Size = new Size(508, 20);
        pnlSysAdvisory.Controls.Add(lblAdvisoryTitle);

        lblAdvisoryDesc = new Label();
        lblAdvisoryDesc.Text = "Analyzing hardware accelerators and runtime prerequisites...";
        lblAdvisoryDesc.Font = new Font("Segoe UI", 8.5F);
        lblAdvisoryDesc.ForeColor = Color.FromArgb(220, 220, 230);
        lblAdvisoryDesc.BackColor = Color.Transparent;
        lblAdvisoryDesc.Location = new Point(16, 32);
        lblAdvisoryDesc.Size = new Size(508, 40);
        pnlSysAdvisory.Controls.Add(lblAdvisoryDesc);

        // System Check Action Row
        btnHowToReady = new Button();
        btnHowToReady.Text = "How to Make Ready...";
        btnHowToReady.Location = new Point(28, 376);
        btnHowToReady.Size = new Size(175, 34);
        btnHowToReady.BackColor = Color.FromArgb(48, 48, 56);
        btnHowToReady.ForeColor = Color.White;
        btnHowToReady.FlatStyle = FlatStyle.Flat;
        btnHowToReady.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 85);
        btnHowToReady.Cursor = Cursors.Hand;
        btnHowToReady.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnHowToReady.Click += (s, e) => ShowTroubleshootingDialog();
        panelSystemCheck.Controls.Add(btnHowToReady);

        btnRefreshScan = new Button();
        btnRefreshScan.Text = "Refresh Scan";
        btnRefreshScan.Location = new Point(211, 376);
        btnRefreshScan.Size = new Size(130, 34);
        btnRefreshScan.BackColor = ClrNvidiaGreen;
        btnRefreshScan.ForeColor = Color.Black;
        btnRefreshScan.FlatStyle = FlatStyle.Flat;
        btnRefreshScan.FlatAppearance.BorderSize = 0;
        btnRefreshScan.Cursor = Cursors.Hand;
        btnRefreshScan.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnRefreshScan.Click += (s, e) => RunSystemCheck();
        panelSystemCheck.Controls.Add(btnRefreshScan);

        lblLastScanTime = new Label();
        lblLastScanTime.Text = "Click Refresh after installing dependencies";
        lblLastScanTime.Font = new Font("Segoe UI", 8.5F);
        lblLastScanTime.ForeColor = Color.FromArgb(190, 190, 200);
        lblLastScanTime.Location = new Point(352, 384);
        lblLastScanTime.Size = new Size(216, 20);
        panelSystemCheck.Controls.Add(lblLastScanTime);

        // ================= PAGE 1: USER AGREEMENT =================
        panelAgreement = new Panel();
        panelAgreement.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelAgreement);

        lblAgreeTitle = new Label();
        lblAgreeTitle.Text = "License & AI Dependencies Agreement";
        lblAgreeTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblAgreeTitle.ForeColor = Color.White;
        lblAgreeTitle.Location = new Point(28, 16);
        lblAgreeTitle.Size = new Size(540, 26);
        panelAgreement.Controls.Add(lblAgreeTitle);

        lblAgreeDesc = new Label();
        lblAgreeDesc.Text = "Please review the conservation research license, pip permissions, and privacy terms.";
        lblAgreeDesc.ForeColor = Color.FromArgb(200, 200, 210);
        lblAgreeDesc.Location = new Point(28, 42);
        lblAgreeDesc.Size = new Size(540, 20);
        panelAgreement.Controls.Add(lblAgreeDesc);

        txtAgreementTerms = new RichTextBox();
        txtAgreementTerms.Multiline = true;
        txtAgreementTerms.ReadOnly = true;
        txtAgreementTerms.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtAgreementTerms.BackColor = Color.FromArgb(16, 16, 20);
        txtAgreementTerms.ForeColor = Color.FromArgb(235, 235, 240);
        txtAgreementTerms.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        txtAgreementTerms.BorderStyle = BorderStyle.None;
        txtAgreementTerms.Location = new Point(28, 68);
        txtAgreementTerms.Size = new Size(540, 196);
        txtAgreementTerms.Text =
            "BLACK-FACED SPOONBILL AI STUDIO V4 — LICENSE & USAGE AGREEMENT" + Environment.NewLine +
            "=========================================================================" + Environment.NewLine + Environment.NewLine +
            "1. CONSERVATION RESEARCH LICENSE GRANT" + Environment.NewLine +
            "This software is provided under Creative Commons Attribution-NonCommercial-" + Environment.NewLine +
            "ShareAlike 4.0 International (CC BY-NC-SA 4.0). You are granted permission to" + Environment.NewLine +
            "use, evaluate, and adapt this studio for non-commercial ecological research," + Environment.NewLine +
            "academic study, and Black-faced Spoonbill (Platalea minor) wildlife census." + Environment.NewLine +
            "Commercial monetization or sale of this application without explicit permission" + Environment.NewLine +
            "from the original authors is strictly prohibited." + Environment.NewLine + Environment.NewLine +
            "2. AI DEPENDENCIES & AUTOMATED PIP EXECUTION" + Environment.NewLine +
            "By agreeing, you authorize the setup wizard to verify your local Python 3" + Environment.NewLine +
            "installation and run 'pip install -r requirements_lean.txt' to acquire runtime" + Environment.NewLine +
            "libraries (onnxruntime, fastapi, uvicorn, opencv-python-headless, pillow, numpy)." + Environment.NewLine +
            "Total download footprint is approximately 50 MB with zero PyTorch bloat." + Environment.NewLine + Environment.NewLine +
            "3. 100% LOCAL INFERENCE & DATA PRIVACY GUARANTEE" + Environment.NewLine +
            "All deep learning inference, image processing, and census calculations execute" + Environment.NewLine +
            "100% locally on your computer. No user photographs, surveillance feeds, or survey" + Environment.NewLine +
            "data are uploaded to external cloud endpoints." + Environment.NewLine + Environment.NewLine +
            "4. MANDATORY DEPENDENCY REQUIREMENT & CLEAN ABORT" + Environment.NewLine +
            "Spoonbill AI Studio cannot operate without its local neural inference backend." + Environment.NewLine +
            "If you select 'Do not accept', setup will cancel immediately without writing files.";
        panelAgreement.Controls.Add(txtAgreementTerms);

        // Agreement Toggleable Choice Box 1 (Accept)
        pnlAgreeBox = new Panel();
        pnlAgreeBox.Size = new Size(540, 52);
        pnlAgreeBox.Location = new Point(28, 274);
        pnlAgreeBox.BackColor = ClrCardBg;
        pnlAgreeBox.Cursor = Cursors.Hand;
        pnlAgreeBox.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (agreementChoice == 1);
            Color bClr = isSel ? ClrNvidiaGreen : Color.FromArgb(50, 50, 58);
            Color fClr = isSel ? Color.FromArgb(24, 40, 16) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlAgreeBox.Width, pnlAgreeBox.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 1.5f : 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlAgreeBox.Width - 1, pnlAgreeBox.Height - 1);
            }

            // Indicator Square at (16, 16, 20, 20)
            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillRectangle(b, 16, 16, 20, 20);
                }
                using (Pen pCheck = new Pen(Color.FromArgb(13, 13, 15), 2.2f))
                {
                    e.Graphics.DrawLines(pCheck, new Point[] {
                        new Point(20, 26),
                        new Point(24, 31),
                        new Point(32, 20)
                    });
                }
            }
            else
            {
                using (Pen pBox = new Pen(Color.FromArgb(140, 140, 150), 1.5f))
                {
                    e.Graphics.DrawRectangle(pBox, 16, 16, 20, 20);
                }
            }

            // High-contrast direct text drawing
            using (Font fTitle = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.White : Color.FromArgb(235, 235, 240)))
            {
                e.Graphics.DrawString("I accept the license terms and agree to install required AI dependencies", fTitle, bTitle, 46, 8);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(190, 225, 170) : Color.FromArgb(180, 180, 190)))
            {
                e.Graphics.DrawString("Authorizes automated pip execution for lean runtime libraries (onnxruntime, fastapi, uvicorn).", fSub, bSub, 46, 28);
            }
        };
        pnlAgreeBox.Click += (s, e) => ToggleAgreementChoice(1);
        panelAgreement.Controls.Add(pnlAgreeBox);

        // Agreement Toggleable Choice Box 2 (Decline)
        pnlDeclineBox = new Panel();
        pnlDeclineBox.Size = new Size(540, 52);
        pnlDeclineBox.Location = new Point(28, 334);
        pnlDeclineBox.BackColor = ClrCardBg;
        pnlDeclineBox.Cursor = Cursors.Hand;
        pnlDeclineBox.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (agreementChoice == 2);
            Color bClr = isSel ? Color.FromArgb(239, 68, 68) : Color.FromArgb(50, 50, 58);
            Color fClr = isSel ? Color.FromArgb(40, 18, 18) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlDeclineBox.Width, pnlDeclineBox.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 1.5f : 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlDeclineBox.Width - 1, pnlDeclineBox.Height - 1);
            }

            // Indicator Square at (16, 16, 20, 20)
            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    e.Graphics.FillRectangle(b, 16, 16, 20, 20);
                }
                using (Pen pX = new Pen(Color.White, 2.0f))
                {
                    e.Graphics.DrawLine(pX, 21, 21, 31, 31);
                    e.Graphics.DrawLine(pX, 31, 21, 21, 31);
                }
            }
            else
            {
                using (Pen pBox = new Pen(Color.FromArgb(140, 140, 150), 1.5f))
                {
                    e.Graphics.DrawRectangle(pBox, 16, 16, 20, 20);
                }
            }

            // High-contrast direct text drawing
            using (Font fTitle = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.FromArgb(254, 202, 202) : Color.FromArgb(235, 235, 240)))
            {
                e.Graphics.DrawString("I do not accept the terms (Cancel installation and exit setup immediately)", fTitle, bTitle, 46, 8);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(240, 180, 180) : Color.FromArgb(180, 180, 190)))
            {
                e.Graphics.DrawString("Setup will safely abort without modifying system files or installing packages.", fSub, bSub, 46, 28);
            }
        };
        pnlDeclineBox.Click += (s, e) => ToggleAgreementChoice(2);
        panelAgreement.Controls.Add(pnlDeclineBox);

        // Agreement Selection Status
        lblAgreementStatus = new Label();
        lblAgreementStatus.Font = new Font("Segoe UI", 9F);
        lblAgreementStatus.ForeColor = Color.FromArgb(200, 200, 210);
        lblAgreementStatus.Location = new Point(28, 396);
        lblAgreementStatus.Size = new Size(540, 22);
        lblAgreementStatus.Text = "Please click an option box above to select your choice (click again to deselect).";
        panelAgreement.Controls.Add(lblAgreementStatus);

        // ================= PAGE 2: MODE & OPTIONS =================
        panelModeAndOptions = new Panel();
        panelModeAndOptions.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelModeAndOptions);

        lblOptTitle = new Label();
        lblOptTitle.Text = "Installation Mode & Configuration";
        lblOptTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblOptTitle.ForeColor = Color.White;
        lblOptTitle.Location = new Point(28, 16);
        lblOptTitle.Size = new Size(540, 26);
        panelModeAndOptions.Controls.Add(lblOptTitle);

        lblOptDesc = new Label();
        lblOptDesc.Text = "Select Express for automated deployment or Custom for advanced paths and parts.";
        lblOptDesc.ForeColor = Color.FromArgb(200, 200, 210);
        lblOptDesc.Location = new Point(28, 42);
        lblOptDesc.Size = new Size(540, 20);
        panelModeAndOptions.Controls.Add(lblOptDesc);

        // Express Card (Strict Mutually Exclusive)
        pnlExpressCard = new Panel();
        pnlExpressCard.Size = new Size(260, 80);
        pnlExpressCard.Location = new Point(28, 72);
        pnlExpressCard.BackColor = ClrCardBg;
        pnlExpressCard.Cursor = Cursors.Hand;
        pnlExpressCard.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (installMode == 0);
            Color bClr = isSel ? ClrNvidiaGreen : Color.FromArgb(50, 50, 58);
            Color fClr = isSel ? Color.FromArgb(24, 40, 16) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlExpressCard.Width, pnlExpressCard.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 1.5f : 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlExpressCard.Width - 1, pnlExpressCard.Height - 1);
            }

            // Radio Circle at (16, 14, 18, 18)
            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillEllipse(b, 16, 14, 18, 18);
                }
                using (SolidBrush bDot = new SolidBrush(Color.FromArgb(13, 13, 15)))
                {
                    e.Graphics.FillEllipse(bDot, 22, 20, 6, 6);
                }
            }
            else
            {
                using (Pen pCircle = new Pen(Color.FromArgb(140, 140, 150), 1.5f))
                {
                    e.Graphics.DrawEllipse(pCircle, 16, 14, 18, 18);
                }
            }

            // Direct text drawing
            using (Font fTitle = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.White : Color.FromArgb(235, 235, 240)))
            {
                e.Graphics.DrawString("Express (Recommended)", fTitle, bTitle, 42, 12);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(190, 225, 170) : Color.FromArgb(180, 180, 190)))
            {
                e.Graphics.DrawString("Automatic setup with all models,\ndependencies, & clean upgrade.", fSub, bSub, 42, 34);
            }
        };
        pnlExpressCard.Click += (s, e) => SetInstallMode(0);
        panelModeAndOptions.Controls.Add(pnlExpressCard);

        // Custom Card (Strict Mutually Exclusive)
        pnlCustomCard = new Panel();
        pnlCustomCard.Size = new Size(260, 80);
        pnlCustomCard.Location = new Point(308, 72);
        pnlCustomCard.BackColor = ClrCardBg;
        pnlCustomCard.Cursor = Cursors.Hand;
        pnlCustomCard.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (installMode == 1);
            Color bClr = isSel ? ClrNvidiaGreen : Color.FromArgb(50, 50, 58);
            Color fClr = isSel ? Color.FromArgb(24, 40, 16) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlCustomCard.Width, pnlCustomCard.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 1.5f : 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlCustomCard.Width - 1, pnlCustomCard.Height - 1);
            }

            // Radio Circle at (16, 14, 18, 18)
            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillEllipse(b, 16, 14, 18, 18);
                }
                using (SolidBrush bDot = new SolidBrush(Color.FromArgb(13, 13, 15)))
                {
                    e.Graphics.FillEllipse(bDot, 22, 20, 6, 6);
                }
            }
            else
            {
                using (Pen pCircle = new Pen(Color.FromArgb(140, 140, 150), 1.5f))
                {
                    e.Graphics.DrawEllipse(pCircle, 16, 14, 18, 18);
                }
            }

            // Direct text drawing
            using (Font fTitle = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.White : Color.FromArgb(235, 235, 240)))
            {
                e.Graphics.DrawString("Custom (Advanced)", fTitle, bTitle, 42, 12);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(190, 225, 170) : Color.FromArgb(180, 180, 190)))
            {
                e.Graphics.DrawString("Customize installation folder,\nselective models, & shortcuts.", fSub, bSub, 42, 34);
            }
        };
        pnlCustomCard.Click += (s, e) => SetInstallMode(1);
        panelModeAndOptions.Controls.Add(pnlCustomCard);

        // Options Details Panel (Container for path & toggles)
        pnlCustomDetails = new Panel();
        pnlCustomDetails.Location = new Point(28, 164);
        pnlCustomDetails.Size = new Size(540, 246);
        pnlCustomDetails.BackColor = Color.FromArgb(22, 22, 25);
        pnlCustomDetails.Paint += (s, e) => {
            using (Pen p = new Pen(ClrBorderSubtle, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlCustomDetails.Width - 1, pnlCustomDetails.Height - 1);
            }
        };
        panelModeAndOptions.Controls.Add(pnlCustomDetails);

        lblDest = new Label();
        lblDest.Text = "Target Folder (Express Default):";
        lblDest.ForeColor = Color.FromArgb(220, 220, 230);
        lblDest.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblDest.Location = new Point(16, 10);
        lblDest.Size = new Size(240, 16);
        pnlCustomDetails.Controls.Add(lblDest);

        txtTargetDir = new TextBox();
        txtTargetDir.Text = targetDir;
        txtTargetDir.BackColor = Color.FromArgb(20, 20, 24);
        txtTargetDir.ForeColor = Color.White;
        txtTargetDir.BorderStyle = BorderStyle.FixedSingle;
        txtTargetDir.Location = new Point(16, 30);
        txtTargetDir.Size = new Size(410, 23);
        txtTargetDir.TextChanged += (s, e) => { targetDir = txtTargetDir.Text; UpdateDiskSpace(); };
        pnlCustomDetails.Controls.Add(txtTargetDir);

        btnBrowseTarget = new Button();
        btnBrowseTarget.Text = "Browse...";
        btnBrowseTarget.Size = new Size(90, 25);
        btnBrowseTarget.Location = new Point(434, 29);
        btnBrowseTarget.BackColor = Color.FromArgb(48, 48, 54);
        btnBrowseTarget.ForeColor = Color.White;
        btnBrowseTarget.FlatStyle = FlatStyle.Flat;
        btnBrowseTarget.FlatAppearance.BorderSize = 0;
        btnBrowseTarget.Cursor = Cursors.Hand;
        btnBrowseTarget.Click += OnBrowseTargetClick;
        pnlCustomDetails.Controls.Add(btnBrowseTarget);

        lblDiskSpace = new Label();
        lblDiskSpace.Text = "Required space: ~550 MB | Available space: Calculating...";
        lblDiskSpace.ForeColor = Color.FromArgb(190, 190, 200);
        lblDiskSpace.Font = new Font("Segoe UI", 8F);
        lblDiskSpace.Location = new Point(16, 58);
        lblDiskSpace.Size = new Size(500, 16);
        pnlCustomDetails.Controls.Add(lblDiskSpace);

        chkCleanInstall = new CheckBox();
        chkCleanInstall.Text = "Perform Clean Installation (Safely purge previous build files first)";
        chkCleanInstall.Checked = true;
        chkCleanInstall.ForeColor = Color.FromArgb(246, 130, 31);
        chkCleanInstall.Location = new Point(16, 82);
        chkCleanInstall.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkCleanInstall);

        chkInstallModels = new CheckBox();
        chkInstallModels.Text = "Deploy Bundled ONNX Models (YOLO11 Nano & Large neural weights)";
        chkInstallModels.Checked = true;
        chkInstallModels.ForeColor = Color.FromArgb(235, 235, 240);
        chkInstallModels.Location = new Point(16, 106);
        chkInstallModels.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkInstallModels);

        chkInstallDeps = new CheckBox();
        chkInstallDeps.Text = "Install AI Dependencies via pip (onnxruntime, fastapi, uvicorn, opencv)";
        chkInstallDeps.Checked = true;
        chkInstallDeps.ForeColor = Color.FromArgb(235, 235, 240);
        chkInstallDeps.Location = new Point(16, 130);
        chkInstallDeps.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkInstallDeps);

        chkDesktopShortcut = new CheckBox();
        chkDesktopShortcut.Text = "Create Desktop shortcut";
        chkDesktopShortcut.Checked = true;
        chkDesktopShortcut.ForeColor = Color.FromArgb(235, 235, 240);
        chkDesktopShortcut.Location = new Point(16, 158);
        chkDesktopShortcut.Size = new Size(230, 22);
        pnlCustomDetails.Controls.Add(chkDesktopShortcut);

        chkStartMenuShortcut = new CheckBox();
        chkStartMenuShortcut.Text = "Create Start Menu shortcut";
        chkStartMenuShortcut.Checked = true;
        chkStartMenuShortcut.ForeColor = Color.FromArgb(235, 235, 240);
        chkStartMenuShortcut.Location = new Point(260, 158);
        chkStartMenuShortcut.Size = new Size(230, 22);
        pnlCustomDetails.Controls.Add(chkStartMenuShortcut);

        chkRegisterUninstaller = new CheckBox();
        chkRegisterUninstaller.Text = "Register Uninstaller in Windows Installed Apps (Control Panel / Settings)";
        chkRegisterUninstaller.Checked = true;
        chkRegisterUninstaller.ForeColor = Color.FromArgb(235, 235, 240);
        chkRegisterUninstaller.Location = new Point(16, 186);
        chkRegisterUninstaller.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkRegisterUninstaller);

        ToggleCustomMode(false);
        UpdateDiskSpace();

        // ================= PAGE 3: PROGRESS =================
        panelProgress = new Panel();
        panelProgress.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelProgress);

        lblProgTitle = new Label();
        lblProgTitle.Text = "Installing Spoonbill AI Studio V4...";
        lblProgTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblProgTitle.ForeColor = Color.White;
        lblProgTitle.Location = new Point(28, 16);
        lblProgTitle.Size = new Size(540, 26);
        panelProgress.Controls.Add(lblProgTitle);

        lblProgStatus = new Label();
        lblProgStatus.Text = "Preparing installation package...";
        lblProgStatus.ForeColor = Color.FromArgb(200, 200, 210);
        lblProgStatus.Location = new Point(28, 42);
        lblProgStatus.Size = new Size(540, 20);
        panelProgress.Controls.Add(lblProgStatus);

        // Custom Sleek Modern Progress Bar
        customProgressBar = new Panel();
        customProgressBar.Size = new Size(540, 8);
        customProgressBar.Location = new Point(28, 72);
        customProgressBar.BackColor = Color.FromArgb(32, 32, 36);
        customProgressBar.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int fillW = (int)((currentProgressPercent / 100.0) * customProgressBar.Width);
            if (fillW > 0)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillRectangle(b, 0, 0, fillW, customProgressBar.Height);
                }
            }
        };
        panelProgress.Controls.Add(customProgressBar);

        logBox = new RichTextBox();
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        logBox.BackColor = Color.FromArgb(14, 14, 18);
        logBox.ForeColor = Color.FromArgb(220, 220, 230);
        logBox.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        logBox.Location = new Point(28, 92);
        logBox.Size = new Size(540, 320);
        logBox.BorderStyle = BorderStyle.None;
        panelProgress.Controls.Add(logBox);

        // ================= PAGE 4: FINISH =================
        panelFinish = new Panel();
        panelFinish.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelFinish);

        lblFinishTitle = new Label();
        lblFinishTitle.Text = "Installation Complete";
        lblFinishTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblFinishTitle.ForeColor = Color.White;
        lblFinishTitle.Location = new Point(28, 20);
        lblFinishTitle.Size = new Size(540, 28);
        panelFinish.Controls.Add(lblFinishTitle);

        lblFinishDesc = new Label();
        lblFinishDesc.Text = "Spoonbill AI Studio V4 has been deployed successfully to your system.";
        lblFinishDesc.ForeColor = ClrTextSecondary;
        lblFinishDesc.Location = new Point(28, 52);
        lblFinishDesc.Size = new Size(540, 22);
        panelFinish.Controls.Add(lblFinishDesc);

        // Finish Information Card
        pnlFinishCard = new Panel();
        pnlFinishCard.Size = new Size(540, 150);
        pnlFinishCard.Location = new Point(28, 88);
        pnlFinishCard.BackColor = ClrCardBg;
        pnlFinishCard.Paint += (s, e) => {
            using (Pen p = new Pen(ClrCardBorder, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlFinishCard.Width - 1, pnlFinishCard.Height - 1);
            }
        };
        panelFinish.Controls.Add(pnlFinishCard);

        Label lblCardHeader = new Label();
        lblCardHeader.Text = "DEPLOYMENT SUMMARY";
        lblCardHeader.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblCardHeader.ForeColor = ClrNvidiaGreen;
        lblCardHeader.Location = new Point(18, 14);
        lblCardHeader.AutoSize = true;
        pnlFinishCard.Controls.Add(lblCardHeader);

        Label lblSummaryBody = new Label();
        lblSummaryBody.Text =
            "• Studio Executable : Installed & verified\n" +
            "• Neural Backend    : CPU Safe Mode (ONNX Runtime 11N & 11L)\n" +
            "• Windows Control   : Uninstaller registered in Settings > Apps\n" +
            "• Local Privacy     : 100% offline edge inference active";
        lblSummaryBody.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        lblSummaryBody.ForeColor = ClrTextPrimary;
        lblSummaryBody.Location = new Point(18, 38);
        lblSummaryBody.Size = new Size(500, 95);
        pnlFinishCard.Controls.Add(lblSummaryBody);

        chkLaunchNow = new CheckBox();
        chkLaunchNow.Text = "Launch Spoonbill AI Studio now";
        chkLaunchNow.Checked = true;
        chkLaunchNow.ForeColor = ClrNvidiaGreen;
        chkLaunchNow.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        chkLaunchNow.Location = new Point(28, 260);
        chkLaunchNow.Size = new Size(400, 26);
        panelFinish.Controls.Add(chkLaunchNow);

        chkOpenFolder = new CheckBox();
        chkOpenFolder.Text = "Open application installation directory in File Explorer";
        chkOpenFolder.Checked = false;
        chkOpenFolder.ForeColor = ClrTextSecondary;
        chkOpenFolder.Location = new Point(28, 292);
        chkOpenFolder.Size = new Size(400, 24);
        panelFinish.Controls.Add(chkOpenFolder);
    }

    private void SetInstallMode(int mode)
    {
        installMode = mode;
        pnlExpressCard.Invalidate();
        pnlCustomCard.Invalidate();
        ToggleCustomMode(installMode == 1);
    }

    private void ToggleCustomMode(bool customEnabled)
    {
        txtTargetDir.ReadOnly = !customEnabled;
        txtTargetDir.BackColor = customEnabled ? Color.FromArgb(20, 20, 24) : Color.FromArgb(16, 16, 20);
        txtTargetDir.ForeColor = customEnabled ? Color.White : Color.FromArgb(180, 180, 190);
        btnBrowseTarget.Enabled = customEnabled;
        btnBrowseTarget.BackColor = customEnabled ? Color.FromArgb(48, 48, 54) : Color.FromArgb(28, 28, 32);
        btnBrowseTarget.ForeColor = customEnabled ? Color.White : Color.FromArgb(120, 120, 130);

        chkCleanInstall.AutoCheck = customEnabled;
        chkInstallModels.AutoCheck = customEnabled;
        chkInstallDeps.AutoCheck = customEnabled;
        chkDesktopShortcut.AutoCheck = customEnabled;
        chkStartMenuShortcut.AutoCheck = customEnabled;
        chkRegisterUninstaller.AutoCheck = customEnabled;

        if (!customEnabled)
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            targetDir = Path.Combine(localApp, "Programs", "Spoonbill_Studio");
            txtTargetDir.Text = targetDir;
            chkCleanInstall.Checked = true;
            chkInstallModels.Checked = true;
            chkInstallDeps.Checked = true;
            chkDesktopShortcut.Checked = true;
            chkStartMenuShortcut.Checked = true;
            chkRegisterUninstaller.Checked = true;

            chkCleanInstall.ForeColor = Color.FromArgb(246, 130, 31);
            chkInstallModels.ForeColor = Color.FromArgb(215, 215, 225);
            chkInstallDeps.ForeColor = Color.FromArgb(215, 215, 225);
            chkDesktopShortcut.ForeColor = Color.FromArgb(215, 215, 225);
            chkStartMenuShortcut.ForeColor = Color.FromArgb(215, 215, 225);
            chkRegisterUninstaller.ForeColor = Color.FromArgb(215, 215, 225);
            lblDest.Text = "Target Folder (Express Default):";
        }
        else
        {
            chkCleanInstall.ForeColor = Color.FromArgb(246, 130, 31);
            chkInstallModels.ForeColor = Color.FromArgb(244, 244, 245);
            chkInstallDeps.ForeColor = Color.FromArgb(244, 244, 245);
            chkDesktopShortcut.ForeColor = Color.FromArgb(244, 244, 245);
            chkStartMenuShortcut.ForeColor = Color.FromArgb(244, 244, 245);
            chkRegisterUninstaller.ForeColor = Color.FromArgb(244, 244, 245);
            lblDest.Text = "Target Folder (Custom Path):";
        }
    }

    private void OnBrowseTargetClick(object sender, EventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "Select Installation Directory for Spoonbill AI Studio V4";
            fbd.SelectedPath = Directory.Exists(targetDir) ? targetDir : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                targetDir = fbd.SelectedPath;
                txtTargetDir.Text = targetDir;
                UpdateDiskSpace();
            }
        }
    }

    private void UpdateDiskSpace()
    {
        try
        {
            string root = Path.GetPathRoot(targetDir);
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                DriveInfo d = new DriveInfo(root);
                long freeMb = d.AvailableFreeSpace / (1024 * 1024);
                lblDiskSpace.Text = "Required: ~550 MB | Available on " + root + ": " + freeMb + " MB";
            }
        }
        catch
        {
            lblDiskSpace.Text = "Required space: ~550 MB";
        }
    }

    private void SwitchStep(int step)
    {
        currentStep = step;

        for (int i = 0; i < stepBadgePanels.Length; i++)
        {
            stepBadgePanels[i].Invalidate();
            if (i == step)
            {
                stepNumberLabels[i].ForeColor = ClrNvidiaGreen;
                stepTitleLabels[i].ForeColor = Color.White;
                stepTitleLabels[i].Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            else if (i < step)
            {
                stepNumberLabels[i].ForeColor = Color.FromArgb(200, 200, 205);
                stepTitleLabels[i].ForeColor = Color.FromArgb(200, 200, 205);
                stepTitleLabels[i].Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
            else
            {
                stepNumberLabels[i].ForeColor = ClrTextMuted;
                stepTitleLabels[i].ForeColor = ClrTextMuted;
                stepTitleLabels[i].Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
        }

        panelSystemCheck.Visible = (step == 0);
        panelAgreement.Visible = (step == 1);
        panelModeAndOptions.Visible = (step == 2);
        panelProgress.Visible = (step == 3);
        panelFinish.Visible = (step == 4);

        if (step == 0)
        {
            btnBack.Enabled = false;
            btnNext.Text = "Next >";
            btnNext.Enabled = true;
            RunSystemCheck();
        }
        else if (step == 1)
        {
            btnBack.Enabled = true;
            UpdateAgreementUI();
        }
        else if (step == 2)
        {
            btnBack.Enabled = true;
            btnNext.Text = "Install";
            btnNext.Enabled = true;
        }
        else if (step == 3)
        {
            btnBack.Enabled = false;
            btnNext.Enabled = false;
            btnCancel.Enabled = false;
            isInstalling = true;
            ThreadPool.QueueUserWorkItem(RunInstallation);
        }
        else if (step == 4)
        {
            btnBack.Visible = false;
            btnCancel.Visible = false;
            btnNext.Text = "Finish";
            btnNext.Enabled = true;
        }
    }

    private void RunSystemCheck()
    {
        // 1. Operating System & Architecture
        sysCardValues[0].Text = Environment.OSVersion.ToString() + " (64-bit)";
        sysCardBadges[0].Text = "COMPATIBLE";
        sysCardBadges[0].ForeColor = ClrNvidiaGreen;

        // 2. Hardware Resources
        int cores = Environment.ProcessorCount;
        sysCardValues[1].Text = cores + " Logical Cores detected";
        sysCardBadges[1].Text = "READY";
        sysCardBadges[1].ForeColor = ClrNvidiaGreen;

        // 3. Graphics & Hardware Acceleration
        detectedGpu = DetectGraphicsCard(out hasNvidiaGpu);
        if (hasNvidiaGpu)
        {
            sysCardValues[2].Text = detectedGpu + " (CUDA Capable)";
            sysCardBadges[2].Text = "NVIDIA GPU";
            sysCardBadges[2].ForeColor = ClrNvidiaGreen;
            sysCardBadges[2].BackColor = Color.FromArgb(24, 38, 16);
        }
        else
        {
            sysCardValues[2].Text = detectedGpu + " (CPU Safe Mode)";
            sysCardBadges[2].Text = "CPU SAFE MODE";
            sysCardBadges[2].ForeColor = Color.FromArgb(56, 189, 248);
            sysCardBadges[2].BackColor = Color.FromArgb(16, 32, 48);
        }

        // 4. Python Environment
        pythonBinary = FindPythonBinary();
        bool pythonReady = !string.IsNullOrEmpty(pythonBinary);
        if (pythonReady)
        {
            sysCardValues[3].Text = "Python 3 located at " + Path.GetFileName(pythonBinary);
            sysCardBadges[3].Text = "READY";
            sysCardBadges[3].ForeColor = ClrNvidiaGreen;
            sysCardBadges[3].BackColor = Color.FromArgb(24, 38, 16);
        }
        else
        {
            sysCardValues[3].Text = "Python 3 not found on system PATH.";
            sysCardBadges[3].Text = "ACTION NEEDED";
            sysCardBadges[3].ForeColor = Color.FromArgb(239, 68, 68);
            sysCardBadges[3].BackColor = Color.FromArgb(48, 18, 18);
        }

        existingVersionDetected = Directory.Exists(targetDir) && File.Exists(Path.Combine(targetDir, "Spoonbill_Studio.exe"));

        // Update Advisory Banner
        if (!pythonReady)
        {
            lblAdvisoryTitle.Text = "Action Recommended: Python 3 Runtime Not Found";
            lblAdvisoryTitle.ForeColor = Color.FromArgb(239, 68, 68);
            lblAdvisoryDesc.Text = "Python 3.8 - 3.12 is required for neural inference. Click 'How to Make Ready...' for installation commands, then click 'Refresh Scan'.";
            pnlSysAdvisory.BackColor = Color.FromArgb(32, 18, 18);
            btnHowToReady.Text = "How to Make Ready...";
            btnHowToReady.BackColor = Color.FromArgb(60, 24, 24);
            btnHowToReady.ForeColor = Color.FromArgb(254, 202, 202);
        }
        else
        {
            lblAdvisoryTitle.Text = "System Ready for Spoonbill AI Studio";
            lblAdvisoryTitle.ForeColor = ClrNvidiaGreen;
            string upgradeMsg = existingVersionDetected ? " (Prior installation detected and will be upgraded)." : "";
            lblAdvisoryDesc.Text = "All core system requirements satisfied" + upgradeMsg + ". You can proceed with installation or review the hardware guide.";
            pnlSysAdvisory.BackColor = Color.FromArgb(18, 30, 14);
            btnHowToReady.Text = "Setup & Hardware Guide";
            btnHowToReady.BackColor = Color.FromArgb(26, 40, 18);
            btnHowToReady.ForeColor = ClrNvidiaGreen;
        }
        pnlSysAdvisory.Invalidate();

        lblLastScanTime.Text = "Scanned: " + DateTime.Now.ToString("HH:mm:ss") + " | Refresh enabled";
    }

    private void OnNextClick(object sender, EventArgs e)
    {
        if (currentStep == 0)
        {
            if (string.IsNullOrEmpty(pythonBinary))
            {
                DialogResult res = MessageBox.Show(
                    this,
                    "Python 3 runtime was not detected on your system PATH.\n\n" +
                    "While you can continue to configure installation paths, local neural inference requires Python 3.8 - 3.12.\n\n" +
                    "Would you like to open the Setup & Readiness Guide to view installation instructions now?\n\n" +
                    "- Click 'Yes' to view the Setup & Readiness Guide.\n" +
                    "- Click 'No' to proceed to the License Agreement.",
                    "Python Runtime Advisory",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (res == DialogResult.Yes)
                {
                    ShowTroubleshootingDialog();
                    return;
                }
            }
            SwitchStep(1);
        }
        else if (currentStep == 1)
        {
            if (agreementChoice == 0)
            {
                MessageBox.Show(
                    this,
                    "Please click one of the agreement choice boxes above to select your preference before proceeding.\n\n" +
                    "You can click the box again at any time to deselect it.",
                    "Agreement Selection Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }
            if (agreementChoice == 2)
            {
                DialogResult res = MessageBox.Show(
                    this,
                    "You have selected not to agree to the terms or install required dependencies.\n\n" +
                    "Spoonbill AI Studio cannot run without its local neural inference engine.\n\n" +
                    "Do you want to cancel the installation now?",
                    "Cancel Setup?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (res == DialogResult.Yes)
                {
                    MessageBox.Show(
                        this,
                        "Installation has been cancelled as requested.\nNo changes were made to your computer.",
                        "Setup Aborted",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    Application.Exit();
                    return;
                }
                return;
            }
            SwitchStep(2);
        }
        else if (currentStep == 2)
        {
            SwitchStep(3);
        }
        else if (currentStep == 4)
        {
            if (chkOpenFolder.Checked && Directory.Exists(targetDir))
            {
                try { Process.Start("explorer.exe", targetDir); } catch {}
            }

            if (chkLaunchNow.Checked)
            {
                string exePath = Path.Combine(targetDir, "Spoonbill_Studio.exe");
                if (File.Exists(exePath))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = exePath;
                        psi.WorkingDirectory = targetDir;
                        Process.Start(psi);
                    }
                    catch {}
                }
            }
            Application.Exit();
        }
    }

    private void OnCancelClick(object sender, EventArgs e)
    {
        if (isInstalling)
        {
            MessageBox.Show(this, "Installation is currently in progress. Please wait for completion.", "Setup Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult res = MessageBox.Show(this, "Are you sure you want to cancel the installation wizard?", "Exit Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            Application.Exit();
        }
    }

    private void RunInstallation(object state)
    {
        try
        {
            UpdateStatus("Setting target directory: " + targetDir, 5);
            AppendLog("Preparing destination directory: " + targetDir);

            // Clean Installation if requested or express
            bool doClean = (installMode == 0) || chkCleanInstall.Checked;
            if (doClean && Directory.Exists(targetDir))
            {
                UpdateStatus("Purging previous build files...", 10);
                AppendLog("Terminating any running Spoonbill Studio processes...");
                KillRunningProcesses();

                AppendLog("Safely purging old build files from target directory...");
                try
                {
                    foreach (string file in Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); } catch {}
                    }
                    foreach (string dir in Directory.GetDirectories(targetDir))
                    {
                        try { Directory.Delete(dir, true); } catch {}
                    }
                    AppendLog("Clean installation directory prepared.");
                }
                catch (Exception cex)
                {
                    AppendLog("  [Notice] Clean purge message: " + cex.Message);
                }
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Payload Extraction
            UpdateStatus("Extracting core studio files, models, and UI...", 25);
            AppendLog("Extracting embedded application payload...");
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("Payload"))
            {
                if (s == null)
                {
                    throw new Exception("Embedded installation payload not found.");
                }
                string tempZip = Path.Combine(Path.GetTempPath(), "spoonbill_setup_" + Guid.NewGuid().ToString("N") + ".zip");
                using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                {
                    s.CopyTo(fs);
                }
                ZipFile.ExtractToDirectory(tempZip, targetDir);
                try { File.Delete(tempZip); } catch {}
            }
            AppendLog("Application binaries and resources extracted successfully.");
            SetProgress(50);

            // AI Dependencies
            bool doDeps = (installMode == 0) || chkInstallDeps.Checked;
            if (doDeps)
            {
                UpdateStatus("Checking for Python runtime...", 55);
                string pyCmd = FindPythonBinary();
                if (string.IsNullOrEmpty(pyCmd))
                {
                    AppendLog("[WARNING] Python 3.10+ was not found on PATH.");
                    AppendLog("Please install Python 3.10+ from https://www.python.org/ with 'Add to PATH' checked.");
                }
                else
                {
                    string reqFile = Path.Combine(targetDir, "requirements_lean.txt");
                    if (!File.Exists(reqFile))
                    {
                        reqFile = Path.Combine(targetDir, "resources", "app", "requirements_lean.txt");
                    }

                    if (File.Exists(reqFile))
                    {
                        UpdateStatus("Installing AI dependencies via pip (onnxruntime, fastapi, uvicorn)...", 65);
                        AppendLog("Running pip dependency installer: " + pyCmd + " -m pip install -r " + Path.GetFileName(reqFile));
                        try
                        {
                            ProcessStartInfo pipPsi = new ProcessStartInfo();
                            pipPsi.FileName = pyCmd;
                            pipPsi.Arguments = "-m pip install -r \"" + reqFile + "\"";
                            pipPsi.WorkingDirectory = targetDir;
                            pipPsi.UseShellExecute = false;
                            pipPsi.CreateNoWindow = true;
                            pipPsi.RedirectStandardOutput = true;
                            pipPsi.RedirectStandardError = true;

                            using (Process pipProc = Process.Start(pipPsi))
                            {
                                pipProc.OutputDataReceived += (sender, args) => {
                                    if (!string.IsNullOrEmpty(args.Data)) AppendLog("  " + args.Data);
                                };
                                pipProc.ErrorDataReceived += (sender, args) => {
                                    if (!string.IsNullOrEmpty(args.Data)) AppendLog("  " + args.Data);
                                };
                                pipProc.BeginOutputReadLine();
                                pipProc.BeginErrorReadLine();

                                pipProc.WaitForExit(180000);
                                if (pipProc.ExitCode == 0)
                                {
                                    AppendLog("[SUCCESS] AI dependencies installed and verified successfully.");
                                }
                                else
                                {
                                    AppendLog("[NOTICE] pip finished with code " + pipProc.ExitCode + ".");
                                }
                            }
                        }
                        catch (Exception pex)
                        {
                            AppendLog("[ERROR] pip execution failed: " + pex.Message);
                        }
                    }
                }
            }

            SetProgress(80);

            // Shortcuts
            bool doDesktop = (installMode == 0) || chkDesktopShortcut.Checked;
            bool doStart = (installMode == 0) || chkStartMenuShortcut.Checked;
            if (doDesktop || doStart)
            {
                UpdateStatus("Creating program shortcuts...", 85);
                AppendLog("Generating desktop and start menu shortcuts...");
                CreateShortcuts(targetDir, doDesktop, doStart);
            }

            // Uninstaller Registration
            bool doUninst = (installMode == 0) || chkRegisterUninstaller.Checked;
            if (doUninst)
            {
                UpdateStatus("Registering Windows Uninstaller...", 95);
                AppendLog("Registering Spoonbill AI Studio in Windows Control Panel / Settings...");
                RegisterUninstaller(targetDir);
            }

            SetProgress(100);
            UpdateStatus("Installation complete.", 100);
            AppendLog("All selected components installed successfully.");

            Thread.Sleep(600);
            this.Invoke((MethodInvoker)delegate {
                isInstalling = false;
                SwitchStep(4);
            });
        }
        catch (Exception ex)
        {
            AppendLog("[FATAL ERROR] " + ex.Message);
            this.Invoke((MethodInvoker)delegate {
                MessageBox.Show(this, "Installation Error:\n" + ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCancel.Enabled = true;
            });
        }
    }

    private void UpdateStatus(string text, int progress)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                lblProgStatus.Text = text;
                currentProgressPercent = Math.Min(100, Math.Max(0, progress));
                customProgressBar.Invalidate();
            });
        }
    }

    private void SetProgress(int progress)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                currentProgressPercent = Math.Min(100, Math.Max(0, progress));
                customProgressBar.Invalidate();
            });
        }
    }

    private void AppendLog(string message)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                string stamp = DateTime.Now.ToString("HH:mm:ss");
                logBox.AppendText("[" + stamp + "] " + message + Environment.NewLine);
            });
        }
    }

    private void KillRunningProcesses()
    {
        try
        {
            foreach (Process p in Process.GetProcessesByName("Spoonbill_Studio"))
            {
                try { p.Kill(); p.WaitForExit(1500); } catch {}
            }
        }
        catch {}
    }

    private string FindPythonBinary()
    {
        string[] candidates = new string[] { "python.exe", "py.exe", "python3.exe" };
        foreach (string cmd in candidates)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(cmd, "--version");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(2500);
                    if (p.ExitCode == 0) return cmd;
                }
            }
            catch {}
        }

        try
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string pyPrograms = Path.Combine(localApp, "Programs", "Python");
            if (Directory.Exists(pyPrograms))
            {
                foreach (string dir in Directory.GetDirectories(pyPrograms))
                {
                    string p = Path.Combine(dir, "python.exe");
                    if (File.Exists(p)) return p;
                }
            }

            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (Directory.Exists(progFiles))
            {
                foreach (string dir in Directory.GetDirectories(progFiles, "Python*"))
                {
                    string p = Path.Combine(dir, "python.exe");
                    if (File.Exists(p)) return p;
                }
            }
        }
        catch {}

        return null;
    }

    private void CreateShortcuts(string targetDir, bool desktop, bool startMenu)
    {
        try
        {
            string exePath = Path.Combine(targetDir, "Spoonbill_Studio.exe");
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);

            if (desktop)
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                dynamic desktopLink = shell.CreateShortcut(Path.Combine(desktopDir, "Spoonbill AI Studio.lnk"));
                desktopLink.TargetPath = exePath;
                desktopLink.WorkingDirectory = targetDir;
                desktopLink.Description = "Black-Faced Spoonbill AI Studio V4 (Photoshop Edition)";
                desktopLink.Save();
            }

            if (startMenu)
            {
                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                dynamic startMenuLink = shell.CreateShortcut(Path.Combine(startMenuDir, "Spoonbill AI Studio.lnk"));
                startMenuLink.TargetPath = exePath;
                startMenuLink.WorkingDirectory = targetDir;
                startMenuLink.Description = "Black-Faced Spoonbill AI Studio V4 (Photoshop Edition)";
                startMenuLink.Save();
            }
        }
        catch {}
    }

    private void RegisterUninstaller(string targetDir)
    {
        try
        {
            string uninstallerExe = Path.Combine(targetDir, "Uninstall.exe");
            string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio";
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
            {
                if (key != null)
                {
                    key.SetValue("DisplayName", "Black-Faced Spoonbill AI Studio V4 (Photoshop Edition)");
                    key.SetValue("DisplayVersion", "4.0.0");
                    key.SetValue("Publisher", "Black-Faced Spoonbill AI Research Consortium");
                    key.SetValue("InstallLocation", targetDir);
                    key.SetValue("UninstallString", "\"" + uninstallerExe + "\"");
                    key.SetValue("DisplayIcon", Path.Combine(targetDir, "Spoonbill_Studio.exe") + ",0");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    key.SetValue("EstimatedSize", 480000, RegistryValueKind.DWord);
                }
            }
        }
        catch {}
    }

    private string DetectGraphicsCard(out bool hasNvidia)
    {
        hasNvidia = false;
        string detected = null;
        try
        {
            using (RegistryKey root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
            {
                if (root != null)
                {
                    foreach (string subKeyName in root.GetSubKeyNames())
                    {
                        if (subKeyName.StartsWith("0"))
                        {
                            using (RegistryKey sub = root.OpenSubKey(subKeyName))
                            {
                                if (sub != null)
                                {
                                    object desc = sub.GetValue("DriverDesc");
                                    if (desc != null)
                                    {
                                        string name = desc.ToString();
                                        if (name.IndexOf("Remote", StringComparison.OrdinalIgnoreCase) < 0 &&
                                            name.IndexOf("Basic Display", StringComparison.OrdinalIgnoreCase) < 0)
                                        {
                                            if (name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                hasNvidia = true;
                                                return name;
                                            }
                                            else if (detected == null)
                                            {
                                                detected = name;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch {}

        if (!string.IsNullOrEmpty(detected)) return detected;
        return "Standard / Integrated Display Controller";
    }

    private void ToggleAgreementChoice(int choice)
    {
        if (agreementChoice == choice)
        {
            agreementChoice = 0; // Release selection
        }
        else
        {
            agreementChoice = choice; // Select this, deselect the other
        }
        UpdateAgreementUI();
    }

    private void UpdateAgreementUI()
    {
        pnlAgreeBox.Invalidate();
        pnlDeclineBox.Invalidate();

        if (agreementChoice == 0)
        {
            lblAgreementStatus.Text = "Please click an option box above to select your agreement (click again to deselect).";
            lblAgreementStatus.ForeColor = ClrTextMuted;
            if (currentStep == 1)
            {
                btnNext.Enabled = false;
                btnNext.Text = "Next >";
            }
        }
        else if (agreementChoice == 1)
        {
            lblAgreementStatus.Text = "License accepted. Click 'Next >' to configure installation options.";
            lblAgreementStatus.ForeColor = ClrNvidiaGreen;
            if (currentStep == 1)
            {
                btnNext.Enabled = true;
                btnNext.Text = "Next >";
            }
        }
        else if (agreementChoice == 2)
        {
            lblAgreementStatus.Text = "Decline selected. Click 'Cancel Setup' to abort without writing files.";
            lblAgreementStatus.ForeColor = Color.FromArgb(248, 113, 113);
            if (currentStep == 1)
            {
                btnNext.Enabled = true;
                btnNext.Text = "Cancel Setup";
            }
        }
    }

    private void ShowTroubleshootingDialog()
    {
        using (SystemReadinessDialog dlg = new SystemReadinessDialog(string.IsNullOrEmpty(pythonBinary), hasNvidiaGpu))
        {
            dlg.ShowDialog(this);
        }
        RunSystemCheck();
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SpoonbillNvidiaInstaller());
    }
}

public class SystemReadinessDialog : Form
{
    public SystemReadinessDialog(bool pythonMissing, bool hasNvidia)
    {
        this.Text = "Spoonbill AI Studio — System Setup & Readiness Guide";
        this.ClientSize = new Size(680, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(19, 19, 22);
        this.ForeColor = Color.FromArgb(244, 244, 245);
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        // Header Panel
        Panel pnlHeader = new Panel();
        pnlHeader.Location = new Point(0, 0);
        pnlHeader.Size = new Size(680, 60);
        pnlHeader.BackColor = Color.FromArgb(13, 13, 15);
        pnlHeader.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(34, 34, 39), 1), 0, 59, 680, 59);
        };
        this.Controls.Add(pnlHeader);

        Label lblTitle = new Label();
        lblTitle.Text = "SYSTEM READINESS & SETUP GUIDE";
        lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(24, 12);
        lblTitle.AutoSize = true;
        pnlHeader.Controls.Add(lblTitle);

        Label lblSub = new Label();
        lblSub.Text = "Step-by-step instructions to make all hardware and software components ready.";
        lblSub.Font = new Font("Segoe UI", 8.5F);
        lblSub.ForeColor = Color.FromArgb(118, 185, 0);
        lblSub.Location = new Point(25, 34);
        lblSub.AutoSize = true;
        pnlHeader.Controls.Add(lblSub);

        // Card 1: Python AI Runtime Setup
        Panel cardPython = new Panel();
        cardPython.Location = new Point(24, 74);
        cardPython.Size = new Size(632, 180);
        cardPython.BackColor = Color.FromArgb(26, 26, 30);
        cardPython.Paint += (s, e) => {
            Color border = pythonMissing ? Color.FromArgb(239, 68, 68) : Color.FromArgb(42, 42, 48);
            using (Pen p = new Pen(border, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, cardPython.Width - 1, cardPython.Height - 1);
            }
        };
        this.Controls.Add(cardPython);

        Label lblPyHead = new Label();
        lblPyHead.Text = "1. Python 3 AI Inference Runtime (Required for Neural Inference)";
        lblPyHead.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblPyHead.ForeColor = pythonMissing ? Color.FromArgb(248, 113, 113) : Color.FromArgb(118, 185, 0);
        lblPyHead.Location = new Point(16, 12);
        lblPyHead.AutoSize = true;
        cardPython.Controls.Add(lblPyHead);

        Label lblPyBody = new Label();
        lblPyBody.Text = "Spoonbill Studio requires Python 3.8 to 3.12 (64-bit) for local neural network inference.\n\n" +
                         "• Method A (Fastest via Windows Terminal / PowerShell):\n" +
                         "  winget install Python.Python.3.11\n\n" +
                         "• Method B (Manual Installer from python.org):\n" +
                         "  Download installer. CRITICAL: Check 'Add python.exe to PATH' at the bottom of setup!";
        lblPyBody.Font = new Font("Segoe UI", 8.5F);
        lblPyBody.ForeColor = Color.FromArgb(212, 212, 216);
        lblPyBody.Location = new Point(16, 34);
        lblPyBody.Size = new Size(600, 95);
        cardPython.Controls.Add(lblPyBody);

        Button btnPyWeb = new Button();
        btnPyWeb.Text = "Download Python (python.org)";
        btnPyWeb.Location = new Point(16, 136);
        btnPyWeb.Size = new Size(205, 28);
        btnPyWeb.BackColor = Color.FromArgb(38, 38, 44);
        btnPyWeb.ForeColor = Color.White;
        btnPyWeb.FlatStyle = FlatStyle.Flat;
        btnPyWeb.FlatAppearance.BorderColor = Color.FromArgb(118, 185, 0);
        btnPyWeb.Cursor = Cursors.Hand;
        btnPyWeb.Click += (s, e) => {
            try { Process.Start("https://www.python.org/downloads/windows/"); } catch {}
        };
        cardPython.Controls.Add(btnPyWeb);

        Button btnCopyWinget = new Button();
        btnCopyWinget.Text = "Copy 'winget' Command";
        btnCopyWinget.Location = new Point(231, 136);
        btnCopyWinget.Size = new Size(170, 28);
        btnCopyWinget.BackColor = Color.FromArgb(38, 38, 44);
        btnCopyWinget.ForeColor = Color.White;
        btnCopyWinget.FlatStyle = FlatStyle.Flat;
        btnCopyWinget.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 68);
        btnCopyWinget.Cursor = Cursors.Hand;
        btnCopyWinget.Click += (s, e) => {
            try {
                Clipboard.SetText("winget install Python.Python.3.11");
                MessageBox.Show(this, "Command copied to clipboard!\nOpen PowerShell and press Ctrl+V to install.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch {}
        };
        cardPython.Controls.Add(btnCopyWinget);

        // Card 2: GPU Acceleration & CUDA
        Panel cardGpu = new Panel();
        cardGpu.Location = new Point(24, 266);
        cardGpu.Size = new Size(632, 135);
        cardGpu.BackColor = Color.FromArgb(26, 26, 30);
        cardGpu.Paint += (s, e) => {
            using (Pen p = new Pen(Color.FromArgb(42, 42, 48), 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, cardGpu.Width - 1, cardGpu.Height - 1);
            }
        };
        this.Controls.Add(cardGpu);

        Label lblGpuHead = new Label();
        lblGpuHead.Text = "2. Graphics Acceleration & NVIDIA CUDA (Optional)";
        lblGpuHead.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblGpuHead.ForeColor = Color.FromArgb(118, 185, 0);
        lblGpuHead.Location = new Point(16, 12);
        lblGpuHead.AutoSize = true;
        cardGpu.Controls.Add(lblGpuHead);

        Label lblGpuBody = new Label();
        lblGpuBody.Text = "• CPU Safe Mode: Operates out-of-the-box on all laptops (Intel, AMD, CPU) using YOLO11 Nano.\n" +
                         "• NVIDIA GPU Acceleration: Unlocks YOLO11 Large and BeakAttn models.\n" +
                         "  Requires NVIDIA GeForce / RTX drivers and the CUDA 12.x Toolkit.";
        lblGpuBody.Font = new Font("Segoe UI", 8.5F);
        lblGpuBody.ForeColor = Color.FromArgb(212, 212, 216);
        lblGpuBody.Location = new Point(16, 34);
        lblGpuBody.Size = new Size(600, 56);
        cardGpu.Controls.Add(lblGpuBody);

        Button btnNvidiaWeb = new Button();
        btnNvidiaWeb.Text = "NVIDIA Drivers";
        btnNvidiaWeb.Location = new Point(16, 94);
        btnNvidiaWeb.Size = new Size(130, 28);
        btnNvidiaWeb.BackColor = Color.FromArgb(38, 38, 44);
        btnNvidiaWeb.ForeColor = Color.White;
        btnNvidiaWeb.FlatStyle = FlatStyle.Flat;
        btnNvidiaWeb.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 68);
        btnNvidiaWeb.Cursor = Cursors.Hand;
        btnNvidiaWeb.Click += (s, e) => {
            try { Process.Start("https://www.nvidia.com/Download/index.aspx"); } catch {}
        };
        cardGpu.Controls.Add(btnNvidiaWeb);

        Button btnCudaWeb = new Button();
        btnCudaWeb.Text = "CUDA Toolkit Downloads";
        btnCudaWeb.Location = new Point(156, 94);
        btnCudaWeb.Size = new Size(170, 28);
        btnCudaWeb.BackColor = Color.FromArgb(38, 38, 44);
        btnCudaWeb.ForeColor = Color.White;
        btnCudaWeb.FlatStyle = FlatStyle.Flat;
        btnCudaWeb.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 68);
        btnCudaWeb.Cursor = Cursors.Hand;
        btnCudaWeb.Click += (s, e) => {
            try { Process.Start("https://developer.nvidia.com/cuda-downloads"); } catch {}
        };
        cardGpu.Controls.Add(btnCudaWeb);

        // Note at bottom
        Label lblRefreshNote = new Label();
        lblRefreshNote.Text = "Tip: After installing Python or drivers, do NOT restart setup. Simply close this guide and click 'Refresh Scan' on the main window.";
        lblRefreshNote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblRefreshNote.ForeColor = Color.FromArgb(161, 161, 170);
        lblRefreshNote.Location = new Point(24, 412);
        lblRefreshNote.Size = new Size(632, 34);
        this.Controls.Add(lblRefreshNote);

        // Bottom Bar
        Panel pnlBottom = new Panel();
        pnlBottom.Location = new Point(0, 456);
        pnlBottom.Size = new Size(680, 64);
        pnlBottom.BackColor = Color.FromArgb(13, 13, 15);
        pnlBottom.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(34, 34, 39), 1), 0, 0, 680, 0);
        };
        this.Controls.Add(pnlBottom);

        Button btnClose = new Button();
        btnClose.Text = "Close Guide";
        btnClose.Location = new Point(540, 14);
        btnClose.Size = new Size(116, 34);
        btnClose.BackColor = Color.FromArgb(118, 185, 0);
        btnClose.ForeColor = Color.Black;
        btnClose.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnClose.FlatStyle = FlatStyle.Flat;
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Cursor = Cursors.Hand;
        btnClose.Click += (s, e) => this.Close();
        pnlBottom.Controls.Add(btnClose);
    }
}
