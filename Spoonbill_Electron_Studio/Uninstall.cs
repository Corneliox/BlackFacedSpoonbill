using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

public class SpoonbillUninstaller : Form
{
    private Panel sidebarPanel;
    private Panel contentPanel;
    private Panel bottomPanel;

    private Label brandTitle;
    private Label brandSub;
    private Label[] stepLabels;
    private int currentStep = 0;

    // Content controls
    private Label titleHeader;
    private Label descLabel;
    private ProgressBar progressBar;
    private RichTextBox logBox;
    private Button btnAction;
    private Button btnCancel;

    private string targetDir;

    public SpoonbillUninstaller(string installDir)
    {
        this.targetDir = installDir;
        this.Text = "Spoonbill AI Studio V4 — Uninstaller";
        this.Size = new Size(640, 420);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(27, 27, 30);
        this.ForeColor = Color.FromArgb(228, 228, 231);
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        InitializeLayout();
        ShowStep(0);
    }

    private void InitializeLayout()
    {
        // 1. Sidebar (NVIDIA Dark Style)
        sidebarPanel = new Panel();
        sidebarPanel.Dock = DockStyle.Left;
        sidebarPanel.Width = 190;
        sidebarPanel.BackColor = Color.FromArgb(18, 18, 20);
        this.Controls.Add(sidebarPanel);

        brandTitle = new Label();
        brandTitle.Text = "SPOONBILL AI";
        brandTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        brandTitle.ForeColor = Color.FromArgb(118, 185, 0); // NVIDIA signature green
        brandTitle.Location = new Point(20, 24);
        brandTitle.AutoSize = true;
        sidebarPanel.Controls.Add(brandTitle);

        brandSub = new Label();
        brandSub.Text = "Studio Uninstaller";
        brandSub.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        brandSub.ForeColor = Color.FromArgb(161, 161, 170);
        brandSub.Location = new Point(21, 46);
        brandSub.AutoSize = true;
        sidebarPanel.Controls.Add(brandSub);

        string[] steps = new string[] {
            "1. Confirm Removal",
            "2. Uninstallation",
            "3. Finish"
        };
        stepLabels = new Label[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            Label lbl = new Label();
            lbl.Text = steps[i];
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(113, 113, 122);
            lbl.Location = new Point(24, 100 + (i * 36));
            lbl.Size = new Size(150, 24);
            sidebarPanel.Controls.Add(lbl);
            stepLabels[i] = lbl;
        }

        // 2. Bottom Navigation Bar
        bottomPanel = new Panel();
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Height = 54;
        bottomPanel.BackColor = Color.FromArgb(22, 22, 25);
        bottomPanel.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(39, 39, 42)), 0, 0, bottomPanel.Width, 0);
        };
        this.Controls.Add(bottomPanel);

        btnAction = new Button();
        btnAction.Text = "Uninstall";
        btnAction.Size = new Size(96, 32);
        btnAction.Location = new Point(415, 11);
        btnAction.BackColor = Color.FromArgb(118, 185, 0);
        btnAction.ForeColor = Color.Black;
        btnAction.FlatStyle = FlatStyle.Flat;
        btnAction.FlatAppearance.BorderSize = 0;
        btnAction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnAction.Click += OnActionClick;
        bottomPanel.Controls.Add(btnAction);

        btnCancel = new Button();
        btnCancel.Text = "Cancel";
        btnCancel.Size = new Size(84, 32);
        btnCancel.Location = new Point(520, 11);
        btnCancel.BackColor = Color.FromArgb(39, 39, 42);
        btnCancel.ForeColor = Color.FromArgb(228, 228, 231);
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => this.Close();
        bottomPanel.Controls.Add(btnCancel);

        // 3. Content Panel
        contentPanel = new Panel();
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Padding = new Padding(30, 25, 30, 15);
        this.Controls.Add(contentPanel);

        titleHeader = new Label();
        titleHeader.Text = "Uninstall Spoonbill AI Studio";
        titleHeader.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        titleHeader.ForeColor = Color.White;
        titleHeader.Location = new Point(30, 24);
        titleHeader.Size = new Size(380, 30);
        contentPanel.Controls.Add(titleHeader);

        descLabel = new Label();
        descLabel.Text = "This wizard will completely remove Spoonbill AI Studio V4 and all associated shortcuts from your computer.";
        descLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        descLabel.ForeColor = Color.FromArgb(161, 161, 170);
        descLabel.Location = new Point(32, 62);
        descLabel.Size = new Size(380, 50);
        contentPanel.Controls.Add(descLabel);

        progressBar = new ProgressBar();
        progressBar.Location = new Point(32, 120);
        progressBar.Size = new Size(375, 14);
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Visible = false;
        contentPanel.Controls.Add(progressBar);

        logBox = new RichTextBox();
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        logBox.BackColor = Color.FromArgb(18, 18, 20);
        logBox.ForeColor = Color.FromArgb(220, 220, 230);
        logBox.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        logBox.Location = new Point(32, 145);
        logBox.Size = new Size(375, 150);
        logBox.BorderStyle = BorderStyle.None;
        logBox.Visible = false;
        contentPanel.Controls.Add(logBox);
    }

    private void ShowStep(int step)
    {
        currentStep = step;
        for (int i = 0; i < stepLabels.Length; i++)
        {
            if (i == step)
            {
                stepLabels[i].ForeColor = Color.FromArgb(118, 185, 0);
                stepLabels[i].Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            else if (i < step)
            {
                stepLabels[i].ForeColor = Color.FromArgb(228, 228, 231);
                stepLabels[i].Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
            else
            {
                stepLabels[i].ForeColor = Color.FromArgb(113, 113, 122);
                stepLabels[i].Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
        }

        if (step == 0)
        {
            titleHeader.Text = "Confirm Removal";
            descLabel.Text = "Are you sure you want to uninstall Spoonbill AI Studio V4?\n\nTarget directory:\n" + targetDir;
            progressBar.Visible = false;
            logBox.Visible = false;
            btnAction.Text = "Uninstall";
            btnAction.Enabled = true;
            btnCancel.Enabled = true;
        }
        else if (step == 1)
        {
            titleHeader.Text = "Uninstalling Spoonbill AI Studio...";
            descLabel.Text = "Removing installed components, shortcuts, and registry entries.";
            progressBar.Visible = true;
            progressBar.Value = 10;
            logBox.Visible = true;
            btnAction.Enabled = false;
            btnCancel.Enabled = false;
            ThreadPool.QueueUserWorkItem(RunUninstall);
        }
        else if (step == 2)
        {
            titleHeader.Text = "Uninstallation Complete";
            descLabel.Text = "Spoonbill AI Studio V4 has been cleanly removed from your system.";
            progressBar.Value = 100;
            btnAction.Text = "Close";
            btnAction.Enabled = true;
            btnCancel.Visible = false;
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

            AppendLog("Removing Desktop shortcut...");
            SetProgress(40);
            string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Spoonbill AI Studio.lnk");
            if (File.Exists(desktopLnk)) { try { File.Delete(desktopLnk); } catch {} }

            AppendLog("Removing Start Menu shortcut...");
            SetProgress(60);
            string startMenuLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Spoonbill AI Studio.lnk");
            if (File.Exists(startMenuLnk)) { try { File.Delete(startMenuLnk); } catch {} }

            AppendLog("Removing Windows Registry uninstaller registration...");
            SetProgress(80);
            RemoveRegistryEntry();

            AppendLog("Removing application files...");
            DeleteApplicationDirectory(targetDir);
            SetProgress(100);
            AppendLog("Uninstallation successful.");

            Thread.Sleep(500);
            this.Invoke((MethodInvoker)delegate {
                ShowStep(2);
            });
        }
        catch (Exception ex)
        {
            AppendLog("Error during uninstallation: " + ex.Message);
            this.Invoke((MethodInvoker)delegate {
                btnAction.Text = "Close";
                btnAction.Enabled = true;
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
                logBox.AppendText("[" + stamp + "] " + message + Environment.NewLine);
            });
        }
    }

    private void SetProgress(int val)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                progressBar.Value = Math.Min(100, Math.Max(0, val));
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
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpoonbillAIStudio", false);
        }
        catch {}
    }

    private void DeleteApplicationDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return;
        try
        {
            // Delete all files except currently executing binary if any
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

            // Schedule self-cleanup via background cmd if running from temp or child
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 2 > nul & rd /s /q \"" + dir + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
        catch {}
    }

    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string defaultDir = Path.Combine(localApp, "Programs", "Spoonbill_Studio");
        string installDir = (args.Length > 0 && Directory.Exists(args[0])) ? args[0] : defaultDir;

        // Check if running inside installDir; if so, copy to %TEMP% to unlock directory deletion
        string currentExe = Process.GetCurrentProcess().MainModule.FileName;
        string tempDir = Path.GetTempPath();
        if (currentExe.StartsWith(installDir, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                string tempExe = Path.Combine(tempDir, "Spoonbill_Uninstall_" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(currentExe, tempExe, true);
                ProcessStartInfo psi = new ProcessStartInfo(tempExe, "\"" + installDir + "\"");
                Process.Start(psi);
                return;
            }
            catch {}
        }

        Application.Run(new SpoonbillUninstaller(installDir));
    }
}
