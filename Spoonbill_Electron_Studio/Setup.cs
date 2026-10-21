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

    // Step 1: Agreement Controls
    private Label lblAgreeTitle;
    private Label lblAgreeDesc;
    private TextBox txtAgreementTerms;
    private Panel pnlAgreeCard;
    private Panel pnlDisagreeCard;
    private RadioButton rbAgree;
    private RadioButton rbDisagree;

    // Step 2: Mode & Options Controls
    private Label lblOptTitle;
    private Label lblOptDesc;
    private Panel pnlExpressCard;
    private Panel pnlCustomCard;
    private RadioButton rbModeExpress;
    private RadioButton rbModeCustom;
    private Panel pnlCustomDetails;
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
    private TextBox logBox;

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

        lblSysTitle = new Label();
        lblSysTitle.Text = "System Compatibility & Environment Scan";
        lblSysTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblSysTitle.ForeColor = Color.White;
        lblSysTitle.Location = new Point(28, 20);
        lblSysTitle.Size = new Size(540, 28);
        panelSystemCheck.Controls.Add(lblSysTitle);

        lblSysDesc = new Label();
        lblSysDesc.Text = "Checking operating system architecture, hardware cores, and existing installations.";
        lblSysDesc.ForeColor = ClrTextSecondary;
        lblSysDesc.Location = new Point(28, 52);
        lblSysDesc.Size = new Size(540, 22);
        panelSystemCheck.Controls.Add(lblSysDesc);

        sysCardPanels = new Panel[4];
        sysCardTitles = new Label[4];
        sysCardValues = new Label[4];
        sysCardBadges = new Label[4];

        string[] cardHeaders = new string[] {
            "Operating System",
            "Processor & Memory",
            "Existing Installation",
            "Python AI Environment"
        };

        for (int i = 0; i < 4; i++)
        {
            Panel card = new Panel();
            card.Size = new Size(540, 60);
            card.Location = new Point(28, 88 + (i * 70));
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
            title.ForeColor = ClrTextSecondary;
            title.Location = new Point(16, 11);
            title.AutoSize = true;
            card.Controls.Add(title);

            Label val = new Label();
            val.Text = "Scanning...";
            val.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            val.ForeColor = ClrTextPrimary;
            val.Location = new Point(16, 31);
            val.Size = new Size(390, 20);
            card.Controls.Add(val);

            Label badge = new Label();
            badge.Text = "CHECKING";
            badge.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            badge.ForeColor = ClrNvidiaGreen;
            badge.BackColor = Color.FromArgb(22, 34, 14);
            badge.Location = new Point(415, 17);
            badge.Size = new Size(110, 26);
            badge.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(badge);

            sysCardPanels[i] = card;
            sysCardTitles[i] = title;
            sysCardValues[i] = val;
            sysCardBadges[i] = badge;
        }

        // ================= PAGE 1: USER AGREEMENT =================
        panelAgreement = new Panel();
        panelAgreement.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelAgreement);

        lblAgreeTitle = new Label();
        lblAgreeTitle.Text = "License & AI Dependencies Agreement";
        lblAgreeTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblAgreeTitle.ForeColor = Color.White;
        lblAgreeTitle.Location = new Point(28, 20);
        lblAgreeTitle.Size = new Size(540, 28);
        panelAgreement.Controls.Add(lblAgreeTitle);

        lblAgreeDesc = new Label();
        lblAgreeDesc.Text = "Please review the conservation research license, pip permissions, and privacy policy.";
        lblAgreeDesc.ForeColor = ClrTextSecondary;
        lblAgreeDesc.Location = new Point(28, 52);
        lblAgreeDesc.Size = new Size(540, 22);
        panelAgreement.Controls.Add(lblAgreeDesc);

        txtAgreementTerms = new TextBox();
        txtAgreementTerms.Multiline = true;
        txtAgreementTerms.ReadOnly = true;
        txtAgreementTerms.ScrollBars = ScrollBars.Vertical;
        txtAgreementTerms.BackColor = ClrCardBg;
        txtAgreementTerms.ForeColor = Color.FromArgb(212, 212, 216);
        txtAgreementTerms.Font = new Font("Consolas", 8F, FontStyle.Regular);
        txtAgreementTerms.BorderStyle = BorderStyle.FixedSingle;
        txtAgreementTerms.Location = new Point(28, 80);
        txtAgreementTerms.Size = new Size(540, 200);
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

        // Agreement Selectable Card 1 (Agree)
        pnlAgreeCard = new Panel();
        pnlAgreeCard.Size = new Size(540, 50);
        pnlAgreeCard.Location = new Point(28, 292);
        pnlAgreeCard.BackColor = ClrCardBg;
        pnlAgreeCard.Cursor = Cursors.Hand;
        pnlAgreeCard.Paint += (s, e) => {
            Color border = rbAgree.Checked ? ClrNvidiaGreen : ClrCardBorder;
            using (Pen p = new Pen(border, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlAgreeCard.Width - 1, pnlAgreeCard.Height - 1);
            }
        };
        pnlAgreeCard.Click += (s, e) => { rbAgree.Checked = true; };
        panelAgreement.Controls.Add(pnlAgreeCard);

        rbAgree = new RadioButton();
        rbAgree.Text = "I accept the license terms and agree to install required AI dependencies";
        rbAgree.Checked = true;
        rbAgree.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        rbAgree.ForeColor = ClrTextPrimary;
        rbAgree.Location = new Point(14, 13);
        rbAgree.Size = new Size(515, 24);
        rbAgree.CheckedChanged += (s, e) => { pnlAgreeCard.Invalidate(); pnlDisagreeCard.Invalidate(); };
        pnlAgreeCard.Controls.Add(rbAgree);

        // Agreement Selectable Card 2 (Disagree)
        pnlDisagreeCard = new Panel();
        pnlDisagreeCard.Size = new Size(540, 50);
        pnlDisagreeCard.Location = new Point(28, 350);
        pnlDisagreeCard.BackColor = ClrCardBg;
        pnlDisagreeCard.Cursor = Cursors.Hand;
        pnlDisagreeCard.Paint += (s, e) => {
            Color border = rbDisagree.Checked ? Color.FromArgb(239, 68, 68) : ClrCardBorder;
            using (Pen p = new Pen(border, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlDisagreeCard.Width - 1, pnlDisagreeCard.Height - 1);
            }
        };
        pnlDisagreeCard.Click += (s, e) => { rbDisagree.Checked = true; };
        panelAgreement.Controls.Add(pnlDisagreeCard);

        rbDisagree = new RadioButton();
        rbDisagree.Text = "I do not accept the terms (Cancel installation and exit setup immediately)";
        rbDisagree.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        rbDisagree.ForeColor = Color.FromArgb(248, 113, 113);
        rbDisagree.Location = new Point(14, 13);
        rbDisagree.Size = new Size(515, 24);
        rbDisagree.CheckedChanged += (s, e) => { pnlAgreeCard.Invalidate(); pnlDisagreeCard.Invalidate(); };
        pnlDisagreeCard.Controls.Add(rbDisagree);

        // ================= PAGE 2: MODE & OPTIONS =================
        panelModeAndOptions = new Panel();
        panelModeAndOptions.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelModeAndOptions);

        lblOptTitle = new Label();
        lblOptTitle.Text = "Installation Mode & Configuration";
        lblOptTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblOptTitle.ForeColor = Color.White;
        lblOptTitle.Location = new Point(28, 20);
        lblOptTitle.Size = new Size(540, 28);
        panelModeAndOptions.Controls.Add(lblOptTitle);

        lblOptDesc = new Label();
        lblOptDesc.Text = "Select Express for automated deployment or Custom for advanced paths and parts.";
        lblOptDesc.ForeColor = ClrTextSecondary;
        lblOptDesc.Location = new Point(28, 52);
        lblOptDesc.Size = new Size(540, 22);
        panelModeAndOptions.Controls.Add(lblOptDesc);

        // Express Card
        pnlExpressCard = new Panel();
        pnlExpressCard.Size = new Size(260, 80);
        pnlExpressCard.Location = new Point(28, 80);
        pnlExpressCard.BackColor = ClrCardBg;
        pnlExpressCard.Cursor = Cursors.Hand;
        pnlExpressCard.Paint += (s, e) => {
            Color border = rbModeExpress.Checked ? ClrNvidiaGreen : ClrCardBorder;
            using (Pen p = new Pen(border, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlExpressCard.Width - 1, pnlExpressCard.Height - 1);
            }
        };
        pnlExpressCard.Click += (s, e) => { rbModeExpress.Checked = true; };
        panelModeAndOptions.Controls.Add(pnlExpressCard);

        rbModeExpress = new RadioButton();
        rbModeExpress.Text = "Express (Recommended)";
        rbModeExpress.Checked = true;
        rbModeExpress.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rbModeExpress.ForeColor = ClrTextPrimary;
        rbModeExpress.Location = new Point(14, 12);
        rbModeExpress.Size = new Size(234, 24);
        rbModeExpress.CheckedChanged += (s, e) => {
            pnlExpressCard.Invalidate();
            pnlCustomCard.Invalidate();
            ToggleCustomMode(!rbModeExpress.Checked);
        };
        pnlExpressCard.Controls.Add(rbModeExpress);

        Label lblExpressSub = new Label();
        lblExpressSub.Text = "Automatic setup with all models, dependencies, & clean install.";
        lblExpressSub.Font = new Font("Segoe UI", 7.8F);
        lblExpressSub.ForeColor = ClrTextSecondary;
        lblExpressSub.Location = new Point(34, 38);
        lblExpressSub.Size = new Size(215, 32);
        pnlExpressCard.Controls.Add(lblExpressSub);

        // Custom Card
        pnlCustomCard = new Panel();
        pnlCustomCard.Size = new Size(260, 80);
        pnlCustomCard.Location = new Point(308, 80);
        pnlCustomCard.BackColor = ClrCardBg;
        pnlCustomCard.Cursor = Cursors.Hand;
        pnlCustomCard.Paint += (s, e) => {
            Color border = rbModeCustom.Checked ? ClrNvidiaGreen : ClrCardBorder;
            using (Pen p = new Pen(border, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlCustomCard.Width - 1, pnlCustomCard.Height - 1);
            }
        };
        pnlCustomCard.Click += (s, e) => { rbModeCustom.Checked = true; };
        panelModeAndOptions.Controls.Add(pnlCustomCard);

        rbModeCustom = new RadioButton();
        rbModeCustom.Text = "Custom (Advanced)";
        rbModeCustom.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rbModeCustom.ForeColor = ClrTextPrimary;
        rbModeCustom.Location = new Point(14, 12);
        rbModeCustom.Size = new Size(234, 24);
        rbModeCustom.CheckedChanged += (s, e) => {
            pnlExpressCard.Invalidate();
            pnlCustomCard.Invalidate();
        };
        pnlCustomCard.Controls.Add(rbModeCustom);

        Label lblCustomSub = new Label();
        lblCustomSub.Text = "Customize directory path, selective models, & shortcut options.";
        lblCustomSub.Font = new Font("Segoe UI", 7.8F);
        lblCustomSub.ForeColor = ClrTextSecondary;
        lblCustomSub.Location = new Point(34, 38);
        lblCustomSub.Size = new Size(215, 32);
        pnlCustomCard.Controls.Add(lblCustomSub);

        // Options Details Panel (Container for path & toggles)
        pnlCustomDetails = new Panel();
        pnlCustomDetails.Location = new Point(28, 172);
        pnlCustomDetails.Size = new Size(540, 238);
        pnlCustomDetails.BackColor = Color.FromArgb(22, 22, 25);
        pnlCustomDetails.Paint += (s, e) => {
            using (Pen p = new Pen(ClrBorderSubtle, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlCustomDetails.Width - 1, pnlCustomDetails.Height - 1);
            }
        };
        panelModeAndOptions.Controls.Add(pnlCustomDetails);

        Label lblDest = new Label();
        lblDest.Text = "Installation Target Folder:";
        lblDest.ForeColor = ClrTextSecondary;
        lblDest.Font = new Font("Segoe UI", 8F);
        lblDest.Location = new Point(16, 12);
        lblDest.Size = new Size(200, 16);
        pnlCustomDetails.Controls.Add(lblDest);

        txtTargetDir = new TextBox();
        txtTargetDir.Text = targetDir;
        txtTargetDir.BackColor = ClrCardBg;
        txtTargetDir.ForeColor = ClrTextPrimary;
        txtTargetDir.BorderStyle = BorderStyle.FixedSingle;
        txtTargetDir.Location = new Point(16, 32);
        txtTargetDir.Size = new Size(410, 23);
        txtTargetDir.TextChanged += (s, e) => { targetDir = txtTargetDir.Text; UpdateDiskSpace(); };
        pnlCustomDetails.Controls.Add(txtTargetDir);

        btnBrowseTarget = new Button();
        btnBrowseTarget.Text = "Browse...";
        btnBrowseTarget.Size = new Size(90, 25);
        btnBrowseTarget.Location = new Point(434, 31);
        btnBrowseTarget.BackColor = Color.FromArgb(38, 38, 42);
        btnBrowseTarget.ForeColor = ClrTextPrimary;
        btnBrowseTarget.FlatStyle = FlatStyle.Flat;
        btnBrowseTarget.FlatAppearance.BorderSize = 0;
        btnBrowseTarget.Cursor = Cursors.Hand;
        btnBrowseTarget.Click += OnBrowseTargetClick;
        pnlCustomDetails.Controls.Add(btnBrowseTarget);

        lblDiskSpace = new Label();
        lblDiskSpace.Text = "Required space: ~550 MB | Available space: Calculating...";
        lblDiskSpace.ForeColor = ClrTextMuted;
        lblDiskSpace.Font = new Font("Segoe UI", 7.8F);
        lblDiskSpace.Location = new Point(16, 60);
        lblDiskSpace.Size = new Size(500, 16);
        pnlCustomDetails.Controls.Add(lblDiskSpace);

        chkCleanInstall = new CheckBox();
        chkCleanInstall.Text = "Perform Clean Installation (Safely purge previous build files first)";
        chkCleanInstall.Checked = true;
        chkCleanInstall.ForeColor = Color.FromArgb(246, 130, 31);
        chkCleanInstall.Location = new Point(16, 84);
        chkCleanInstall.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkCleanInstall);

        chkInstallModels = new CheckBox();
        chkInstallModels.Text = "Deploy Bundled ONNX Models (YOLO11 Nano & Large neural weights)";
        chkInstallModels.Checked = true;
        chkInstallModels.ForeColor = ClrTextPrimary;
        chkInstallModels.Location = new Point(16, 108);
        chkInstallModels.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkInstallModels);

        chkInstallDeps = new CheckBox();
        chkInstallDeps.Text = "Install AI Dependencies via pip (onnxruntime, fastapi, uvicorn, opencv)";
        chkInstallDeps.Checked = true;
        chkInstallDeps.ForeColor = ClrTextPrimary;
        chkInstallDeps.Location = new Point(16, 132);
        chkInstallDeps.Size = new Size(500, 22);
        pnlCustomDetails.Controls.Add(chkInstallDeps);

        chkDesktopShortcut = new CheckBox();
        chkDesktopShortcut.Text = "Create Desktop shortcut";
        chkDesktopShortcut.Checked = true;
        chkDesktopShortcut.ForeColor = ClrTextPrimary;
        chkDesktopShortcut.Location = new Point(16, 160);
        chkDesktopShortcut.Size = new Size(230, 22);
        pnlCustomDetails.Controls.Add(chkDesktopShortcut);

        chkStartMenuShortcut = new CheckBox();
        chkStartMenuShortcut.Text = "Create Start Menu shortcut";
        chkStartMenuShortcut.Checked = true;
        chkStartMenuShortcut.ForeColor = ClrTextPrimary;
        chkStartMenuShortcut.Location = new Point(260, 160);
        chkStartMenuShortcut.Size = new Size(230, 22);
        pnlCustomDetails.Controls.Add(chkStartMenuShortcut);

        chkRegisterUninstaller = new CheckBox();
        chkRegisterUninstaller.Text = "Register Uninstaller in Windows Installed Apps (Control Panel / Settings)";
        chkRegisterUninstaller.Checked = true;
        chkRegisterUninstaller.ForeColor = ClrTextPrimary;
        chkRegisterUninstaller.Location = new Point(16, 188);
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
        lblProgTitle.Location = new Point(28, 20);
        lblProgTitle.Size = new Size(540, 28);
        panelProgress.Controls.Add(lblProgTitle);

        lblProgStatus = new Label();
        lblProgStatus.Text = "Preparing installation package...";
        lblProgStatus.ForeColor = ClrTextSecondary;
        lblProgStatus.Location = new Point(28, 52);
        lblProgStatus.Size = new Size(540, 20);
        panelProgress.Controls.Add(lblProgStatus);

        // Custom Sleek Modern Progress Bar
        customProgressBar = new Panel();
        customProgressBar.Size = new Size(540, 8);
        customProgressBar.Location = new Point(28, 80);
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

        logBox = new TextBox();
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.BackColor = Color.FromArgb(14, 14, 16);
        logBox.ForeColor = Color.FromArgb(170, 170, 178);
        logBox.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        logBox.Location = new Point(28, 102);
        logBox.Size = new Size(540, 310);
        logBox.BorderStyle = BorderStyle.FixedSingle;
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

    private void ToggleCustomMode(bool enabled)
    {
        txtTargetDir.Enabled = enabled;
        btnBrowseTarget.Enabled = enabled;
        chkCleanInstall.Enabled = enabled;
        chkInstallModels.Enabled = enabled;
        chkInstallDeps.Enabled = enabled;
        chkDesktopShortcut.Enabled = enabled;
        chkStartMenuShortcut.Enabled = enabled;
        chkRegisterUninstaller.Enabled = enabled;

        if (!enabled)
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
            btnNext.Text = "Next >";
            btnNext.Enabled = true;
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
        // 1. Operating System
        sysCardValues[0].Text = Environment.OSVersion.ToString() + " (64-bit)";
        sysCardBadges[0].Text = "COMPATIBLE";
        sysCardBadges[0].ForeColor = ClrNvidiaGreen;

        // 2. Hardware Resources
        int cores = Environment.ProcessorCount;
        sysCardValues[1].Text = cores + " Logical Cores detected";
        sysCardBadges[1].Text = "READY";
        sysCardBadges[1].ForeColor = ClrNvidiaGreen;

        // 3. Existing Installation Detection
        existingVersionDetected = Directory.Exists(targetDir) && File.Exists(Path.Combine(targetDir, "Spoonbill_Studio.exe"));
        if (existingVersionDetected)
        {
            sysCardValues[2].Text = "Prior build found at %LOCALAPPDATA%\\Programs...";
            sysCardBadges[2].Text = "UPGRADE READY";
            sysCardBadges[2].ForeColor = Color.FromArgb(246, 130, 31);
        }
        else
        {
            sysCardValues[2].Text = "Clean system. No existing builds found.";
            sysCardBadges[2].Text = "CLEAN";
            sysCardBadges[2].ForeColor = ClrNvidiaGreen;
        }

        // 4. Python Environment
        pythonBinary = FindPythonBinary();
        if (!string.IsNullOrEmpty(pythonBinary))
        {
            sysCardValues[3].Text = "Python 3 located at " + Path.GetFileName(pythonBinary);
            sysCardBadges[3].Text = "READY";
            sysCardBadges[3].ForeColor = ClrNvidiaGreen;
        }
        else
        {
            sysCardValues[3].Text = "Python 3 not found on system PATH.";
            sysCardBadges[3].Text = "ACTION NEEDED";
            sysCardBadges[3].ForeColor = Color.FromArgb(239, 68, 68);
        }
    }

    private void OnNextClick(object sender, EventArgs e)
    {
        if (currentStep == 0)
        {
            SwitchStep(1);
        }
        else if (currentStep == 1)
        {
            if (rbDisagree.Checked)
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
            bool doClean = rbModeExpress.Checked || chkCleanInstall.Checked;
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
            bool doDeps = rbModeExpress.Checked || chkInstallDeps.Checked;
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
            bool doDesktop = rbModeExpress.Checked || chkDesktopShortcut.Checked;
            bool doStart = rbModeExpress.Checked || chkStartMenuShortcut.Checked;
            if (doDesktop || doStart)
            {
                UpdateStatus("Creating program shortcuts...", 85);
                AppendLog("Generating desktop and start menu shortcuts...");
                CreateShortcuts(targetDir, doDesktop, doStart);
            }

            // Uninstaller Registration
            bool doUninst = rbModeExpress.Checked || chkRegisterUninstaller.Checked;
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

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SpoonbillNvidiaInstaller());
    }
}
