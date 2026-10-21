using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

public class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        this.DoubleBuffered = true;
        this.SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true
        );
        this.UpdateStyles();
    }
}

public class ModernButton : BufferedPanel
{
    private string buttonText = "";
    private Color activeBack = Color.FromArgb(44, 44, 52);
    private Color activeFore = Color.White;
    private Color borderColor = Color.FromArgb(76, 76, 88);
    private Font textFont = new Font("Segoe UI", 8.75F, FontStyle.Bold);
    private bool isInteractive = true;
    private bool isHovered = false;
    public event EventHandler ButtonClick;

    public string ButtonText
    {
        get { return buttonText; }
        set { buttonText = value; this.Invalidate(); }
    }

    public Color ActiveBackColor
    {
        get { return activeBack; }
        set { activeBack = value; this.Invalidate(); }
    }

    public Color ActiveForeColor
    {
        get { return activeFore; }
        set { activeFore = value; this.Invalidate(); }
    }

    public Color BorderColor
    {
        get { return borderColor; }
        set { borderColor = value; this.Invalidate(); }
    }

    public Font TextFont
    {
        get { return textFont; }
        set { textFont = value; this.Invalidate(); }
    }

    public bool IsInteractive
    {
        get { return isInteractive; }
        set
        {
            isInteractive = value;
            this.Cursor = isInteractive ? Cursors.Hand : Cursors.Default;
            this.Invalidate();
        }
    }

    public ModernButton()
    {
        this.Cursor = Cursors.Hand;
        this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
        this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
        this.MouseDown += (s, e) => {
            if (isInteractive && e.Button == MouseButtons.Left && ButtonClick != null)
            {
                ButtonClick(this, EventArgs.Empty);
            }
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Color bg = isInteractive ? activeBack : Color.FromArgb(26, 26, 32);
        if (isInteractive && isHovered)
        {
            int r = Math.Min(255, bg.R + 18);
            int g = Math.Min(255, bg.G + 18);
            int b = Math.Min(255, bg.B + 18);
            bg = Color.FromArgb(r, g, b);
        }
        Color fg = isInteractive ? activeFore : Color.FromArgb(145, 145, 160);
        Color bd = isInteractive ? borderColor : Color.FromArgb(52, 52, 62);

        using (SolidBrush sb = new SolidBrush(bg))
        {
            e.Graphics.FillRectangle(sb, 0, 0, this.Width, this.Height);
        }
        if (bd != Color.Empty && bd.A > 0)
        {
            using (Pen p = new Pen(bd, 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
        using (Brush tb = new SolidBrush(fg))
        {
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            e.Graphics.DrawString(buttonText, textFont, tb, new RectangleF(0, 0, this.Width, this.Height), sf);
        }
    }
}

public class ScrollTextPanel : BufferedPanel
{
    private Panel clipPanel;
    private Label lblContent;
    private BufferedPanel scrollTrack;
    private BufferedPanel scrollThumb;
    private int scrollOffset = 0;
    private int maxScroll = 0;
    private bool isDraggingThumb = false;
    private int dragStartY = 0;
    private int dragStartOffset = 0;

    public ScrollTextPanel(int width, int height)
    {
        this.Size = new Size(width, height);
        this.BackColor = Color.FromArgb(14, 14, 18);

        clipPanel = new Panel();
        clipPanel.Location = new Point(2, 2);
        clipPanel.Size = new Size(width - 16, height - 4);
        clipPanel.BackColor = Color.FromArgb(14, 14, 18);
        clipPanel.AutoScroll = false;
        this.Controls.Add(clipPanel);

        lblContent = new Label();
        lblContent.Location = new Point(10, 8);
        lblContent.AutoSize = true;
        lblContent.MaximumSize = new Size(width - 32, 0);
        lblContent.BackColor = Color.FromArgb(14, 14, 18);
        lblContent.ForeColor = Color.FromArgb(236, 236, 244);
        lblContent.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        lblContent.UseMnemonic = false;
        clipPanel.Controls.Add(lblContent);

        scrollTrack = new BufferedPanel();
        scrollTrack.Location = new Point(width - 10, 4);
        scrollTrack.Size = new Size(6, height - 8);
        scrollTrack.BackColor = Color.FromArgb(22, 22, 28);
        this.Controls.Add(scrollTrack);

        scrollThumb = new BufferedPanel();
        scrollThumb.Size = new Size(6, 30);
        scrollThumb.Location = new Point(0, 0);
        scrollThumb.BackColor = Color.FromArgb(75, 75, 88);
        scrollTrack.Controls.Add(scrollThumb);

        // Wheel events
        this.MouseWheel += HandleWheel;
        clipPanel.MouseWheel += HandleWheel;
        lblContent.MouseWheel += HandleWheel;
        scrollTrack.MouseWheel += HandleWheel;
        scrollThumb.MouseWheel += HandleWheel;

        scrollThumb.MouseEnter += (s, e) => {
            scrollThumb.BackColor = Color.FromArgb(246, 130, 31);
        };
        scrollThumb.MouseLeave += (s, e) => {
            if (!isDraggingThumb) scrollThumb.BackColor = Color.FromArgb(75, 75, 88);
        };

        scrollThumb.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Left)
            {
                isDraggingThumb = true;
                dragStartY = Cursor.Position.Y;
                dragStartOffset = scrollOffset;
                scrollThumb.BackColor = Color.FromArgb(246, 130, 31);
            }
        };

        scrollThumb.MouseMove += (s, e) => {
            if (isDraggingThumb && maxScroll > 0)
            {
                int deltaY = Cursor.Position.Y - dragStartY;
                int trackScrollable = scrollTrack.Height - scrollThumb.Height;
                if (trackScrollable > 0)
                {
                    float ratio = (float)maxScroll / trackScrollable;
                    SetScrollOffset(dragStartOffset + (int)(deltaY * ratio));
                }
            }
        };

        scrollThumb.MouseUp += (s, e) => {
            isDraggingThumb = false;
            scrollThumb.BackColor = Color.FromArgb(75, 75, 88);
        };

        scrollTrack.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Left && maxScroll > 0)
            {
                int targetThumbTop = e.Y - scrollThumb.Height / 2;
                int trackScrollable = scrollTrack.Height - scrollThumb.Height;
                if (trackScrollable > 0)
                {
                    float ratio = (float)targetThumbTop / trackScrollable;
                    SetScrollOffset((int)(ratio * maxScroll));
                }
            }
        };
    }

    private void HandleWheel(object sender, MouseEventArgs e)
    {
        if (maxScroll <= 0) return;
        int delta = -e.Delta / 2;
        SetScrollOffset(scrollOffset + delta);
    }

    public void SetScrollOffset(int offset)
    {
        scrollOffset = Math.Max(0, Math.Min(maxScroll, offset));
        lblContent.Top = 8 - scrollOffset;

        int trackScrollable = scrollTrack.Height - scrollThumb.Height;
        if (maxScroll > 0 && trackScrollable > 0)
        {
            float ratio = (float)scrollOffset / maxScroll;
            scrollThumb.Top = (int)(ratio * trackScrollable);
        }
        else
        {
            scrollThumb.Top = 0;
        }
    }

    private void UpdateScrollLayout()
    {
        int contentH = lblContent.Height + 16;
        int viewH = clipPanel.Height;
        if (contentH > viewH)
        {
            maxScroll = contentH - viewH;
            scrollTrack.Visible = true;
            float viewRatio = (float)viewH / contentH;
            int thumbH = Math.Max(24, (int)(scrollTrack.Height * viewRatio));
            scrollThumb.Height = thumbH;
            SetScrollOffset(scrollOffset);
        }
        else
        {
            maxScroll = 0;
            scrollTrack.Visible = false;
            SetScrollOffset(0);
        }
    }

    public void SetText(string text)
    {
        lblContent.Text = text;
        scrollOffset = 0;
        UpdateScrollLayout();
    }

