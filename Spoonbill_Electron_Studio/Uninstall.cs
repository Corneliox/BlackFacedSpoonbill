using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Security.Principal;
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
    private Panel innerScroll;
    private Label lblContent;
    private Label bottomAnchor;

    public ScrollTextPanel(int width, int height)
    {
        this.Size = new Size(width, height);
        this.BackColor = Color.FromArgb(14, 14, 18);

        innerScroll = new Panel();
        innerScroll.Location = new Point(2, 2);
        innerScroll.Size = new Size(width - 4, height - 4);
        innerScroll.BackColor = Color.FromArgb(14, 14, 18);
        innerScroll.AutoScroll = true;
        this.Controls.Add(innerScroll);

        lblContent = new Label();
        lblContent.Location = new Point(10, 8);
        lblContent.AutoSize = true;
        lblContent.MaximumSize = new Size(width - 36, 0);
        lblContent.BackColor = Color.FromArgb(14, 14, 18);
        lblContent.ForeColor = Color.FromArgb(236, 236, 244);
        lblContent.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        lblContent.UseMnemonic = false;
        innerScroll.Controls.Add(lblContent);

        bottomAnchor = new Label();
        bottomAnchor.Size = new Size(1, 4);
        bottomAnchor.Location = new Point(10, 20);
        innerScroll.Controls.Add(bottomAnchor);
    }

    public void SetText(string text)
    {
        lblContent.Text = text;
        bottomAnchor.Location = new Point(10, lblContent.Bottom + 4);
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
        bottomAnchor.Location = new Point(10, lblContent.Bottom + 4);
        try
        {
            innerScroll.ScrollControlIntoView(bottomAnchor);
        }
        catch {}
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

public class SpoonbillUninstaller : Form
{
    private const int FormWidth = 680;
    private const int FormHeight = 420;
    private const int SidebarWidth = 200;
    private const int ContentWidth = 480;
    private const int MainHeight = 362;
    private const int BottomHeight = 58;

    private BufferedPanel sidebarPanel;
    private BufferedPanel contentPanel;
    private BufferedPanel bottomPanel;

    private Label[] stepLabels;
    private int currentStep = 0;

    // Content controls
    private Label titleHeader;
    private Label descLabel;
    private BufferedPanel targetCard;
    private Label lblTargetValue;
    private BufferedPanel customProgressBar;
    private int currentProgressPercent = 0;
    private ScrollTextPanel logScrollBox;
    private ModernButton btnAction;
    private ModernButton btnCancel;

    private string targetDir;

    private readonly Color ClrBgDark = Color.FromArgb(19, 19, 22);
    private readonly Color ClrSidebar = Color.FromArgb(13, 13, 15);
    private readonly Color ClrCardBg = Color.FromArgb(26, 26, 30);
    private readonly Color ClrCardBorder = Color.FromArgb(46, 46, 54);
    private readonly Color ClrNvidiaGreen = Color.FromArgb(118, 185, 0);
    private readonly Color ClrTextPrimary = Color.FromArgb(244, 244, 246);
    private readonly Color ClrTextSecondary = Color.FromArgb(195, 195, 205);

    public SpoonbillUninstaller(string installDir)
    {
        this.targetDir = installDir;
        this.Text = "Spoonbill AI Studio V4 — Uninstaller";
        this.ClientSize = new Size(FormWidth, FormHeight);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ClrBgDark;
        this.ForeColor = ClrTextPrimary;
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        this.DoubleBuffered = true;

        InitializeLayout();
        ShowStep(0);
    }

    private void InitializeLayout()
    {
        // 1. Sidebar (Explicit Left Column: X = 0, Width = 200)
        sidebarPanel = new BufferedPanel();
        sidebarPanel.Location = new Point(0, 0);
        sidebarPanel.Size = new Size(SidebarWidth, MainHeight);
        sidebarPanel.BackColor = ClrSidebar;
        sidebarPanel.Paint += (s, e) => {
            using (Pen p = new Pen(Color.FromArgb(36, 36, 42), 1))
            {
                e.Graphics.DrawLine(p, SidebarWidth - 1, 0, SidebarWidth - 1, MainHeight);
            }
        };
        this.Controls.Add(sidebarPanel);

        Label brandTitle = new Label();
        brandTitle.UseMnemonic = false;
        brandTitle.Text = "SPOONBILL AI";
        brandTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
        brandTitle.ForeColor = ClrNvidiaGreen;
        brandTitle.Location = new Point(22, 22);
        brandTitle.AutoSize = true;
        sidebarPanel.Controls.Add(brandTitle);

        Label brandSub = new Label();
        brandSub.UseMnemonic = false;
        brandSub.Text = "Studio Uninstaller V4";
        brandSub.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        brandSub.ForeColor = ClrTextSecondary;
        brandSub.Location = new Point(23, 44);
        brandSub.AutoSize = true;
        sidebarPanel.Controls.Add(brandSub);

        string[] steps = new string[] {
            "1. Confirm Removal",
            "2. Uninstallation",
            "3. Complete"
        };
        stepLabels = new Label[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            Label lbl = new Label();
            lbl.UseMnemonic = false;
            lbl.Text = steps[i];
            lbl.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(140, 140, 152);
            lbl.Location = new Point(22, 96 + (i * 38));
            lbl.Size = new Size(165, 24);
            sidebarPanel.Controls.Add(lbl);
            stepLabels[i] = lbl;
        }

        // 2. Main Content Panel (Explicit Right Column: X = 200, Width = 480)
        contentPanel = new BufferedPanel();
        contentPanel.Location = new Point(SidebarWidth, 0);
        contentPanel.Size = new Size(ContentWidth, MainHeight);
        contentPanel.BackColor = ClrBgDark;
        this.Controls.Add(contentPanel);

        titleHeader = new Label();
        titleHeader.UseMnemonic = false;
        titleHeader.Text = "Uninstall Spoonbill AI Studio";
        titleHeader.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        titleHeader.ForeColor = Color.White;
        titleHeader.Location = new Point(26, 20);
        titleHeader.Size = new Size(428, 28);
        contentPanel.Controls.Add(titleHeader);

        descLabel = new Label();
        descLabel.UseMnemonic = false;
        descLabel.Text = "This wizard will cleanly remove Spoonbill AI Studio V4, shortcuts, and registry entries from your computer.";
        descLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        descLabel.ForeColor = ClrTextSecondary;
        descLabel.Location = new Point(26, 52);
        descLabel.Size = new Size(428, 40);
        contentPanel.Controls.Add(descLabel);

        targetCard = new BufferedPanel();
        targetCard.Location = new Point(26, 100);
        targetCard.Size = new Size(428, 66);
        targetCard.BackColor = ClrCardBg;
        targetCard.Paint += (s, e) => {
            using (Pen p = new Pen(ClrCardBorder, 1))
            {
                e.Graphics.DrawRectangle(p, 0, 0, targetCard.Width - 1, targetCard.Height - 1);
            }
        };
        contentPanel.Controls.Add(targetCard);

        Label lblTargetHead = new Label();
        lblTargetHead.UseMnemonic = false;
        lblTargetHead.Text = "Target Installation Directory to Remove:";
        lblTargetHead.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblTargetHead.ForeColor = ClrNvidiaGreen;
        lblTargetHead.Location = new Point(14, 10);
        lblTargetHead.AutoSize = true;
        targetCard.Controls.Add(lblTargetHead);

        lblTargetValue = new Label();
        lblTargetValue.UseMnemonic = false;
        lblTargetValue.Text = targetDir;
        lblTargetValue.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        lblTargetValue.ForeColor = Color.White;
        lblTargetValue.Location = new Point(14, 32);
        lblTargetValue.Size = new Size(400, 22);
        lblTargetValue.AutoEllipsis = true;
        targetCard.Controls.Add(lblTargetValue);

        customProgressBar = new BufferedPanel();
        customProgressBar.Location = new Point(26, 104);
        customProgressBar.Size = new Size(428, 12);
        customProgressBar.BackColor = Color.FromArgb(32, 32, 38);
        customProgressBar.Visible = false;
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
        contentPanel.Controls.Add(customProgressBar);

        logScrollBox = new ScrollTextPanel(428, 210);
        logScrollBox.Location = new Point(26, 126);
        logScrollBox.Visible = false;
        contentPanel.Controls.Add(logScrollBox);

        // 3. Bottom Navigation Bar (Explicit Bottom Bar: Y = 362, Height = 58)
        bottomPanel = new BufferedPanel();
        bottomPanel.Location = new Point(0, MainHeight);
        bottomPanel.Size = new Size(FormWidth, BottomHeight);
        bottomPanel.BackColor = ClrSidebar;
        bottomPanel.Paint += (s, e) => {
            using (Pen p = new Pen(Color.FromArgb(36, 36, 42), 1))
            {
                e.Graphics.DrawLine(p, 0, 0, FormWidth, 0);
            }
        };
        this.Controls.Add(bottomPanel);

        btnAction = new ModernButton();
        btnAction.ButtonText = "Uninstall";
        btnAction.Size = new Size(106, 34);
        btnAction.Location = new Point(450, 12);
        btnAction.ActiveBackColor = Color.FromArgb(220, 38, 38);
        btnAction.ActiveForeColor = Color.White;
        btnAction.BorderColor = Color.FromArgb(248, 113, 113);
        btnAction.ButtonClick += OnActionClick;
        bottomPanel.Controls.Add(btnAction);

        btnCancel = new ModernButton();
        btnCancel.ButtonText = "Cancel";
        btnCancel.Size = new Size(92, 34);
        btnCancel.Location = new Point(564, 12);
        btnCancel.ActiveBackColor = Color.FromArgb(40, 40, 48);
        btnCancel.ActiveForeColor = ClrTextSecondary;
        btnCancel.BorderColor = Color.FromArgb(72, 72, 84);
        btnCancel.ButtonClick += (s, e) => this.Close();
        bottomPanel.Controls.Add(btnCancel);
    }

    private void ShowStep(int step)
    {
        currentStep = step;
        for (int i = 0; i < stepLabels.Length; i++)
        {
            if (i == step)
            {
                stepLabels[i].ForeColor = ClrNvidiaGreen;
                stepLabels[i].Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }
            else if (i < step)
            {
                stepLabels[i].ForeColor = Color.White;
                stepLabels[i].Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            }
            else
            {
                stepLabels[i].ForeColor = Color.FromArgb(135, 135, 148);
                stepLabels[i].Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            }
        }

        if (step == 0)
        {
            titleHeader.Text = "Confirm Removal";
            descLabel.Text = "Are you sure you want to completely remove Spoonbill AI Studio V4 and its shortcuts from your computer?";
            targetCard.Visible = true;
            customProgressBar.Visible = false;
            logScrollBox.Visible = false;
            btnAction.ButtonText = "Uninstall";
            btnAction.ActiveBackColor = Color.FromArgb(220, 38, 38);
            btnAction.ActiveForeColor = Color.White;
            btnAction.BorderColor = Color.FromArgb(248, 113, 113);
            btnAction.IsInteractive = true;
            btnCancel.IsInteractive = true;
        }
        else if (step == 1)
        {
            titleHeader.Text = "Uninstalling Spoonbill AI Studio...";
            descLabel.Text = "Stopping active processes and removing files, shortcuts, and registry keys.";
            targetCard.Visible = false;
            customProgressBar.Visible = true;
            currentProgressPercent = 10;
            customProgressBar.Invalidate();
            logScrollBox.Visible = true;
            btnAction.IsInteractive = false;
            btnCancel.IsInteractive = false;
            ThreadPool.QueueUserWorkItem(RunUninstall);
        }
        else if (step == 2)
        {
            titleHeader.Text = "Uninstallation Complete";
            descLabel.Text = "Spoonbill AI Studio V4 has been cleanly removed from your workstation.";
            currentProgressPercent = 100;
            customProgressBar.Invalidate();
            btnAction.ButtonText = "Finish";
            btnAction.ActiveBackColor = ClrNvidiaGreen;
            btnAction.ActiveForeColor = Color.Black;
            btnAction.BorderColor = ClrNvidiaGreen;
            btnAction.IsInteractive = true;
            btnCancel.Visible = false;
            btnAction.Location = new Point(550, 12);
        }
    }

    private void OnActionClick(object sender, EventArgs e)
    {
        if (currentStep == 0)
        {
            ShowStep(1);
        }
        else if (currentStep == 2)
        {
            this.Close();
        }
    }

    private void RunUninstall(object state)
    {
        try
        {
            AppendLog("Terminating any running Spoonbill Studio processes...");
            SetProgress(20);
            KillRunningProcesses();

            AppendLog("Removing Desktop and Start Menu shortcuts...");
            SetProgress(50);
            string[] lnkDirs = new string[] {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
            };
            foreach (string ld in lnkDirs)
            {
                try
                {
                    if (Directory.Exists(ld))
                    {
                        string p = Path.Combine(ld, "Spoonbill AI Studio.lnk");
                        if (File.Exists(p)) { File.Delete(p); }
                    }
                }
                catch {}
            }

            AppendLog("Removing Windows Registry uninstaller registration...");
            SetProgress(75);
            RemoveRegistryEntry();

            AppendLog("Removing application files from " + targetDir + "...");
            DeleteApplicationDirectory(targetDir);
            SetProgress(100);
            AppendLog("Uninstallation completed successfully.");

            Thread.Sleep(500);
            this.Invoke((MethodInvoker)delegate {
                ShowStep(2);
            });
        }
        catch (Exception ex)
        {
            AppendLog("Error during uninstallation: " + ex.Message);
            this.Invoke((MethodInvoker)delegate {
                btnAction.ButtonText = "Close";
                btnAction.IsInteractive = true;
                btnCancel.Visible = false;
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

    private void SetProgress(int val)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                currentProgressPercent = Math.Min(100, Math.Max(0, val));
                customProgressBar.Invalidate();
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

    private void RemoveRegistryEntry()
    {
        string[] keys = new string[] {
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio",
            @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio"
        };
        foreach (string k in keys)
        {
            try { Registry.LocalMachine.DeleteSubKeyTree(k, false); } catch {}
            try { Registry.CurrentUser.DeleteSubKeyTree(k, false); } catch {}
        }
    }

    private void DeleteApplicationDirectory(string dir)
    {
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    if (string.Equals(file, currentExe, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch {}
                }

                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 2 > nul & rd /s /q \"" + dir + "\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch {}
        }

        // Also clean up any legacy AppData folder so no orphan files remain
        try
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] legacyPaths = new string[] {
                Path.Combine(localApp, "Programs", "Spoonbill_Studio"),
                Path.Combine(localApp, "Spoonbill_Studio")
            };
            foreach (string lp in legacyPaths)
            {
                if (Directory.Exists(lp) && !string.Equals(lp, dir, StringComparison.OrdinalIgnoreCase))
                {
                    try { Directory.Delete(lp, true); } catch {}
                }
            }
        }
        catch {}
    }

    public void RunVisualVerification(string outDir)
    {
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        this.Show();
        Application.DoEvents();
        Thread.Sleep(150);

        ShowStep(0);
        Application.DoEvents();
        using (Bitmap bmp = new Bitmap(this.Width, this.Height))
        {
            this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
            bmp.Save(Path.Combine(outDir, "uninstall_step0_confirm.png"), ImageFormat.Png);
        }

        currentStep = 1;
        targetCard.Visible = false;
        customProgressBar.Visible = true;
        currentProgressPercent = 65;
        customProgressBar.Invalidate();
        logScrollBox.Visible = true;
        logScrollBox.SetText(
            "[19:45:01] Terminating any running Spoonbill Studio processes...\r\n" +
            "[19:45:01] Removing Desktop shortcut...\r\n" +
            "[19:45:02] Removing Start Menu shortcut...\r\n" +
            "[19:45:02] Removing Windows Registry uninstaller registration..."
        );
        Application.DoEvents();
        using (Bitmap bmp = new Bitmap(this.Width, this.Height))
        {
            this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
            bmp.Save(Path.Combine(outDir, "uninstall_step1_progress.png"), ImageFormat.Png);
        }

        this.Close();
    }

    private static bool IsAdministrator()
    {
        try
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveInstallDir(string[] args, string currentExe)
    {
        if (args != null && args.Length > 0 && Directory.Exists(args[0]))
        {
            return args[0];
        }

        string parentDir = Path.GetDirectoryName(currentExe);
        if (!string.IsNullOrEmpty(parentDir) && File.Exists(Path.Combine(parentDir, "Spoonbill_Studio.exe")))
        {
            return parentDir;
        }

        string[] regKeys = new string[] {
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio",
            @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio"
        };
        foreach (string rk in regKeys)
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(rk))
                {
                    if (k != null)
                    {
                        object loc = k.GetValue("InstallLocation");
                        if (loc != null && Directory.Exists(loc.ToString())) return loc.ToString();
                    }
                }
            }
            catch {}
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(rk))
                {
                    if (k != null)
                    {
                        object loc = k.GetValue("InstallLocation");
                        if (loc != null && Directory.Exists(loc.ToString())) return loc.ToString();
                    }
                }
            }
            catch {}
        }

        string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Spoonbill_Studio");
        if (Directory.Exists(pf)) return pf;

        string localApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Spoonbill_Studio");
        if (Directory.Exists(localApp)) return localApp;

        return pf;
    }

    [STAThread]
    public static void Main(string[] args)
    {
        string currentExe = Process.GetCurrentProcess().MainModule.FileName;
        string installDir = ResolveInstallDir(args, currentExe);

        if (args != null && args.Length >= 2 && args[0] == "--verify-ui")
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SpoonbillUninstaller vForm = new SpoonbillUninstaller(installDir);
            vForm.RunVisualVerification(args[1]);
            return;
        }

        if (!IsAdministrator())
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.WorkingDirectory = Environment.CurrentDirectory;
                psi.FileName = Application.ExecutablePath;
                if (args != null && args.Length > 0)
                {
                    psi.Arguments = string.Join(" ", args);
                }
                psi.Verb = "runas";
                Process.Start(psi);
                return;
            }
            catch {}
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string tempDir = Path.GetTempPath();
        if (currentExe.StartsWith(installDir, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                string tempExe = Path.Combine(tempDir, "Spoonbill_Uninstall_" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(currentExe, tempExe, true);
                ProcessStartInfo psi = new ProcessStartInfo(tempExe, "\"" + installDir + "\"");
                psi.Verb = "runas";
                Process.Start(psi);
                return;
            }
            catch {}
        }

        Application.Run(new SpoonbillUninstaller(installDir));
    }
}