    public void AppendLine(string line)
    {
        if (string.IsNullOrEmpty(lblContent.Text))
        {
            lblContent.Text = line;
        }
        else
        {
            lblContent.Text = lblContent.Text + Environment.NewLine + line;
        }
        UpdateScrollLayout();
        SetScrollOffset(maxScroll);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (Pen p = new Pen(Color.FromArgb(52, 52, 62), 1))
        {
            e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
        }
    }
}

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
    private BufferedPanel sidebarPanel;
    private BufferedPanel contentPanel;
    private BufferedPanel bottomPanel;

    // Sidebar Step Badges
    private BufferedPanel[] stepBadgePanels;
    private Label[] stepNumberLabels;
    private Label[] stepTitleLabels;
    private Label[] stepSubLabels;
    private int currentStep = 0;

    // Navigation Buttons
    private ModernButton btnBack;
    private ModernButton btnNext;
    private ModernButton btnCancel;
    private Label lblVersionInfo;

    // Step Pages
    private BufferedPanel panelSystemCheck;
    private BufferedPanel panelAgreement;
    private BufferedPanel panelModeAndOptions;
    private BufferedPanel panelProgress;
    private BufferedPanel panelFinish;

    // Step 0: System Check Controls
    private Label lblSysTitle;
    private Label lblSysDesc;
    private BufferedPanel[] sysCardPanels;
    private Label[] sysCardTitles;
    private Label[] sysCardValues;
    private Label[] sysCardBadges;
    private BufferedPanel pnlSysAdvisory;
    private Label lblAdvisoryTitle;
    private Label lblAdvisoryDesc;
    private ModernButton btnHowToReady;
    private ModernButton btnRefreshScan;
    private ModernButton btnRemoveExisting;
    private Label lblLastScanTime;
    private string detectedGpu = null;
    private bool hasNvidiaGpu = false;
    private string detectedPythonVersion = null;

    // Step 1: Agreement Controls
    private Label lblAgreeTitle;
    private Label lblAgreeDesc;
    private ScrollTextPanel pnlTermsScroll;
    private int agreementChoice = 0; // 0 = unselected/released, 1 = accept, 2 = decline
    private BufferedPanel pnlAgreeBox;
    private BufferedPanel pnlDeclineBox;
    private Label lblAgreementStatus;

    // Step 2: Mode & Options Controls
    private Label lblOptTitle;
    private Label lblOptDesc;
    private int installMode = 0; // 0 = Express (default), 1 = Custom
    private BufferedPanel pnlExpressCard;
    private BufferedPanel pnlCustomCard;
    private BufferedPanel pnlCustomDetails;
    private Label lblDest;
    private TextBox txtTargetDir;
    private ModernButton btnBrowseTarget;
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
    private BufferedPanel pnlPhaseTracker;
    private int currentInstallPhase = 0; // 0..3
    private BufferedPanel customProgressBar;
    private int currentProgressPercent = 0;
    private ScrollTextPanel logScrollBox;

    // Step 4: Finish Controls
    private Label lblFinishTitle;
    private Label lblFinishDesc;
    private BufferedPanel pnlFinishCard;
    private Label lblSummaryBody;
    private CheckBox chkLaunchNow;
    private CheckBox chkOpenFolder;

    // Installation Parameters
    private string targetDir;
    private string pythonBinary = null;
    private bool existingVersionDetected = false;
    private string existingInstallDir = null;
    private bool isInstalling = false;
    private bool pipSucceeded = false;

    // Colors
    private readonly Color ClrBgDark = Color.FromArgb(19, 19, 22);
    private readonly Color ClrSidebar = Color.FromArgb(13, 13, 15);
    private readonly Color ClrCardBg = Color.FromArgb(26, 26, 30);
    private readonly Color ClrCardBorder = Color.FromArgb(46, 46, 54);
    private readonly Color ClrNvidiaGreen = Color.FromArgb(118, 185, 0);
    private readonly Color ClrTextPrimary = Color.FromArgb(244, 244, 246);
    private readonly Color ClrTextSecondary = Color.FromArgb(195, 195, 205);
    private readonly Color ClrTextMuted = Color.FromArgb(140, 140, 152);
    private readonly Color ClrBorderSubtle = Color.FromArgb(36, 36, 42);

    public SpoonbillNvidiaInstaller()
    {
        this.Text = "Spoonbill AI Studio v4.1 (Stable) — Setup Wizard";
        this.ClientSize = new Size(FormWidth, FormHeight);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ClrBgDark;
        this.ForeColor = ClrTextPrimary;
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        targetDir = Path.Combine(localApp, "Programs", "Spoonbill_Studio");

        BuildTwoColumnLayout();
        SwitchStep(0);
    }

    private Label MakeLabel(string text, Font font, Color fg, Point loc, Size size)
    {
        Label l = new Label();
        l.UseMnemonic = false;
        l.Text = text;
        l.Font = font;
        l.ForeColor = fg;
        l.Location = loc;
        if (size == Size.Empty)
        {
            l.AutoSize = true;
        }
        else
        {
            l.Size = size;
        }
        return l;
    }

    private void BuildTwoColumnLayout()
    {
        // 1. Sidebar Panel (Fixed at X = 0, Y = 0, Width = 220, Height = 500)
        sidebarPanel = new BufferedPanel();
        sidebarPanel.Location = new Point(0, 0);
        sidebarPanel.Size = new Size(SidebarWidth, MainHeight);
        sidebarPanel.BackColor = ClrSidebar;
        sidebarPanel.Paint += (s, e) => {
            using (Pen p = new Pen(ClrBorderSubtle, 1))
            {
                e.Graphics.DrawLine(p, SidebarWidth - 1, 0, SidebarWidth - 1, MainHeight);
            }
        };
        this.Controls.Add(sidebarPanel);

        // Brand Emblem
        BufferedPanel emblemPanel = new BufferedPanel();
        emblemPanel.Size = new Size(34, 34);
        emblemPanel.Location = new Point(22, 22);
        emblemPanel.BackColor = Color.FromArgb(24, 36, 12);
        emblemPanel.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen p = new Pen(ClrNvidiaGreen, 1.5f))
            {
                e.Graphics.DrawRectangle(p, 1, 1, 31, 31);
            }
            using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
            {
                e.Graphics.FillRectangle(b, 9, 9, 16, 16);
            }
        };
        sidebarPanel.Controls.Add(emblemPanel);

        sidebarPanel.Controls.Add(MakeLabel("SPOONBILL AI", new Font("Segoe UI", 11F, FontStyle.Bold), Color.White, new Point(64, 20), Size.Empty));
        sidebarPanel.Controls.Add(MakeLabel("Studio Wizard v4.1", new Font("Segoe UI", 8F, FontStyle.Regular), ClrNvidiaGreen, new Point(65, 40), Size.Empty));

        string[] stepTitles = new string[] {
            "System Check",
            "User Agreement",
            "Mode & Options",
            "Deployment",
            "Ready to Use"
        };
        string[] stepSubs = new string[] {
            "Hardware & Python scan",
            "License & pip consent",
            "Express or Custom path",
            "Extract & configure AI",
            "Summary & launch"
        };

        stepBadgePanels = new BufferedPanel[stepTitles.Length];
        stepNumberLabels = new Label[stepTitles.Length];
        stepTitleLabels = new Label[stepTitles.Length];
        stepSubLabels = new Label[stepTitles.Length];

        for (int i = 0; i < stepTitles.Length; i++)
        {
            int idx = i;
            int rowY = 88 + (i * 56);

            BufferedPanel badge = new BufferedPanel();
            badge.Size = new Size(26, 26);
            badge.Location = new Point(22, rowY);
            badge.BackColor = ClrSidebar;
            badge.Cursor = Cursors.Hand;

            Label num = new Label();
            num.UseMnemonic = false;
            num.Text = (i + 1).ToString();
            num.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            num.ForeColor = ClrTextMuted;
            num.Dock = DockStyle.Fill;
            num.TextAlign = ContentAlignment.MiddleCenter;
            num.BackColor = Color.Transparent;
            num.Cursor = Cursors.Hand;
            num.Click += (s, e) => OnStepSidebarClick(idx);
            badge.Controls.Add(num);

            badge.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isCurr = (idx == currentStep);
                bool isDone = (idx < currentStep);
                Color fillClr = isCurr ? Color.FromArgb(28, 46, 14) : (isDone ? Color.FromArgb(20, 32, 12) : Color.FromArgb(24, 24, 28));
                Color borderClr = (isCurr || isDone) ? ClrNvidiaGreen : Color.FromArgb(52, 52, 60);
                using (SolidBrush sb = new SolidBrush(fillClr))
                {
                    e.Graphics.FillEllipse(sb, 1, 1, 23, 23);
                }
                using (Pen p = new Pen(borderClr, isCurr ? 1.8f : 1.2f))
                {
                    e.Graphics.DrawEllipse(p, 1, 1, 23, 23);
                }
            };
            badge.Click += (s, e) => OnStepSidebarClick(idx);
            sidebarPanel.Controls.Add(badge);

            Label lbl = MakeLabel(stepTitles[i], new Font("Segoe UI", 9.5F, FontStyle.Regular), ClrTextMuted, new Point(56, rowY - 2), new Size(150, 18));
            lbl.Cursor = Cursors.Hand;
            lbl.Click += (s, e) => OnStepSidebarClick(idx);
            sidebarPanel.Controls.Add(lbl);

            Label sub = MakeLabel(stepSubs[i], new Font("Segoe UI", 7.5F, FontStyle.Regular), Color.FromArgb(118, 118, 130), new Point(56, rowY + 16), new Size(150, 15));
            sub.Cursor = Cursors.Hand;
            sub.Click += (s, e) => OnStepSidebarClick(idx);
            sidebarPanel.Controls.Add(sub);

            stepBadgePanels[i] = badge;
            stepNumberLabels[i] = num;
            stepTitleLabels[i] = lbl;
            stepSubLabels[i] = sub;
        }

        sidebarPanel.Controls.Add(MakeLabel(
            "100% Offline Local AI\nCC BY-NC-SA 4.0 License\nNo PyTorch Bloat (~50MB deps)",
            new Font("Segoe UI", 7.5F, FontStyle.Regular),
            Color.FromArgb(135, 135, 148),
            new Point(22, 428),
            new Size(180, 52)
        ));

        // 2. Main Content Area (Fixed adjacent at X = 220, Y = 0, Width = 600, Height = 500)
        contentPanel = new BufferedPanel();
        contentPanel.Location = new Point(SidebarWidth, 0);
        contentPanel.Size = new Size(ContentWidth, MainHeight);
        contentPanel.BackColor = ClrBgDark;
        this.Controls.Add(contentPanel);

        // 3. Bottom Bar (Fixed at X = 0, Y = 500, Width = 820, Height = 60)
        bottomPanel = new BufferedPanel();
        bottomPanel.Location = new Point(0, MainHeight);
        bottomPanel.Size = new Size(FormWidth, BottomHeight);
        bottomPanel.BackColor = ClrSidebar;
        bottomPanel.Paint += (s, e) => {
            using (Pen p = new Pen(ClrBorderSubtle, 1))
            {
                e.Graphics.DrawLine(p, 0, 0, FormWidth, 0);
            }
        };
        this.Controls.Add(bottomPanel);

        lblVersionInfo = MakeLabel(
            "Spoonbill AI Studio v4.1.0 (x64 CPU Safe Mode & NVIDIA CUDA)",
            new Font("Segoe UI", 8F),
            ClrTextSecondary,
            new Point(22, 21),
            Size.Empty
        );
        bottomPanel.Controls.Add(lblVersionInfo);

        btnBack = new ModernButton();
        btnBack.ButtonText = "< Back";
        btnBack.Size = new Size(92, 34);
        btnBack.Location = new Point(496, 13);
        btnBack.ActiveBackColor = Color.FromArgb(40, 40, 48);
        btnBack.ActiveForeColor = Color.White;
        btnBack.BorderColor = Color.FromArgb(72, 72, 84);
        btnBack.TextFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        btnBack.ButtonClick += (s, e) => { if (currentStep > 0 && !isInstalling) SwitchStep(currentStep - 1); };
        bottomPanel.Controls.Add(btnBack);

        btnNext = new ModernButton();
        btnNext.ButtonText = "Next >";
        btnNext.Size = new Size(104, 34);
        btnNext.Location = new Point(596, 13);
        btnNext.ActiveBackColor = ClrNvidiaGreen;
        btnNext.ActiveForeColor = Color.Black;
        btnNext.BorderColor = ClrNvidiaGreen;
        btnNext.TextFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnNext.ButtonClick += OnNextClick;
        bottomPanel.Controls.Add(btnNext);

        btnCancel = new ModernButton();
        btnCancel.ButtonText = "Cancel";
        btnCancel.Size = new Size(90, 34);
        btnCancel.Location = new Point(708, 13);
        btnCancel.ActiveBackColor = Color.FromArgb(40, 40, 48);
        btnCancel.ActiveForeColor = ClrTextSecondary;
        btnCancel.BorderColor = Color.FromArgb(72, 72, 84);
        btnCancel.TextFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        btnCancel.ButtonClick += OnCancelClick;
        bottomPanel.Controls.Add(btnCancel);

        BuildStepPages();
    }

    private void OnStepSidebarClick(int idx)
    {
        if (!isInstalling && idx < currentStep)
        {
            SwitchStep(idx);
        }
    }

    private BufferedPanel CreateStepPage()
    {
        BufferedPanel p = new BufferedPanel();
        p.Location = new Point(0, 0);
        p.Size = new Size(ContentWidth, MainHeight);
        p.BackColor = ClrBgDark;
        p.Visible = false;
        contentPanel.Controls.Add(p);
        return p;
    }

    private void BuildStepPages()
    {
        // ================= PAGE 0: SYSTEM CHECK =================
        panelSystemCheck = CreateStepPage();

        lblSysTitle = MakeLabel("Step 1: System Compatibility & Environment Scan", new Font("Segoe UI", 13F, FontStyle.Bold), Color.White, new Point(28, 14), new Size(544, 24));
        panelSystemCheck.Controls.Add(lblSysTitle);

        lblSysDesc = MakeLabel("Checking operating system, compute cores, GPU acceleration, Python runtime, and prior builds.", new Font("Segoe UI", 8.5F), ClrTextSecondary, new Point(28, 39), new Size(544, 18));
        panelSystemCheck.Controls.Add(lblSysDesc);

        sysCardPanels = new BufferedPanel[5];
        sysCardTitles = new Label[5];
        sysCardValues = new Label[5];
        sysCardBadges = new Label[5];

        string[] cardHeaders = new string[] {
            "Operating System & Architecture",
            "Processor & Compute Cores",
            "Graphics & Acceleration Engine",
            "Python 3 AI Inference Runtime",
            "Existing Studio Installation Check"
        };

        for (int i = 0; i < 5; i++)
        {
            BufferedPanel card = new BufferedPanel();
            card.Size = new Size(544, 42);
            card.Location = new Point(28, 62 + (i * 46));
            card.BackColor = ClrCardBg;
            card.Paint += (s, e) => {
                using (Pen p = new Pen(ClrCardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            panelSystemCheck.Controls.Add(card);

            Label title = MakeLabel(cardHeaders[i], new Font("Segoe UI", 7.5F, FontStyle.Regular), Color.FromArgb(185, 185, 198), new Point(14, 4), Size.Empty);
            card.Controls.Add(title);

            Label val = MakeLabel("Scanning environment...", new Font("Segoe UI", 8.5F, FontStyle.Bold), Color.White, new Point(14, 20), new Size(388, 18));
            val.AutoEllipsis = true;
            card.Controls.Add(val);

            Label badge = MakeLabel("CHECKING", new Font("Segoe UI", 7.5F, FontStyle.Bold), ClrNvidiaGreen, new Point(410, 9), new Size(122, 24));
            badge.BackColor = Color.FromArgb(24, 38, 16);
            badge.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(badge);

            sysCardPanels[i] = card;
            sysCardTitles[i] = title;
            sysCardValues[i] = val;
            sysCardBadges[i] = badge;
        }

        // Advisory Banner for Readiness & Troubleshooting
        pnlSysAdvisory = new BufferedPanel();
        pnlSysAdvisory.Size = new Size(544, 72);
        pnlSysAdvisory.Location = new Point(28, 296);
        pnlSysAdvisory.BackColor = Color.FromArgb(22, 28, 16);
        pnlSysAdvisory.Paint += (s, e) => {
            bool pyOk = !string.IsNullOrEmpty(pythonBinary);
            Color bClr = pyOk ? ClrNvidiaGreen : Color.FromArgb(239, 68, 68);
            using (Pen p = new Pen(bClr, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlSysAdvisory.Width - 1, pnlSysAdvisory.Height - 1);
            }
        };
        panelSystemCheck.Controls.Add(pnlSysAdvisory);

        lblAdvisoryTitle = MakeLabel("System Scan Initializing...", new Font("Segoe UI", 9F, FontStyle.Bold), Color.White, new Point(14, 8), new Size(516, 18));
        lblAdvisoryTitle.BackColor = Color.Transparent;
        pnlSysAdvisory.Controls.Add(lblAdvisoryTitle);

        lblAdvisoryDesc = MakeLabel("Analyzing hardware accelerators and runtime prerequisites...", new Font("Segoe UI", 8.25F), Color.FromArgb(228, 228, 236), new Point(14, 28), new Size(516, 38));
        lblAdvisoryDesc.BackColor = Color.Transparent;
        pnlSysAdvisory.Controls.Add(lblAdvisoryDesc);

        // System Check Action Row
        btnHowToReady = new ModernButton();
        btnHowToReady.ButtonText = "How to Make Ready...";
        btnHowToReady.Location = new Point(28, 378);
        btnHowToReady.Size = new Size(176, 34);
        btnHowToReady.ActiveBackColor = Color.FromArgb(44, 44, 52);
        btnHowToReady.ActiveForeColor = Color.White;
        btnHowToReady.BorderColor = Color.FromArgb(80, 80, 92);
        btnHowToReady.ButtonClick += (s, e) => ShowTroubleshootingDialog();
        panelSystemCheck.Controls.Add(btnHowToReady);

        btnRefreshScan = new ModernButton();
        btnRefreshScan.ButtonText = "Refresh Scan";
        btnRefreshScan.Location = new Point(212, 378);
        btnRefreshScan.Size = new Size(124, 34);
        btnRefreshScan.ActiveBackColor = ClrNvidiaGreen;
        btnRefreshScan.ActiveForeColor = Color.Black;
        btnRefreshScan.BorderColor = ClrNvidiaGreen;
        btnRefreshScan.ButtonClick += (s, e) => RunSystemCheck();
        panelSystemCheck.Controls.Add(btnRefreshScan);

        btnRemoveExisting = new ModernButton();
        btnRemoveExisting.ButtonText = "Uninstall Old Build";
        btnRemoveExisting.Location = new Point(344, 378);
        btnRemoveExisting.Size = new Size(144, 34);
        btnRemoveExisting.ActiveBackColor = Color.FromArgb(56, 22, 22);
        btnRemoveExisting.ActiveForeColor = Color.FromArgb(254, 202, 202);
        btnRemoveExisting.BorderColor = Color.FromArgb(185, 60, 60);
        btnRemoveExisting.Visible = false;
        btnRemoveExisting.ButtonClick += OnQuickRemoveExistingClick;
        panelSystemCheck.Controls.Add(btnRemoveExisting);

        lblLastScanTime = MakeLabel("Tip: Click 'Refresh Scan' after installing Python or drivers.", new Font("Segoe UI", 8F), ClrTextSecondary, new Point(28, 420), new Size(544, 18));
        panelSystemCheck.Controls.Add(lblLastScanTime);

        // ================= PAGE 1: USER AGREEMENT =================
        panelAgreement = CreateStepPage();

        lblAgreeTitle = MakeLabel("Step 2: License & AI Dependencies Agreement", new Font("Segoe UI", 13F, FontStyle.Bold), Color.White, new Point(28, 14), new Size(544, 24));
        panelAgreement.Controls.Add(lblAgreeTitle);

        lblAgreeDesc = MakeLabel("Review the conservation license and pip dependency authorization. Select one option box below.", new Font("Segoe UI", 8.5F), ClrTextSecondary, new Point(28, 39), new Size(544, 18));
        panelAgreement.Controls.Add(lblAgreeDesc);

        pnlTermsScroll = new ScrollTextPanel(544, 198);
        pnlTermsScroll.Location = new Point(28, 62);
        pnlTermsScroll.SetText(
            "BLACK-FACED SPOONBILL AI STUDIO V4 — LICENSE & USAGE AGREEMENT\r\n" +
            "==============================================================\r\n\r\n" +
            "1. CONSERVATION RESEARCH LICENSE GRANT (CC BY-NC-SA 4.0)\r\n" +
            "This software is licensed under Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International. " +
            "You are granted permission to use, evaluate, and adapt this studio for non-commercial ecological research, " +
            "academic study, and Black-faced Spoonbill (Platalea minor) wildlife conservation census. Commercial use or " +
            "monetization without explicit written permission and author attribution is strictly prohibited.\r\n\r\n" +
            "2. AI DEPENDENCIES & AUTOMATED PIP EXECUTION\r\n" +
            "By accepting, you authorize the installer to verify your local Python 3 installation and execute " +
            "'pip install -r requirements_lean.txt' to install lean runtime packages (onnxruntime, fastapi, uvicorn, " +
            "opencv-python-headless, pillow, numpy, python-multipart). Total footprint is ~50 MB with zero PyTorch bloat.\r\n\r\n" +
            "3. 100% LOCAL INFERENCE & DATA PRIVACY GUARANTEE\r\n" +
            "All deep learning inference, segmentation masks, and population counts run 100% locally on your workstation. " +
            "No user imagery or telemetry is ever uploaded to external servers.\r\n\r\n" +
            "4. MANDATORY DEPENDENCY REQUIREMENT & CLEAN ABORT\r\n" +
            "Spoonbill AI Studio requires its local inference backend to operate. If you select 'I DO NOT ACCEPT', " +
            "setup will safely abort without writing any files or modifying your computer."
        );
        panelAgreement.Controls.Add(pnlTermsScroll);

        // Agreement Toggleable Choice Box 1 (Accept)
        pnlAgreeBox = new BufferedPanel();
        pnlAgreeBox.Size = new Size(544, 54);
        pnlAgreeBox.Location = new Point(28, 270);
        pnlAgreeBox.BackColor = ClrCardBg;
        pnlAgreeBox.Cursor = Cursors.Hand;
        pnlAgreeBox.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (agreementChoice == 1);
            Color bClr = isSel ? ClrNvidiaGreen : Color.FromArgb(62, 62, 72);
            Color fClr = isSel ? Color.FromArgb(24, 42, 16) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlAgreeBox.Width, pnlAgreeBox.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 2.0f : 1.0f))
            {
                e.Graphics.DrawRectangle(p, 1, 1, pnlAgreeBox.Width - 2, pnlAgreeBox.Height - 2);
            }

            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillRectangle(b, 16, 17, 20, 20);
                }
                using (Pen pCheck = new Pen(Color.FromArgb(10, 10, 12), 2.4f))
                {
                    e.Graphics.DrawLines(pCheck, new Point[] {
                        new Point(20, 27),
                        new Point(24, 32),
                        new Point(32, 21)
                    });
                }
            }
            else
            {
                using (Pen pBox = new Pen(Color.FromArgb(160, 160, 175), 1.5f))
                {
                    e.Graphics.DrawRectangle(pBox, 16, 17, 20, 20);
                }
            }

            using (Font fTitle = new Font("Segoe UI", 9.25F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.White : Color.FromArgb(242, 242, 248)))
            {
                e.Graphics.DrawString("I ACCEPT the license terms and authorize AI dependency installation", fTitle, bTitle, 46, 8);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(200, 238, 175) : Color.FromArgb(195, 195, 208)))
            {
                e.Graphics.DrawString("Enables Next button to configure Express or Custom installation options (click again to unselect).", fSub, bSub, 46, 29);
            }
        };
        pnlAgreeBox.MouseDown += (s, e) => ToggleAgreementChoice(1);
        panelAgreement.Controls.Add(pnlAgreeBox);

        // Agreement Toggleable Choice Box 2 (Decline)
        pnlDeclineBox = new BufferedPanel();
        pnlDeclineBox.Size = new Size(544, 54);
        pnlDeclineBox.Location = new Point(28, 332);
        pnlDeclineBox.BackColor = ClrCardBg;
        pnlDeclineBox.Cursor = Cursors.Hand;
        pnlDeclineBox.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (agreementChoice == 2);
            Color bClr = isSel ? Color.FromArgb(239, 68, 68) : Color.FromArgb(62, 62, 72);
            Color fClr = isSel ? Color.FromArgb(46, 18, 18) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlDeclineBox.Width, pnlDeclineBox.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 2.0f : 1.0f))
            {
                e.Graphics.DrawRectangle(p, 1, 1, pnlDeclineBox.Width - 2, pnlDeclineBox.Height - 2);
            }

            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    e.Graphics.FillRectangle(b, 16, 17, 20, 20);
                }
                using (Pen pX = new Pen(Color.White, 2.2f))
                {
                    e.Graphics.DrawLine(pX, 21, 22, 31, 32);
                    e.Graphics.DrawLine(pX, 31, 22, 21, 32);
                }
            }
            else
            {
                using (Pen pBox = new Pen(Color.FromArgb(160, 160, 175), 1.5f))
                {
                    e.Graphics.DrawRectangle(pBox, 16, 17, 20, 20);
                }
            }

            using (Font fTitle = new Font("Segoe UI", 9.25F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.FromArgb(254, 215, 215) : Color.FromArgb(242, 242, 248)))
            {
                e.Graphics.DrawString("I DO NOT ACCEPT the terms (Cancel installation and exit setup)", fTitle, bTitle, 46, 8);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(248, 185, 185) : Color.FromArgb(195, 195, 208)))
            {
                e.Graphics.DrawString("Setup will safely abort without modifying system files or installing packages (click again to unselect).", fSub, bSub, 46, 29);
            }
        };
        pnlDeclineBox.MouseDown += (s, e) => ToggleAgreementChoice(2);
        panelAgreement.Controls.Add(pnlDeclineBox);

        lblAgreementStatus = MakeLabel("Status: No option selected. Click one of the boxes above (only one can be active at a time).", new Font("Segoe UI", 8.75F, FontStyle.Bold), ClrTextSecondary, new Point(28, 396), new Size(544, 22));
        panelAgreement.Controls.Add(lblAgreementStatus);

        // ================= PAGE 2: MODE & OPTIONS =================
        panelModeAndOptions = CreateStepPage();

        lblOptTitle = MakeLabel("Step 3: Installation Mode & Target Configuration", new Font("Segoe UI", 13F, FontStyle.Bold), Color.White, new Point(28, 14), new Size(544, 24));
        panelModeAndOptions.Controls.Add(lblOptTitle);

        lblOptDesc = MakeLabel("Choose Express for automatic setup or Custom to customize paths, models, and shortcuts.", new Font("Segoe UI", 8.5F), ClrTextSecondary, new Point(28, 39), new Size(544, 18));
        panelModeAndOptions.Controls.Add(lblOptDesc);

        // Express Card (Strict Mutually Exclusive)
        pnlExpressCard = new BufferedPanel();
        pnlExpressCard.Size = new Size(264, 78);
        pnlExpressCard.Location = new Point(28, 64);
        pnlExpressCard.BackColor = ClrCardBg;
        pnlExpressCard.Cursor = Cursors.Hand;
        pnlExpressCard.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (installMode == 0);
            Color bClr = isSel ? ClrNvidiaGreen : Color.FromArgb(62, 62, 72);
            Color fClr = isSel ? Color.FromArgb(24, 42, 16) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlExpressCard.Width, pnlExpressCard.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 2.0f : 1.0f))
            {
                e.Graphics.DrawRectangle(p, 1, 1, pnlExpressCard.Width - 2, pnlExpressCard.Height - 2);
            }

            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillEllipse(b, 14, 14, 18, 18);
                }
                using (SolidBrush bDot = new SolidBrush(Color.FromArgb(10, 10, 12)))
                {
                    e.Graphics.FillEllipse(bDot, 20, 20, 6, 6);
                }
            }
            else
            {
                using (Pen pCircle = new Pen(Color.FromArgb(160, 160, 175), 1.5f))
                {
                    e.Graphics.DrawEllipse(pCircle, 14, 14, 18, 18);
                }
            }

            using (Font fTitle = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.White : Color.FromArgb(242, 242, 248)))
            {
                e.Graphics.DrawString("Express (Recommended)", fTitle, bTitle, 40, 11);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(200, 238, 175) : Color.FromArgb(195, 195, 208)))
            {
                e.Graphics.DrawString("Automated clean install with all\nONNX models, pip deps, & shortcuts.", fSub, bSub, 40, 33);
            }
        };
        pnlExpressCard.MouseDown += (s, e) => SetInstallMode(0);
        panelModeAndOptions.Controls.Add(pnlExpressCard);

        // Custom Card (Strict Mutually Exclusive)
        pnlCustomCard = new BufferedPanel();
        pnlCustomCard.Size = new Size(264, 78);
        pnlCustomCard.Location = new Point(308, 64);
        pnlCustomCard.BackColor = ClrCardBg;
        pnlCustomCard.Cursor = Cursors.Hand;
        pnlCustomCard.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool isSel = (installMode == 1);
            Color bClr = isSel ? ClrNvidiaGreen : Color.FromArgb(62, 62, 72);
            Color fClr = isSel ? Color.FromArgb(24, 42, 16) : ClrCardBg;

            using (SolidBrush sb = new SolidBrush(fClr))
            {
                e.Graphics.FillRectangle(sb, 0, 0, pnlCustomCard.Width, pnlCustomCard.Height);
            }
            using (Pen p = new Pen(bClr, isSel ? 2.0f : 1.0f))
            {
                e.Graphics.DrawRectangle(p, 1, 1, pnlCustomCard.Width - 2, pnlCustomCard.Height - 2);
            }

            if (isSel)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillEllipse(b, 14, 14, 18, 18);
                }
                using (SolidBrush bDot = new SolidBrush(Color.FromArgb(10, 10, 12)))
                {
                    e.Graphics.FillEllipse(bDot, 20, 20, 6, 6);
                }
            }
            else
            {
                using (Pen pCircle = new Pen(Color.FromArgb(160, 160, 175), 1.5f))
                {
                    e.Graphics.DrawEllipse(pCircle, 14, 14, 18, 18);
                }
            }

            using (Font fTitle = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Brush bTitle = new SolidBrush(isSel ? Color.White : Color.FromArgb(242, 242, 248)))
            {
                e.Graphics.DrawString("Custom (Advanced)", fTitle, bTitle, 40, 11);
            }
            using (Font fSub = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush bSub = new SolidBrush(isSel ? Color.FromArgb(200, 238, 175) : Color.FromArgb(195, 195, 208)))
            {
                e.Graphics.DrawString("Select custom installation folder,\nmodel weights, & shortcut options.", fSub, bSub, 40, 33);
            }
        };
        pnlCustomCard.MouseDown += (s, e) => SetInstallMode(1);
        panelModeAndOptions.Controls.Add(pnlCustomCard);

        // Options Details Panel
        pnlCustomDetails = new BufferedPanel();
        pnlCustomDetails.Location = new Point(28, 152);
        pnlCustomDetails.Size = new Size(544, 260);
        pnlCustomDetails.BackColor = Color.FromArgb(23, 23, 27);
        pnlCustomDetails.Paint += (s, e) => {
            using (Pen p = new Pen(ClrCardBorder, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlCustomDetails.Width - 1, pnlCustomDetails.Height - 1);
            }
        };
        panelModeAndOptions.Controls.Add(pnlCustomDetails);

        lblDest = MakeLabel("Target Installation Directory (Express Default — Locked):", new Font("Segoe UI", 8.5F, FontStyle.Bold), Color.FromArgb(235, 235, 245), new Point(16, 10), new Size(400, 18));
        pnlCustomDetails.Controls.Add(lblDest);

        txtTargetDir = new TextBox();
        txtTargetDir.Text = targetDir;
        txtTargetDir.BackColor = Color.FromArgb(16, 16, 20);
        txtTargetDir.ForeColor = Color.White;
        txtTargetDir.BorderStyle = BorderStyle.FixedSingle;
        txtTargetDir.Font = new Font("Segoe UI", 9F);
        txtTargetDir.Location = new Point(16, 30);
        txtTargetDir.Size = new Size(414, 24);
        txtTargetDir.TextChanged += (s, e) => { targetDir = txtTargetDir.Text; UpdateDiskSpace(); };
        pnlCustomDetails.Controls.Add(txtTargetDir);

        btnBrowseTarget = new ModernButton();
        btnBrowseTarget.ButtonText = "Browse...";
        btnBrowseTarget.Size = new Size(94, 24);
        btnBrowseTarget.Location = new Point(436, 30);
        btnBrowseTarget.ActiveBackColor = Color.FromArgb(48, 48, 58);
        btnBrowseTarget.ActiveForeColor = Color.White;
        btnBrowseTarget.BorderColor = Color.FromArgb(82, 82, 94);
        btnBrowseTarget.TextFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        btnBrowseTarget.ButtonClick += OnBrowseTargetClick;
        pnlCustomDetails.Controls.Add(btnBrowseTarget);

        lblDiskSpace = MakeLabel("Required space: ~550 MB | Available space: Calculating...", new Font("Segoe UI", 8F), Color.FromArgb(195, 195, 210), new Point(16, 58), new Size(510, 16));
        pnlCustomDetails.Controls.Add(lblDiskSpace);

        chkCleanInstall = new CheckBox();
        chkCleanInstall.UseMnemonic = false;
        chkCleanInstall.Text = "Perform Clean Upgrade / Installation (Safely purge prior build files first)";
        chkCleanInstall.Checked = true;
        chkCleanInstall.ForeColor = Color.FromArgb(251, 146, 60);
        chkCleanInstall.Font = new Font("Segoe UI", 8.75F, FontStyle.Bold);
        chkCleanInstall.Location = new Point(16, 82);
        chkCleanInstall.Size = new Size(510, 22);
        pnlCustomDetails.Controls.Add(chkCleanInstall);

        chkInstallModels = new CheckBox();
        chkInstallModels.UseMnemonic = false;
        chkInstallModels.Text = "Include High-Precision Large ONNX Models (YOLO11-L & BeakAttn + Nano)";
        chkInstallModels.Checked = true;
        chkInstallModels.ForeColor = Color.FromArgb(240, 240, 245);
        chkInstallModels.Font = new Font("Segoe UI", 8.75F);
        chkInstallModels.Location = new Point(16, 108);
        chkInstallModels.Size = new Size(510, 22);
        chkInstallModels.CheckedChanged += (s, e) => UpdateDiskSpace();
        pnlCustomDetails.Controls.Add(chkInstallModels);

        chkInstallDeps = new CheckBox();
        chkInstallDeps.UseMnemonic = false;
        chkInstallDeps.Text = "Install AI Runtime Dependencies via pip (onnxruntime, fastapi, uvicorn, opencv)";
        chkInstallDeps.Checked = true;
        chkInstallDeps.ForeColor = Color.FromArgb(240, 240, 245);
        chkInstallDeps.Font = new Font("Segoe UI", 8.75F);
        chkInstallDeps.Location = new Point(16, 134);
        chkInstallDeps.Size = new Size(510, 22);
        chkInstallDeps.CheckedChanged += (s, e) => UpdateDiskSpace();
        pnlCustomDetails.Controls.Add(chkInstallDeps);

        chkDesktopShortcut = new CheckBox();
        chkDesktopShortcut.UseMnemonic = false;
        chkDesktopShortcut.Text = "Create Desktop Shortcut";
        chkDesktopShortcut.Checked = true;
        chkDesktopShortcut.ForeColor = Color.FromArgb(240, 240, 245);
        chkDesktopShortcut.Font = new Font("Segoe UI", 8.75F);
        chkDesktopShortcut.Location = new Point(16, 162);
        chkDesktopShortcut.Size = new Size(230, 22);
        pnlCustomDetails.Controls.Add(chkDesktopShortcut);

        chkStartMenuShortcut = new CheckBox();
        chkStartMenuShortcut.UseMnemonic = false;
        chkStartMenuShortcut.Text = "Create Start Menu Shortcut";
        chkStartMenuShortcut.Checked = true;
        chkStartMenuShortcut.ForeColor = Color.FromArgb(240, 240, 245);
        chkStartMenuShortcut.Font = new Font("Segoe UI", 8.75F);
        chkStartMenuShortcut.Location = new Point(264, 162);
        chkStartMenuShortcut.Size = new Size(240, 22);
        pnlCustomDetails.Controls.Add(chkStartMenuShortcut);

        chkRegisterUninstaller = new CheckBox();
        chkRegisterUninstaller.UseMnemonic = false;
        chkRegisterUninstaller.Text = "Register Uninstaller in Windows Installed Apps (Settings / Control Panel)";
        chkRegisterUninstaller.Checked = true;
        chkRegisterUninstaller.ForeColor = Color.FromArgb(240, 240, 245);
        chkRegisterUninstaller.Font = new Font("Segoe UI", 8.75F);
        chkRegisterUninstaller.Location = new Point(16, 190);
        chkRegisterUninstaller.Size = new Size(510, 22);
        pnlCustomDetails.Controls.Add(chkRegisterUninstaller);

        pnlCustomDetails.Controls.Add(MakeLabel(
            "Note: In Express mode, all components above are pre-selected and locked for reliability.",
            new Font("Segoe UI", 7.75F, FontStyle.Italic),
            Color.FromArgb(165, 165, 180),
            new Point(16, 224),
            new Size(510, 18)
        ));

        ToggleCustomMode(false);
        UpdateDiskSpace();

        // ================= PAGE 3: PROGRESS =================
        panelProgress = CreateStepPage();

        lblProgTitle = MakeLabel("Step 4: Deploying Spoonbill AI Studio V4...", new Font("Segoe UI", 13F, FontStyle.Bold), Color.White, new Point(28, 14), new Size(544, 24));
        panelProgress.Controls.Add(lblProgTitle);

        lblProgStatus = MakeLabel("Preparing installation environment...", new Font("Segoe UI", 8.75F), ClrTextSecondary, new Point(28, 40), new Size(544, 18));
        panelProgress.Controls.Add(lblProgStatus);

        // 4-Phase Visual Progress Tracker
        pnlPhaseTracker = new BufferedPanel();
        pnlPhaseTracker.Location = new Point(28, 64);
        pnlPhaseTracker.Size = new Size(544, 34);
        pnlPhaseTracker.BackColor = ClrCardBg;
        pnlPhaseTracker.Paint += (s, e) => {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (Pen p = new Pen(ClrCardBorder, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlPhaseTracker.Width - 1, pnlPhaseTracker.Height - 1);
            }
            string[] phases = new string[] {
                "1. Clean & Prepare",
                "2. Extract Payload",
                "3. AI Dependencies",
                "4. System Shortcuts"
            };
            int segW = pnlPhaseTracker.Width / 4;
            for (int i = 0; i < 4; i++)
            {
                bool active = (i == currentInstallPhase);
                bool done = (i < currentInstallPhase);
                Color bg = active ? Color.FromArgb(26, 44, 14) : (done ? Color.FromArgb(18, 30, 12) : ClrCardBg);
                Color fg = active ? Color.White : (done ? ClrNvidiaGreen : Color.FromArgb(145, 145, 158));
                using (SolidBrush sb = new SolidBrush(bg))
                {
                    e.Graphics.FillRectangle(sb, i * segW + 1, 1, segW - 2, pnlPhaseTracker.Height - 2);
                }
                using (Font f = new Font("Segoe UI", 8F, (active || done) ? FontStyle.Bold : FontStyle.Regular))
                using (Brush b = new SolidBrush(fg))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString(phases[i], f, b, new RectangleF(i * segW, 0, segW, pnlPhaseTracker.Height), sf);
                }
            }
        };
        panelProgress.Controls.Add(pnlPhaseTracker);

        // Custom Sleek Modern Progress Bar
        customProgressBar = new BufferedPanel();
        customProgressBar.Size = new Size(544, 12);
        customProgressBar.Location = new Point(28, 106);
        customProgressBar.BackColor = Color.FromArgb(32, 32, 38);
        customProgressBar.Paint += (s, e) => {
            int fillW = (int)((currentProgressPercent / 100.0) * customProgressBar.Width);
            if (fillW > 0)
            {
                using (SolidBrush b = new SolidBrush(ClrNvidiaGreen))
                {
                    e.Graphics.FillRectangle(b, 0, 0, fillW, customProgressBar.Height);
                }
            }
            using (Pen p = new Pen(ClrCardBorder, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, customProgressBar.Width - 1, customProgressBar.Height - 1);
            }
        };
        panelProgress.Controls.Add(customProgressBar);

        logScrollBox = new ScrollTextPanel(544, 292);
        logScrollBox.Location = new Point(28, 126);
        panelProgress.Controls.Add(logScrollBox);

        // ================= PAGE 4: FINISH =================
        panelFinish = CreateStepPage();

        lblFinishTitle = MakeLabel("Step 5: Installation Complete", new Font("Segoe UI", 13.5F, FontStyle.Bold), Color.White, new Point(28, 18), new Size(544, 28));
        panelFinish.Controls.Add(lblFinishTitle);

        lblFinishDesc = MakeLabel("Spoonbill AI Studio V4 has been deployed and configured on your workstation.", new Font("Segoe UI", 9F), ClrTextSecondary, new Point(28, 48), new Size(544, 22));
        panelFinish.Controls.Add(lblFinishDesc);

        pnlFinishCard = new BufferedPanel();
        pnlFinishCard.Size = new Size(544, 172);
        pnlFinishCard.Location = new Point(28, 82);
        pnlFinishCard.BackColor = ClrCardBg;
        pnlFinishCard.Paint += (s, e) => {
            using (Pen p = new Pen(ClrNvidiaGreen, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, pnlFinishCard.Width - 1, pnlFinishCard.Height - 1);
            }
        };
        panelFinish.Controls.Add(pnlFinishCard);

        pnlFinishCard.Controls.Add(MakeLabel("DEPLOYMENT VERIFICATION SUMMARY", new Font("Segoe UI", 8.5F, FontStyle.Bold), ClrNvidiaGreen, new Point(18, 14), Size.Empty));

        lblSummaryBody = MakeLabel(
            "• Target Directory  : " + targetDir + "\n" +
            "• Studio Executable : Spoonbill_Studio.exe (Verified)\n" +
            "• Neural Engine     : CPU Safe Mode (ONNX Runtime 11N & 11L)\n" +
            "• Windows Control   : Uninstaller registered in Settings > Installed Apps\n" +
            "• Data Privacy      : 100% offline local inference active",
            new Font("Segoe UI", 9F, FontStyle.Regular),
            ClrTextPrimary,
            new Point(18, 38),
            new Size(508, 120)
        );
        pnlFinishCard.Controls.Add(lblSummaryBody);

        chkLaunchNow = new CheckBox();
        chkLaunchNow.UseMnemonic = false;
        chkLaunchNow.Text = "Launch Spoonbill AI Studio V4 now";
        chkLaunchNow.Checked = true;
        chkLaunchNow.ForeColor = ClrNvidiaGreen;
        chkLaunchNow.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        chkLaunchNow.Location = new Point(28, 272);
        chkLaunchNow.Size = new Size(420, 26);
        panelFinish.Controls.Add(chkLaunchNow);

        chkOpenFolder = new CheckBox();
        chkOpenFolder.UseMnemonic = false;
        chkOpenFolder.Text = "Open installation directory in Windows Explorer";
        chkOpenFolder.Checked = false;
        chkOpenFolder.ForeColor = ClrTextSecondary;
        chkOpenFolder.Font = new Font("Segoe UI", 9F);
        chkOpenFolder.Location = new Point(28, 304);
        chkOpenFolder.Size = new Size(420, 24);
        panelFinish.Controls.Add(chkOpenFolder);
    }

    private void SetInstallMode(int mode)
    {
        installMode = mode;
        pnlExpressCard.Invalidate();
        pnlCustomCard.Invalidate();
        ToggleCustomMode(installMode == 1);
        UpdateDiskSpace();
    }

    private void ToggleCustomMode(bool customEnabled)
    {
        txtTargetDir.ReadOnly = !customEnabled;
        txtTargetDir.BackColor = customEnabled ? Color.FromArgb(20, 20, 26) : Color.FromArgb(15, 15, 19);
        txtTargetDir.ForeColor = customEnabled ? Color.White : Color.FromArgb(210, 210, 222);

        btnBrowseTarget.IsInteractive = customEnabled;

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

            chkCleanInstall.ForeColor = Color.FromArgb(251, 146, 60);
            chkInstallModels.ForeColor = Color.FromArgb(225, 225, 235);
            chkInstallDeps.ForeColor = Color.FromArgb(225, 225, 235);
            chkDesktopShortcut.ForeColor = Color.FromArgb(225, 225, 235);
            chkStartMenuShortcut.ForeColor = Color.FromArgb(225, 225, 235);
            chkRegisterUninstaller.ForeColor = Color.FromArgb(225, 225, 235);
            lblDest.Text = "Target Installation Directory (Express Default — Locked):";
        }
        else
        {
            chkCleanInstall.ForeColor = Color.FromArgb(251, 146, 60);
            chkInstallModels.ForeColor = Color.White;
            chkInstallDeps.ForeColor = Color.White;
            chkDesktopShortcut.ForeColor = Color.White;
            chkStartMenuShortcut.ForeColor = Color.White;
            chkRegisterUninstaller.ForeColor = Color.White;
            lblDest.Text = "Target Installation Directory (Custom — Editable):";
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
        int reqMb = 350;
        if (installMode == 0 || (chkInstallModels != null && chkInstallModels.Checked)) reqMb += 150;
        if (installMode == 0 || (chkInstallDeps != null && chkInstallDeps.Checked)) reqMb += 50;

        try
        {
            string root = Path.GetPathRoot(targetDir);
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                DriveInfo d = new DriveInfo(root);
                long freeMb = d.AvailableFreeSpace / (1024 * 1024);
                lblDiskSpace.Text = "Required space: ~" + reqMb + " MB  |  Available on drive (" + root.TrimEnd('\\') + "): " + freeMb.ToString("N0") + " MB";
                return;
            }
        }
        catch {}

        if (lblDiskSpace != null)
        {
            lblDiskSpace.Text = "Required space: ~" + reqMb + " MB";
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
                stepNumberLabels[i].Text = (i + 1).ToString();
                stepNumberLabels[i].ForeColor = ClrNvidiaGreen;
                stepTitleLabels[i].ForeColor = Color.White;
                stepTitleLabels[i].Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                stepSubLabels[i].ForeColor = Color.FromArgb(190, 215, 165);
            }
            else if (i < step)
            {
                stepNumberLabels[i].Text = "✓";
                stepNumberLabels[i].ForeColor = ClrNvidiaGreen;
                stepTitleLabels[i].ForeColor = Color.FromArgb(220, 220, 228);
                stepTitleLabels[i].Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                stepSubLabels[i].ForeColor = Color.FromArgb(145, 145, 158);
            }
            else
            {
                stepNumberLabels[i].Text = (i + 1).ToString();
                stepNumberLabels[i].ForeColor = ClrTextMuted;
                stepTitleLabels[i].ForeColor = ClrTextMuted;
                stepTitleLabels[i].Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                stepSubLabels[i].ForeColor = Color.FromArgb(105, 105, 115);
            }
        }

        panelSystemCheck.Visible = (step == 0);
        panelAgreement.Visible = (step == 1);
        panelModeAndOptions.Visible = (step == 2);
        panelProgress.Visible = (step == 3);
        panelFinish.Visible = (step == 4);

        if (step == 0)
        {
            panelSystemCheck.BringToFront();
            btnBack.Visible = true;
            btnCancel.Visible = true;
            btnBack.IsInteractive = false;
            btnNext.ButtonText = "Next >";
            btnNext.ActiveBackColor = ClrNvidiaGreen;
            btnNext.ActiveForeColor = Color.Black;
            btnNext.BorderColor = ClrNvidiaGreen;
            btnNext.IsInteractive = true;
            RunSystemCheck();
        }
        else if (step == 1)
        {
            panelAgreement.BringToFront();
            btnBack.Visible = true;
            btnCancel.Visible = true;
            btnBack.IsInteractive = true;
            UpdateAgreementUI();
        }
        else if (step == 2)
        {
            panelModeAndOptions.BringToFront();
            btnBack.Visible = true;
            btnCancel.Visible = true;
            btnBack.IsInteractive = true;
            btnNext.ButtonText = "Install Now";
            btnNext.ActiveBackColor = ClrNvidiaGreen;
            btnNext.ActiveForeColor = Color.Black;
            btnNext.BorderColor = ClrNvidiaGreen;
            btnNext.IsInteractive = true;
            UpdateDiskSpace();
        }
        else if (step == 3)
        {
            panelProgress.BringToFront();
            btnBack.IsInteractive = false;
            btnNext.IsInteractive = false;
            btnCancel.IsInteractive = false;
            isInstalling = true;
            ThreadPool.QueueUserWorkItem(RunInstallation);
        }
        else if (step == 4)
        {
            panelFinish.BringToFront();
            UpdateFinishSummary();
            btnBack.Visible = false;
            btnCancel.Visible = false;
            btnNext.ButtonText = "Finish";
            btnNext.ActiveBackColor = ClrNvidiaGreen;
            btnNext.ActiveForeColor = Color.Black;
            btnNext.BorderColor = ClrNvidiaGreen;
            btnNext.IsInteractive = true;
        }
    }

    private void UpdateFinishSummary()
    {
        if (lblSummaryBody == null) return;
        string gpuLine = hasNvidiaGpu
            ? "NVIDIA CUDA Ready (" + detectedGpu + ") + CPU Safe Mode"
            : "CPU Safe Mode Active (" + (detectedGpu ?? "Standard Display") + ")";
        string pyLine = pipSucceeded
            ? "Verified & Installed (" + (detectedPythonVersion ?? "Python 3") + ")"
            : (!string.IsNullOrEmpty(pythonBinary) ? "Python Found (" + (detectedPythonVersion ?? "Python 3") + ")" : "Pending Python 3 Installation");
        lblSummaryBody.Text =
            "• Installed Location : " + targetDir + "\n" +
            "• Studio Executable  : Spoonbill_Studio.exe (Ready)\n" +
            "• Compute Backend    : " + gpuLine + "\n" +
            "• AI Dependencies    : " + pyLine + "\n" +
            "• Windows Control    : Uninstaller registered in Installed Apps";
    }

    private void RunSystemCheck()
    {
        RefreshEnvironmentPath();

        // 1. Operating System & Architecture
        sysCardValues[0].Text = Environment.OSVersion.ToString() + (Environment.Is64BitOperatingSystem ? " (64-bit)" : " (32-bit)");
        sysCardBadges[0].Text = "COMPATIBLE";
        sysCardBadges[0].ForeColor = ClrNvidiaGreen;
        sysCardBadges[0].BackColor = Color.FromArgb(24, 38, 16);

        // 2. Hardware Resources
        int cores = Environment.ProcessorCount;
        sysCardValues[1].Text = cores + " Logical CPU Cores available for ONNX inference";
        sysCardBadges[1].Text = "READY";
        sysCardBadges[1].ForeColor = ClrNvidiaGreen;
        sysCardBadges[1].BackColor = Color.FromArgb(24, 38, 16);

        // 3. Graphics & Hardware Acceleration
        detectedGpu = DetectGraphicsCard(out hasNvidiaGpu);
        if (hasNvidiaGpu)
        {
            sysCardValues[2].Text = detectedGpu + " (NVIDIA CUDA & CPU Safe Mode)";
            sysCardBadges[2].Text = "NVIDIA GPU";
            sysCardBadges[2].ForeColor = ClrNvidiaGreen;
            sysCardBadges[2].BackColor = Color.FromArgb(24, 38, 16);
        }
        else
        {
            sysCardValues[2].Text = detectedGpu + " (Runs in CPU Safe Mode)";
            sysCardBadges[2].Text = "CPU SAFE MODE";
            sysCardBadges[2].ForeColor = Color.FromArgb(56, 189, 248);
            sysCardBadges[2].BackColor = Color.FromArgb(16, 34, 52);
        }

        // 4. Python Environment
        pythonBinary = FindPythonBinary(out detectedPythonVersion);
        bool pythonReady = !string.IsNullOrEmpty(pythonBinary);
        if (pythonReady)
        {
            sysCardValues[3].Text = (detectedPythonVersion ?? "Python 3") + " (" + pythonBinary + ")";
            sysCardBadges[3].Text = "READY";
            sysCardBadges[3].ForeColor = ClrNvidiaGreen;
            sysCardBadges[3].BackColor = Color.FromArgb(24, 38, 16);
        }
        else
        {
            sysCardValues[3].Text = "Python 3.8+ not detected on PATH (Click 'How to Make Ready...')";
            sysCardBadges[3].Text = "ACTION NEEDED";
            sysCardBadges[3].ForeColor = Color.FromArgb(248, 113, 113);
            sysCardBadges[3].BackColor = Color.FromArgb(52, 18, 18);
        }

        // 5. Prior Installation Detection
        existingInstallDir = DetectExistingInstallation();
        existingVersionDetected = !string.IsNullOrEmpty(existingInstallDir);
        if (existingVersionDetected)
        {
            sysCardValues[4].Text = "Detected (" + FormatCompactPath(existingInstallDir) + ")";
            sysCardBadges[4].Text = "UPGRADE READY";
            sysCardBadges[4].ForeColor = Color.FromArgb(251, 146, 60);
            sysCardBadges[4].BackColor = Color.FromArgb(46, 28, 12);
            btnRemoveExisting.Visible = true;
        }
        else
        {
            sysCardValues[4].Text = "No prior installation found (Clean target directory)";
            sysCardBadges[4].Text = "CLEAN TARGET";
            sysCardBadges[4].ForeColor = ClrNvidiaGreen;
            sysCardBadges[4].BackColor = Color.FromArgb(24, 38, 16);
            btnRemoveExisting.Visible = false;
        }

        // Update Advisory Banner
        if (!pythonReady)
        {
            lblAdvisoryTitle.Text = "Action Needed: Python 3 Runtime Not Detected";
            lblAdvisoryTitle.ForeColor = Color.FromArgb(252, 129, 129);
            lblAdvisoryDesc.Text = "Click 'How to Make Ready...' below to auto-install Python 3.11 via winget or download from python.org, then click 'Refresh Scan'.";
            pnlSysAdvisory.BackColor = Color.FromArgb(36, 18, 18);
            btnHowToReady.ButtonText = "How to Make Ready...";
            btnHowToReady.ActiveBackColor = Color.FromArgb(68, 24, 24);
            btnHowToReady.ActiveForeColor = Color.FromArgb(254, 215, 215);
            btnHowToReady.BorderColor = Color.FromArgb(185, 60, 60);
        }
        else
        {
            lblAdvisoryTitle.Text = "All System Prerequisites Ready for Deployment";
            lblAdvisoryTitle.ForeColor = ClrNvidiaGreen;
            string upgradeNote = existingVersionDetected
                ? " Prior build detected and will be cleanly upgraded."
                : " Ready for Express or Custom installation.";
            lblAdvisoryDesc.Text = "Hardware and Python runtime verified." + upgradeNote + " Click 'Next >' to review the License Agreement.";
            pnlSysAdvisory.BackColor = Color.FromArgb(18, 32, 14);
            btnHowToReady.ButtonText = "Setup & Hardware Guide";
            btnHowToReady.ActiveBackColor = Color.FromArgb(28, 44, 18);
            btnHowToReady.ActiveForeColor = ClrNvidiaGreen;
            btnHowToReady.BorderColor = ClrNvidiaGreen;
        }
        pnlSysAdvisory.Invalidate();

        lblLastScanTime.Text = "Last scanned at " + DateTime.Now.ToString("HH:mm:ss") + " — Click 'Refresh Scan' anytime after making system changes.";
    }

    private string FormatCompactPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return "";
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localApp) && fullPath.StartsWith(localApp, StringComparison.OrdinalIgnoreCase))
        {
            return "%LOCALAPPDATA%" + fullPath.Substring(localApp.Length);
        }
        return fullPath;
    }

    private string DetectExistingInstallation()
    {
        try
        {
            if (Directory.Exists(targetDir) && File.Exists(Path.Combine(targetDir, "Spoonbill_Studio.exe")))
            {
                return targetDir;
            }
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio"))
            {
                if (key != null)
                {
                    object loc = key.GetValue("InstallLocation");
                    if (loc != null && Directory.Exists(loc.ToString()))
                    {
                        return loc.ToString();
                    }
                }
            }
        }
        catch {}
        return null;
    }

    private void OnQuickRemoveExistingClick(object sender, EventArgs e)
    {
        string dirToClean = existingInstallDir ?? targetDir;
        DialogResult res = MessageBox.Show(
            this,
            "An existing Spoonbill AI Studio installation was detected at:\n" + dirToClean + "\n\n" +
            "Would you like to completely remove the previous installation and its shortcuts right now?\n" +
            "(Note: Continuing with normal setup will also automatically perform a Clean Upgrade.)",
            "Uninstall Existing Build",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );
        if (res == DialogResult.Yes)
        {
            try
            {
                KillRunningProcesses();
                if (Directory.Exists(dirToClean))
                {
                    foreach (string file in Directory.GetFiles(dirToClean, "*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); } catch {}
                    }
                    try { Directory.Delete(dirToClean, true); } catch {}
                }
                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Spoonbill AI Studio.lnk");
                if (File.Exists(desktopLnk)) { try { File.Delete(desktopLnk); } catch {} }
                string startMenuLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Spoonbill AI Studio.lnk");
                if (File.Exists(startMenuLnk)) { try { File.Delete(startMenuLnk); } catch {} }
                try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio", false); } catch {}

                MessageBox.Show(this, "Previous installation and shortcuts have been cleanly removed.", "Removed Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RunSystemCheck();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not remove some files: " + ex.Message, "Removal Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RunSystemCheck();
            }
        }
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
                    "Spoonbill AI Studio requires Python 3.8 - 3.12 to run its local neural network backend.\n\n" +
                    "Would you like to open the Setup & Readiness Guide to install Python now?\n\n" +
                    "• Click 'Yes' to open the Readiness Guide (Recommended).\n" +
                    "• Click 'No' to continue to the License Agreement anyway.",
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
                    "Please click one of the option boxes on this page to indicate whether you accept or decline the agreement.\n\n" +
                    "Note: Only one box can be active at a time, and clicking an active box again will release your selection.",
                    "Selection Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }
            if (agreementChoice == 2)
            {
                DialogResult res = MessageBox.Show(
                    this,
                    "You have selected 'I DO NOT ACCEPT'.\n\n" +
                    "Because Spoonbill AI Studio cannot function without its local AI runtime dependencies, the installation will now be cancelled.\n\n" +
                    "Confirm cancellation and exit setup?",
                    "Cancel Installation?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (res == DialogResult.Yes)
                {
                    MessageBox.Show(
                        this,
                        "Installation has been cancelled.\nNo files or settings were modified on your computer.",
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
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                MessageBox.Show(this, "Please specify a valid target installation folder.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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
            MessageBox.Show(this, "Deployment is currently in progress. Please wait for the current operation to finish.", "Setup Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult res = MessageBox.Show(this, "Are you sure you want to cancel setup and exit?\nNo changes will be finalized.", "Exit Setup Wizard", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            Application.Exit();
        }
    }

    private void SetInstallPhase(int phase)
    {
        currentInstallPhase = phase;
        if (this.IsHandleCreated && pnlPhaseTracker != null)
        {
            this.Invoke((MethodInvoker)delegate {
                pnlPhaseTracker.Invalidate();
            });
        }
    }

    private void RunInstallation(object state)
    {
        try
        {
            // PHASE 0: Clean & Prepare
            SetInstallPhase(0);
            UpdateStatus("Phase 1/4: Preparing target directory: " + targetDir, 5);
            AppendLog("=== SPOONBILL AI STUDIO V4 DEPLOYMENT STARTED ===");
            AppendLog("Mode: " + (installMode == 0 ? "Express (Recommended)" : "Custom (Advanced)"));
            AppendLog("Target Directory: " + targetDir);

            bool doClean = (installMode == 0) || chkCleanInstall.Checked;
            if (doClean && Directory.Exists(targetDir))
            {
                UpdateStatus("Phase 1/4: Stopping active studio processes and purging old build...", 10);
                AppendLog("Stopping any running Spoonbill_Studio processes...");
                KillRunningProcesses();

                AppendLog("Purging previous installation files in target directory...");
                int removedCount = 0;
                try
                {
                    foreach (string file in Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                            removedCount++;
                        }
                        catch {}
                    }
                    foreach (string dir in Directory.GetDirectories(targetDir))
                    {
                        try { Directory.Delete(dir, true); } catch {}
                    }
                    AppendLog("Clean purge complete (" + removedCount + " prior files removed).");
                }
                catch (Exception cex)
                {
                    AppendLog("[Notice] Partial cleanup note: " + cex.Message);
                }
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // PHASE 1: Safe Entry-by-Entry Payload Extraction
            SetInstallPhase(1);
            UpdateStatus("Phase 2/4: Extracting core studio binaries, UI, and ONNX models...", 18);
            AppendLog("Locating embedded distribution archive...");

            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("Payload"))
            {
                if (s == null)
                {
                    throw new Exception("Embedded installation payload resource ('Payload') was not found in this binary.");
                }
                string tempZip = Path.Combine(Path.GetTempPath(), "spoonbill_setup_" + Guid.NewGuid().ToString("N") + ".zip");
                try
                {
                    using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                    {
                        s.CopyTo(fs);
                    }
                    AppendLog("Payload staged. Extracting files with safe overwrite handling...");

                    using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                    {
                        int totalEntries = Math.Max(1, archive.Entries.Count);
                        int processed = 0;
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            processed++;
                            string fullDestPath = Path.Combine(targetDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                if (!Directory.Exists(fullDestPath)) Directory.CreateDirectory(fullDestPath);
                                continue;
                            }

                            string parentDir = Path.GetDirectoryName(fullDestPath);
                            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                            {
                                Directory.CreateDirectory(parentDir);
                            }

                            if (File.Exists(fullDestPath))
                            {
                                try { File.SetAttributes(fullDestPath, FileAttributes.Normal); } catch {}
                            }

                            entry.ExtractToFile(fullDestPath, true);

                            if (processed % 15 == 0 || processed == totalEntries)
                            {
                                int pct = 18 + (int)((processed / (double)totalEntries) * 37.0);
                                UpdateStatus("Phase 2/4: Extracting files (" + processed + "/" + totalEntries + ")...", pct);
                            }
                        }
                    }
                }
                finally
                {
                    if (File.Exists(tempZip))
                    {
                        try { File.Delete(tempZip); } catch {}
                    }
                }
            }
            AppendLog("All application binaries and assets extracted successfully.");

            // Check selective large models option in Custom Mode
            bool keepLargeModels = (installMode == 0) || chkInstallModels.Checked;
            if (!keepLargeModels)
            {
                AppendLog("Custom option: Removing optional large ONNX weights (keeping Nano model for CPU Safe Mode)...");
                string[] optionalWeights = new string[] { "best_spoonbill_11l.onnx", "yolo11l-seg.onnx", "yolo11m-seg.onnx", "beakattn-l.onnx" };
                foreach (string wName in optionalWeights)
                {
                    foreach (string found in Directory.GetFiles(targetDir, wName, SearchOption.AllDirectories))
                    {
                        try { File.Delete(found); AppendLog("  Removed optional weight: " + Path.GetFileName(found)); } catch {}
                    }
                }
            }

            SetProgress(58);

            // PHASE 2: AI Dependencies via pip
            SetInstallPhase(2);
            bool doDeps = (installMode == 0) || chkInstallDeps.Checked;
            if (doDeps)
            {
                UpdateStatus("Phase 3/4: Verifying Python 3 runtime and installing AI dependencies...", 62);
                RefreshEnvironmentPath();
                string pyVersion;
                string pyCmd = FindPythonBinary(out pyVersion);
                if (string.IsNullOrEmpty(pyCmd))
                {
                    AppendLog("[WARNING] Python 3.8+ was not found on system PATH.");
                    AppendLog("Studio UI is installed, but please install Python 3.11 from python.org (with 'Add to PATH') before running inference.");
                }
                else
                {
                    AppendLog("Using Python runtime: " + (pyVersion ?? "Python 3") + " [" + pyCmd + "]");
                    string reqFile = Path.Combine(targetDir, "requirements_lean.txt");
                    if (!File.Exists(reqFile))
                    {
                        reqFile = Path.Combine(targetDir, "resources", "app", "requirements_lean.txt");
                    }

                    if (File.Exists(reqFile))
                    {
                        UpdateStatus("Phase 3/4: Installing lean AI libraries via pip (onnxruntime, fastapi, uvicorn)...", 68);
                        bool ok = RunPipInstall(pyCmd, reqFile, false);
                        if (!ok)
                        {
                            AppendLog("Retrying pip installation with '--user' scope...");
                            ok = RunPipInstall(pyCmd, reqFile, true);
                        }
                        pipSucceeded = ok;
                    }
                    else
                    {
                        AppendLog("[Notice] requirements_lean.txt not found; skipping pip step.");
                    }
                }
            }
            else
            {
                AppendLog("Custom option: Skipping automated pip dependency installation.");
            }

            SetProgress(84);

            // PHASE 3: Shortcuts & Uninstaller Registration
            SetInstallPhase(3);
            bool doDesktop = (installMode == 0) || chkDesktopShortcut.Checked;
            bool doStart = (installMode == 0) || chkStartMenuShortcut.Checked;
            if (doDesktop || doStart)
            {
                UpdateStatus("Phase 4/4: Creating Desktop and Start Menu shortcuts...", 88);
                AppendLog("Creating Windows shortcuts...");
                CreateShortcuts(targetDir, doDesktop, doStart);
            }

            bool doUninst = (installMode == 0) || chkRegisterUninstaller.Checked;
            if (doUninst)
            {
                UpdateStatus("Phase 4/4: Registering Windows Uninstaller...", 95);
                AppendLog("Registering uninstaller in Windows Installed Apps...");
                RegisterUninstaller(targetDir);
            }

            SetProgress(100);
            UpdateStatus("Deployment complete!", 100);
            AppendLog("=== ALL SELECTED COMPONENTS DEPLOYED SUCCESSFULLY ===");

            Thread.Sleep(500);
            this.Invoke((MethodInvoker)delegate {
                isInstalling = false;
                SwitchStep(4);
            });
        }
        catch (Exception ex)
        {
            AppendLog("[FATAL ERROR] " + ex.Message);
            this.Invoke((MethodInvoker)delegate {
                isInstalling = false;
                MessageBox.Show(this, "An error occurred during installation:\n\n" + ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnBack.IsInteractive = true;
                btnCancel.IsInteractive = true;
            });
        }
    }

    private bool RunPipInstall(string pyCmd, string reqFile, bool userFlag)
    {
        try
        {
            string args = "-m pip install --disable-pip-version-check --no-input " + (userFlag ? "--user " : "") + "-r \"" + reqFile + "\"";
            AppendLog("Executing: " + pyCmd + " " + args);

            ProcessStartInfo pipPsi = new ProcessStartInfo();
            pipPsi.FileName = pyCmd;
            pipPsi.Arguments = args;
            pipPsi.WorkingDirectory = targetDir;
            pipPsi.UseShellExecute = false;
            pipPsi.CreateNoWindow = true;
            pipPsi.RedirectStandardOutput = true;
            pipPsi.RedirectStandardError = true;
            pipPsi.RedirectStandardInput = true;

            using (Process pipProc = Process.Start(pipPsi))
            {
                try { pipProc.StandardInput.Close(); } catch {}
                pipProc.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) AppendLog("  " + e.Data);
                };
                pipProc.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) AppendLog("  " + e.Data);
                };
                pipProc.BeginOutputReadLine();
                pipProc.BeginErrorReadLine();

                bool exited = pipProc.WaitForExit(180000);
                if (!exited)
                {
                    try { pipProc.Kill(); } catch {}
                    AppendLog("[WARNING] pip command timed out after 180 seconds.");
                    return false;
                }
                if (pipProc.ExitCode == 0)
                {
                    AppendLog("[SUCCESS] AI dependencies verified and installed.");
                    return true;
                }
                else
                {
                    AppendLog("[Notice] pip exited with code " + pipProc.ExitCode + ".");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            AppendLog("[Notice] pip execution error: " + ex.Message);
            return false;
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
                logScrollBox.AppendLine("[" + stamp + "] " + message);
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

    public static void RefreshEnvironmentPath()
    {
        try
        {
            string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            string combined = machinePath + ";" + userPath;
            Environment.SetEnvironmentVariable("PATH", combined, EnvironmentVariableTarget.Process);
        }
        catch {}
    }

    private string FindPythonBinary(out string versionText)
    {
        versionText = null;
        string[] candidates = new string[] { "python.exe", "py.exe", "python3.exe" };
        foreach (string cmd in candidates)
        {
            string ver;
            if (TestPythonCandidate(cmd, out ver))
            {
                versionText = ver;
                return cmd;
            }
        }

        try
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string[] searchRoots = new string[] {
                Path.Combine(localApp, "Programs", "Python"),
                progFiles,
                @"C:\",
                userProfile
            };

            foreach (string root in searchRoots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string dir in Directory.GetDirectories(root, "Python*"))
                {
                    string p = Path.Combine(dir, "python.exe");
                    string ver;
                    if (File.Exists(p) && TestPythonCandidate(p, out ver))
                    {
                        versionText = ver;
                        return p;
                    }
                }
            }

            string[] condaPaths = new string[] {
                Path.Combine(userProfile, "miniconda3", "python.exe"),
                Path.Combine(userProfile, "anaconda3", "python.exe"),
                @"C:\ProgramData\miniconda3\python.exe",
                @"C:\ProgramData\anaconda3\python.exe"
            };
            foreach (string cp in condaPaths)
            {
                string ver;
                if (File.Exists(cp) && TestPythonCandidate(cp, out ver))
                {
                    versionText = ver;
                    return cp;
                }
            }
        }
        catch {}

        return null;
    }

    private bool TestPythonCandidate(string binary, out string versionOutput)
    {
        versionOutput = null;
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(binary, "--version");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            using (Process p = Process.Start(psi))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(2500);
                string combined = (stdout + " " + stderr).Trim();
                if (p.ExitCode == 0 && combined.IndexOf("Python 3.", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    versionOutput = combined.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    return true;
                }
            }
        }
        catch {}
        return false;
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
                desktopLink.Description = "Black-Faced Spoonbill AI Studio V4";
                desktopLink.Save();
            }

            if (startMenu)
            {
                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                dynamic startMenuLink = shell.CreateShortcut(Path.Combine(startMenuDir, "Spoonbill AI Studio.lnk"));
                startMenuLink.TargetPath = exePath;
                startMenuLink.WorkingDirectory = targetDir;
                startMenuLink.Description = "Black-Faced Spoonbill AI Studio V4";
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
                    key.SetValue("DisplayName", "Black-Faced Spoonbill AI Studio v4.1");
                    key.SetValue("DisplayVersion", "4.1.0");
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
            agreementChoice = 0; // Click again releases selection
        }
        else
        {
            agreementChoice = choice; // Mutually exclusive selection
        }
        UpdateAgreementUI();
    }

    private void UpdateAgreementUI()
    {
        pnlAgreeBox.Invalidate();
        pnlDeclineBox.Invalidate();

        if (agreementChoice == 0)
        {
            lblAgreementStatus.Text = "Status: No option selected. Click one of the boxes above (click again to release).";
            lblAgreementStatus.ForeColor = ClrTextSecondary;
            if (currentStep == 1)
            {
                btnNext.IsInteractive = false;
                btnNext.ButtonText = "Next >";
            }
        }
        else if (agreementChoice == 1)
        {
            lblAgreementStatus.Text = "Status: License ACCEPTED. Click 'Next >' to configure Express or Custom options.";
            lblAgreementStatus.ForeColor = ClrNvidiaGreen;
            if (currentStep == 1)
            {
                btnNext.IsInteractive = true;
                btnNext.ButtonText = "Next >";
                btnNext.ActiveBackColor = ClrNvidiaGreen;
                btnNext.ActiveForeColor = Color.Black;
                btnNext.BorderColor = ClrNvidiaGreen;
            }
        }
        else if (agreementChoice == 2)
        {
            lblAgreementStatus.Text = "Status: Agreement DECLINED. Click 'Cancel Setup' to exit without modifying files.";
            lblAgreementStatus.ForeColor = Color.FromArgb(252, 129, 129);
            if (currentStep == 1)
            {
                btnNext.IsInteractive = true;
                btnNext.ButtonText = "Cancel Setup";
                btnNext.ActiveBackColor = Color.FromArgb(220, 38, 38);
                btnNext.ActiveForeColor = Color.White;
                btnNext.BorderColor = Color.FromArgb(248, 113, 113);
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

    public void RunVisualVerification(string outDir)
    {
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        this.Show();
        Application.DoEvents();
        Thread.Sleep(200);

        // Step 0: System Check
        SwitchStep(0);
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step0_system_check.png"));

        // Step 1: Agreement Unselected
        SwitchStep(1);
        agreementChoice = 0;
        UpdateAgreementUI();
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step1_agreement_unselected.png"));

        // Step 1: Agreement Accepted
        ToggleAgreementChoice(1);
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step1_agreement_accepted.png"));

        // Step 1: Agreement Declined (verify mutual exclusivity)
        ToggleAgreementChoice(2);
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step1_agreement_declined.png"));

        // Step 1: Click again to release, then Accept
        ToggleAgreementChoice(2);
        ToggleAgreementChoice(1);
        Application.DoEvents();

        // Step 2: Mode & Options (Express)
        SwitchStep(2);
        SetInstallMode(0);
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step2_options_express.png"));

        // Step 2: Mode & Options (Custom)
        SetInstallMode(1);
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step2_options_custom.png"));

        // Step 3: Progress UI Preview
        currentStep = 3;
        panelSystemCheck.Visible = false;
        panelAgreement.Visible = false;
        panelModeAndOptions.Visible = false;
        panelFinish.Visible = false;
        panelProgress.Visible = true;
        panelProgress.BringToFront();
        btnBack.IsInteractive = false;
        btnNext.IsInteractive = false;
        btnCancel.IsInteractive = false;
        currentInstallPhase = 1;
        currentProgressPercent = 45;
        lblProgStatus.Text = "Phase 2/4: Extracting files (42/95)...";
        logScrollBox.SetText(
            "[19:30:01] === SPOONBILL AI STUDIO V4 DEPLOYMENT STARTED ===\r\n" +
            "[19:30:01] Mode: Express (Recommended)\r\n" +
            "[19:30:01] Target Directory: " + targetDir + "\r\n" +
            "[19:30:02] Stopping any running Spoonbill_Studio processes...\r\n" +
            "[19:30:02] Clean purge complete (prior files removed).\r\n" +
            "[19:30:03] Payload staged. Extracting files with safe overwrite handling...\r\n" +
            "[19:30:05] Extracted Spoonbill_Studio.exe, UI assets, and ONNX neural weights.\r\n" +
            "[19:30:06] Using Python runtime: Python 3.12.5 [python.exe]\r\n" +
            "[19:30:07] Executing: python.exe -m pip install -r requirements_lean.txt"
        );
        pnlPhaseTracker.Invalidate();
        customProgressBar.Invalidate();
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step3_progress.png"));

        // Step 4: Finish UI Preview
        SwitchStep(4);
        Application.DoEvents();
        CaptureWindowBitmap(Path.Combine(outDir, "step4_finish.png"));

        this.Close();
    }

    private void CaptureWindowBitmap(string filePath)
    {
        this.Refresh();
        Application.DoEvents();
        using (Bitmap bmp = new Bitmap(this.Width, this.Height))
        {
            this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
            bmp.Save(filePath, ImageFormat.Png);
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        SpoonbillNvidiaInstaller form = new SpoonbillNvidiaInstaller();
        if (args != null && args.Length >= 2 && args[0] == "--verify-ui")
        {
            form.RunVisualVerification(args[1]);
            return;
        }
        Application.Run(form);
    }
}

public class SystemReadinessDialog : Form
{
    private Label lblAutoStatus;
    private ModernButton btnAutoInstallPy;

    public SystemReadinessDialog(bool pythonMissing, bool hasNvidia)
    {
        this.Text = "Spoonbill AI Studio — System Setup & Readiness Guide";
        this.ClientSize = new Size(680, 530);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(19, 19, 22);
        this.ForeColor = Color.FromArgb(244, 244, 245);
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        // Header Panel
        BufferedPanel pnlHeader = new BufferedPanel();
        pnlHeader.Location = new Point(0, 0);
        pnlHeader.Size = new Size(680, 60);
        pnlHeader.BackColor = Color.FromArgb(13, 13, 15);
        pnlHeader.Paint += (s, e) => {
            using (Pen p = new Pen(Color.FromArgb(36, 36, 42), 1))
            {
                e.Graphics.DrawLine(p, 0, 59, 680, 59);
            }
        };
        this.Controls.Add(pnlHeader);

        Label lblTitle = new Label();
        lblTitle.UseMnemonic = false;
        lblTitle.Text = "SYSTEM READINESS & DEPENDENCY ASSISTANT";
        lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(24, 12);
        lblTitle.AutoSize = true;
        pnlHeader.Controls.Add(lblTitle);

        Label lblSub = new Label();
        lblSub.UseMnemonic = false;
        lblSub.Text = "One-click tools and instructions to make all hardware and Python requirements ready.";
        lblSub.Font = new Font("Segoe UI", 8.5F);
        lblSub.ForeColor = Color.FromArgb(118, 185, 0);
        lblSub.Location = new Point(25, 34);
        lblSub.AutoSize = true;
        pnlHeader.Controls.Add(lblSub);

        // Card 1: Python AI Runtime Setup
        BufferedPanel cardPython = new BufferedPanel();
        cardPython.Location = new Point(24, 74);
        cardPython.Size = new Size(632, 196);
        cardPython.BackColor = Color.FromArgb(26, 26, 30);
        cardPython.Paint += (s, e) => {
            Color border = pythonMissing ? Color.FromArgb(239, 68, 68) : Color.FromArgb(118, 185, 0);
            using (Pen p = new Pen(border, 1.2f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, cardPython.Width - 1, cardPython.Height - 1);
            }
        };
        this.Controls.Add(cardPython);

        Label lblPyHead = new Label();
        lblPyHead.UseMnemonic = false;
        lblPyHead.Text = pythonMissing
            ? "1. Python 3 AI Runtime — ACTION NEEDED (Not Detected on PATH)"
            : "1. Python 3 AI Runtime — READY & DETECTED";
        lblPyHead.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblPyHead.ForeColor = pythonMissing ? Color.FromArgb(252, 129, 129) : Color.FromArgb(118, 185, 0);
        lblPyHead.Location = new Point(16, 12);
        lblPyHead.AutoSize = true;
        cardPython.Controls.Add(lblPyHead);

        Label lblPyBody = new Label();
        lblPyBody.UseMnemonic = false;
        lblPyBody.Text = "Spoonbill AI Studio uses Python 3.8 - 3.12 (64-bit) to run its local ONNX Runtime inference server.\n\n" +
                         "• Option A (One-Click Automated Install via Windows Package Manager):\n" +
                         "  Click 'Auto-Install Python 3.11 Now' below to automatically download and configure Python 3.11.\n" +
                         "• Option B (Official Installer from python.org):\n" +
                         "  Click 'Download from python.org' and check 'Add python.exe to PATH' on the first screen.";
        lblPyBody.Font = new Font("Segoe UI", 8.5F);
        lblPyBody.ForeColor = Color.FromArgb(225, 225, 232);
        lblPyBody.Location = new Point(16, 34);
        lblPyBody.Size = new Size(600, 96);
        cardPython.Controls.Add(lblPyBody);

        btnAutoInstallPy = new ModernButton();
        btnAutoInstallPy.ButtonText = "Auto-Install Python 3.11 Now";
        btnAutoInstallPy.Location = new Point(16, 134);
        btnAutoInstallPy.Size = new Size(206, 30);
        btnAutoInstallPy.ActiveBackColor = Color.FromArgb(118, 185, 0);
        btnAutoInstallPy.ActiveForeColor = Color.Black;
        btnAutoInstallPy.BorderColor = Color.FromArgb(118, 185, 0);
        btnAutoInstallPy.ButtonClick += OnAutoInstallPythonClick;
        cardPython.Controls.Add(btnAutoInstallPy);

        ModernButton btnPyWeb = new ModernButton();
        btnPyWeb.ButtonText = "Download from python.org";
        btnPyWeb.Location = new Point(232, 134);
        btnPyWeb.Size = new Size(190, 30);
        btnPyWeb.ActiveBackColor = Color.FromArgb(42, 42, 50);
        btnPyWeb.ActiveForeColor = Color.White;
        btnPyWeb.BorderColor = Color.FromArgb(80, 80, 92);
        btnPyWeb.ButtonClick += (s, e) => {
            try { Process.Start("https://www.python.org/downloads/windows/"); } catch {}
        };
        cardPython.Controls.Add(btnPyWeb);

        ModernButton btnCopyWinget = new ModernButton();
        btnCopyWinget.ButtonText = "Copy winget Command";
        btnCopyWinget.Location = new Point(432, 134);
        btnCopyWinget.Size = new Size(180, 30);
        btnCopyWinget.ActiveBackColor = Color.FromArgb(42, 42, 50);
        btnCopyWinget.ActiveForeColor = Color.White;
        btnCopyWinget.BorderColor = Color.FromArgb(80, 80, 92);
        btnCopyWinget.ButtonClick += (s, e) => {
            try {
                Clipboard.SetText("winget install -e --id Python.Python.3.11");
                MessageBox.Show(this, "Command copied:\nwinget install -e --id Python.Python.3.11\n\nPaste into PowerShell or Command Prompt.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch {}
        };
        cardPython.Controls.Add(btnCopyWinget);

        lblAutoStatus = new Label();
        lblAutoStatus.UseMnemonic = false;
        lblAutoStatus.Text = "Status: Ready to run automated setup or manual download.";
        lblAutoStatus.Font = new Font("Segoe UI", 8F);
        lblAutoStatus.ForeColor = Color.FromArgb(185, 185, 198);
        lblAutoStatus.Location = new Point(16, 170);
        lblAutoStatus.Size = new Size(596, 18);
        cardPython.Controls.Add(lblAutoStatus);

        // Card 2: GPU Acceleration & CUDA
        BufferedPanel cardGpu = new BufferedPanel();
        cardGpu.Location = new Point(24, 282);
        cardGpu.Size = new Size(632, 132);
        cardGpu.BackColor = Color.FromArgb(26, 26, 30);
        cardGpu.Paint += (s, e) => {
            using (Pen p = new Pen(Color.FromArgb(46, 46, 54), 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, cardGpu.Width - 1, cardGpu.Height - 1);
            }
        };
        this.Controls.Add(cardGpu);

        Label lblGpuHead = new Label();
        lblGpuHead.UseMnemonic = false;
        lblGpuHead.Text = "2. Graphics Acceleration & NVIDIA CUDA (Optional)";
        lblGpuHead.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblGpuHead.ForeColor = Color.FromArgb(56, 189, 248);
        lblGpuHead.Location = new Point(16, 12);
        lblGpuHead.AutoSize = true;
        cardGpu.Controls.Add(lblGpuHead);

        Label lblGpuBody = new Label();
        lblGpuBody.UseMnemonic = false;
        lblGpuBody.Text = "• CPU Safe Mode works out-of-the-box on all computers (Intel, AMD, and laptops) with zero GPU setup.\n" +
                         "• Optional NVIDIA GPU Acceleration unlocks high-throughput batch processing for Large models.\n" +
                         "  Requires up-to-date NVIDIA Game Ready / Studio Drivers.";
        lblGpuBody.Font = new Font("Segoe UI", 8.5F);
        lblGpuBody.ForeColor = Color.FromArgb(225, 225, 232);
        lblGpuBody.Location = new Point(16, 34);
        lblGpuBody.Size = new Size(600, 54);
        cardGpu.Controls.Add(lblGpuBody);

        ModernButton btnNvidiaWeb = new ModernButton();
        btnNvidiaWeb.ButtonText = "NVIDIA Driver Downloads";
        btnNvidiaWeb.Location = new Point(16, 92);
        btnNvidiaWeb.Size = new Size(175, 28);
        btnNvidiaWeb.ActiveBackColor = Color.FromArgb(42, 42, 50);
        btnNvidiaWeb.ActiveForeColor = Color.White;
        btnNvidiaWeb.BorderColor = Color.FromArgb(80, 80, 92);
        btnNvidiaWeb.ButtonClick += (s, e) => {
            try { Process.Start("https://www.nvidia.com/Download/index.aspx"); } catch {}
        };
        cardGpu.Controls.Add(btnNvidiaWeb);

        ModernButton btnCudaWeb = new ModernButton();
        btnCudaWeb.ButtonText = "CUDA Toolkit Archive";
        btnCudaWeb.Location = new Point(201, 92);
        btnCudaWeb.Size = new Size(165, 28);
        btnCudaWeb.ActiveBackColor = Color.FromArgb(42, 42, 50);
        btnCudaWeb.ActiveForeColor = Color.White;
        btnCudaWeb.BorderColor = Color.FromArgb(80, 80, 92);
        btnCudaWeb.ButtonClick += (s, e) => {
            try { Process.Start("https://developer.nvidia.com/cuda-downloads"); } catch {}
        };
        cardGpu.Controls.Add(btnCudaWeb);

        Label lblRefreshNote = new Label();
        lblRefreshNote.UseMnemonic = false;
        lblRefreshNote.Text = "When finished, click 'Close & Rescan System' below — the installer will automatically refresh your system PATH.";
        lblRefreshNote.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblRefreshNote.ForeColor = Color.FromArgb(190, 190, 205);
        lblRefreshNote.Location = new Point(24, 424);
        lblRefreshNote.Size = new Size(632, 24);
        this.Controls.Add(lblRefreshNote);

        // Bottom Bar
        BufferedPanel pnlBottom = new BufferedPanel();
        pnlBottom.Location = new Point(0, 464);
        pnlBottom.Size = new Size(680, 66);
        pnlBottom.BackColor = Color.FromArgb(13, 13, 15);
        pnlBottom.Paint += (s, e) => {
            using (Pen p = new Pen(Color.FromArgb(36, 36, 42), 1))
            {
                e.Graphics.DrawLine(p, 0, 0, 680, 0);
            }
        };
        this.Controls.Add(pnlBottom);

        ModernButton btnClose = new ModernButton();
        btnClose.ButtonText = "Close & Rescan System";
        btnClose.Location = new Point(486, 16);
        btnClose.Size = new Size(170, 34);
        btnClose.ActiveBackColor = Color.FromArgb(118, 185, 0);
        btnClose.ActiveForeColor = Color.Black;
        btnClose.BorderColor = Color.FromArgb(118, 185, 0);
        btnClose.ButtonClick += (s, e) => this.Close();
        pnlBottom.Controls.Add(btnClose);
    }

    private void OnAutoInstallPythonClick(object sender, EventArgs e)
    {
        try
        {
            btnAutoInstallPy.IsInteractive = false;
            lblAutoStatus.Text = "Launching Windows Package Manager (winget) to install Python 3.11...";
            lblAutoStatus.ForeColor = Color.FromArgb(118, 185, 0);
            Application.DoEvents();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c echo Installing Python 3.11 via winget... && winget install -e --id Python.Python.3.11 --accept-package-agreements --accept-source-agreements && echo. && echo Done! Closing in 3 seconds... && timeout /t 3";
            psi.UseShellExecute = true;
            using (Process p = Process.Start(psi))
            {
                if (p != null)
                {
                    p.WaitForExit();
                }
            }
            SpoonbillNvidiaInstaller.RefreshEnvironmentPath();
            lblAutoStatus.Text = "Automated installer finished! Click 'Close & Rescan System' below to verify.";
            btnAutoInstallPy.IsInteractive = true;
        }
        catch (Exception ex)
        {
            lblAutoStatus.Text = "Could not launch winget: " + ex.Message + ". Please use 'Download from python.org'.";
            lblAutoStatus.ForeColor = Color.FromArgb(248, 113, 113);
            btnAutoInstallPy.IsInteractive = true;
        }
    }
}
