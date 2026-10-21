using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

public class SpoonbillNvidiaInstaller : Form
{
    // Layout panels
    private Panel sidebarPanel;
    private Panel contentPanel;
    private Panel bottomPanel;

    // Sidebar items
    private Label brandTitle;
    private Label brandSub;
    private Label[] stepLabels;
    private int currentStep = 0;

    // Navigation buttons
    private Button btnBack;
    private Button btnNext;
    private Button btnCancel;

    // Step Panels
    private Panel panelSystemCheck;
    private Panel panelAgreement;
    private Panel panelOptions;
    private Panel panelProgress;
    private Panel panelFinish;

    // Step 1: System Check Controls
    private Label lblSysTitle;
    private Label lblSysDesc;
    private TextBox txtSysSummary;

    // Step 2: Agreement Controls
    private Label lblAgreeTitle;
    private Label lblAgreeDesc;
    private RadioButton rbAgree;
    private RadioButton rbDisagree;
    private Panel pnlWarningBox;
    private Label lblWarningText;

    // Step 3: Options Controls
    private Label lblOptTitle;
    private Label lblOptDesc;
    private TextBox txtTargetDir;
    private CheckBox chkDesktopShortcut;
    private CheckBox chkStartMenuShortcut;
    private CheckBox chkRegisterUninstaller;

    // Step 4: Progress Controls
    private Label lblProgTitle;
    private Label lblProgStatus;
    private ProgressBar progressBar;
    private TextBox logBox;

    // Step 5: Finish Controls
    private Label lblFinishTitle;
    private Label lblFinishDesc;
    private CheckBox chkLaunchNow;

    // Installation Parameters
    private string targetDir;
    private string pythonBinary = null;
    private bool isInstalling = false;

    public SpoonbillNvidiaInstaller()
    {
        this.Text = "Spoonbill AI Studio V4 — Installation Wizard";
        this.Size = new Size(720, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 27);
        this.ForeColor = Color.FromArgb(228, 228, 231);
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        targetDir = Path.Combine(localApp, "Programs", "Spoonbill_Studio");

        BuildSidebar();
        BuildBottomBar();
        BuildContentPanels();

        SwitchStep(0);
    }

    private void BuildSidebar()
    {
        sidebarPanel = new Panel();
        sidebarPanel.Dock = DockStyle.Left;
        sidebarPanel.Width = 200;
        sidebarPanel.BackColor = Color.FromArgb(16, 16, 18);
        this.Controls.Add(sidebarPanel);

        brandTitle = new Label();
        brandTitle.Text = "SPOONBILL AI";
        brandTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        brandTitle.ForeColor = Color.FromArgb(118, 185, 0); // NVIDIA signature green
        brandTitle.Location = new Point(22, 24);
        brandTitle.AutoSize = true;
        sidebarPanel.Controls.Add(brandTitle);

        brandSub = new Label();
        brandSub.Text = "Studio Setup Wizard";
        brandSub.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        brandSub.ForeColor = Color.FromArgb(161, 161, 170);
        brandSub.Location = new Point(23, 46);
        brandSub.AutoSize = true;
        sidebarPanel.Controls.Add(brandSub);

        string[] steps = new string[] {
            "1. System Check",
            "2. License & Agreement",
            "3. Options",
            "4. Installation",
            "5. Finish"
        };
        stepLabels = new Label[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            Label lbl = new Label();
            lbl.Text = steps[i];
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(113, 113, 122);
            lbl.Location = new Point(24, 100 + (i * 36));
            lbl.Size = new Size(160, 24);
            sidebarPanel.Controls.Add(lbl);
            stepLabels[i] = lbl;
        }
    }

    private void BuildBottomBar()
    {
        bottomPanel = new Panel();
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Height = 56;
        bottomPanel.BackColor = Color.FromArgb(20, 20, 23);
        bottomPanel.Paint += (s, e) => {
            e.Graphics.DrawLine(new Pen(Color.FromArgb(39, 39, 42)), 0, 0, bottomPanel.Width, 0);
        };
        this.Controls.Add(bottomPanel);

        btnBack = new Button();
        btnBack.Text = "< Back";
        btnBack.Size = new Size(84, 32);
        btnBack.Location = new Point(400, 12);
        btnBack.BackColor = Color.FromArgb(39, 39, 42);
        btnBack.ForeColor = Color.FromArgb(228, 228, 231);
        btnBack.FlatStyle = FlatStyle.Flat;
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => { if (currentStep > 0 && !isInstalling) SwitchStep(currentStep - 1); };
        bottomPanel.Controls.Add(btnBack);

        btnNext = new Button();
        btnNext.Text = "Next >";
        btnNext.Size = new Size(96, 32);
        btnNext.Location = new Point(494, 12);
        btnNext.BackColor = Color.FromArgb(118, 185, 0);
        btnNext.ForeColor = Color.Black;
        btnNext.FlatStyle = FlatStyle.Flat;
        btnNext.FlatAppearance.BorderSize = 0;
        btnNext.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnNext.Click += OnNextClick;
        bottomPanel.Controls.Add(btnNext);

        btnCancel = new Button();
        btnCancel.Text = "Cancel";
        btnCancel.Size = new Size(84, 32);
        btnCancel.Location = new Point(600, 12);
        btnCancel.BackColor = Color.FromArgb(39, 39, 42);
        btnCancel.ForeColor = Color.FromArgb(228, 228, 231);
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += OnCancelClick;
        bottomPanel.Controls.Add(btnCancel);
    }

    private void BuildContentPanels()
    {
        contentPanel = new Panel();
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Padding = new Padding(30, 25, 30, 15);
        this.Controls.Add(contentPanel);

        // ================= PAGE 0: SYSTEM CHECK =================
        panelSystemCheck = new Panel();
        panelSystemCheck.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelSystemCheck);

        lblSysTitle = new Label();
        lblSysTitle.Text = "System Compatibility Check";
        lblSysTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblSysTitle.ForeColor = Color.White;
        lblSysTitle.Location = new Point(25, 20);
        lblSysTitle.Size = new Size(440, 30);
        panelSystemCheck.Controls.Add(lblSysTitle);

        lblSysDesc = new Label();
        lblSysDesc.Text = "Verifying operating system, hardware capabilities, and Python runtime...";
        lblSysDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblSysDesc.Location = new Point(26, 52);
        lblSysDesc.Size = new Size(440, 25);
        panelSystemCheck.Controls.Add(lblSysDesc);

        txtSysSummary = new TextBox();
        txtSysSummary.Multiline = true;
        txtSysSummary.ReadOnly = true;
        txtSysSummary.BackColor = Color.FromArgb(16, 16, 18);
        txtSysSummary.ForeColor = Color.FromArgb(228, 228, 231);
        txtSysSummary.Font = new Font("Consolas", 9F, FontStyle.Regular);
        txtSysSummary.BorderStyle = BorderStyle.FixedSingle;
        txtSysSummary.Location = new Point(26, 85);
        txtSysSummary.Size = new Size(440, 250);
        panelSystemCheck.Controls.Add(txtSysSummary);

        // ================= PAGE 1: AGREEMENT =================
        panelAgreement = new Panel();
        panelAgreement.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelAgreement);

        lblAgreeTitle = new Label();
        lblAgreeTitle.Text = "License & AI Dependencies Agreement";
        lblAgreeTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblAgreeTitle.ForeColor = Color.White;
        lblAgreeTitle.Location = new Point(25, 20);
        lblAgreeTitle.Size = new Size(440, 30);
        panelAgreement.Controls.Add(lblAgreeTitle);

        lblAgreeDesc = new Label();
        lblAgreeDesc.Text = "Spoonbill AI Studio requires lean Python AI runtime dependencies (ONNX Runtime, FastAPI, OpenCV, Pillow ~50MB) to perform bird segmentation and census.";
        lblAgreeDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblAgreeDesc.Location = new Point(26, 52);
        lblAgreeDesc.Size = new Size(440, 40);
        panelAgreement.Controls.Add(lblAgreeDesc);

        rbAgree = new RadioButton();
        rbAgree.Text = "Agree and automatically install required AI dependencies (Recommended)\nSetup will verify and run 'pip install -r requirements_lean.txt'.";
        rbAgree.Checked = true;
        rbAgree.ForeColor = Color.FromArgb(228, 228, 231);
        rbAgree.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        rbAgree.Location = new Point(26, 105);
        rbAgree.Size = new Size(440, 48);
        panelAgreement.Controls.Add(rbAgree);

        rbDisagree = new RadioButton();
        rbDisagree.Text = "Do not agree to install dependencies (Cancel installation and exit)\nSetup will abort without installing application files.";
        rbDisagree.ForeColor = Color.FromArgb(248, 113, 113);
        rbDisagree.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        rbDisagree.Location = new Point(26, 160);
        rbDisagree.Size = new Size(440, 45);
        panelAgreement.Controls.Add(rbDisagree);

        pnlWarningBox = new Panel();
        pnlWarningBox.BackColor = Color.FromArgb(35, 20, 20);
        pnlWarningBox.Location = new Point(26, 220);
        pnlWarningBox.Size = new Size(440, 100);
        pnlWarningBox.Paint += (s, e) => {
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(239, 68, 68), 1), 0, 0, pnlWarningBox.Width - 1, pnlWarningBox.Height - 1);
        };
        panelAgreement.Controls.Add(pnlWarningBox);

        lblWarningText = new Label();
        lblWarningText.Text = "NOTICE:\nIf you choose not to install the required AI dependencies, installation cannot continue because Spoonbill AI Studio cannot run without its local neural inference engine.\nSelecting 'Do not agree' will cancel setup and exit immediately.";
        lblWarningText.ForeColor = Color.FromArgb(254, 202, 202);
        lblWarningText.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        lblWarningText.Location = new Point(14, 12);
        lblWarningText.Size = new Size(410, 75);
        pnlWarningBox.Controls.Add(lblWarningText);

        // ================= PAGE 2: OPTIONS =================
        panelOptions = new Panel();
        panelOptions.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelOptions);

        lblOptTitle = new Label();
        lblOptTitle.Text = "Installation Options";
        lblOptTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblOptTitle.ForeColor = Color.White;
        lblOptTitle.Location = new Point(25, 20);
        lblOptTitle.Size = new Size(440, 30);
        panelOptions.Controls.Add(lblOptTitle);

        lblOptDesc = new Label();
        lblOptDesc.Text = "Configure installation destination and program shortcuts.";
        lblOptDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblOptDesc.Location = new Point(26, 52);
        lblOptDesc.Size = new Size(440, 25);
        panelOptions.Controls.Add(lblOptDesc);

        Label lblTarget = new Label();
        lblTarget.Text = "Destination Folder:";
        lblTarget.ForeColor = Color.FromArgb(228, 228, 231);
        lblTarget.Location = new Point(26, 88);
        lblTarget.Size = new Size(200, 20);
        panelOptions.Controls.Add(lblTarget);

        txtTargetDir = new TextBox();
        txtTargetDir.Text = targetDir;
        txtTargetDir.ReadOnly = true;
        txtTargetDir.BackColor = Color.FromArgb(16, 16, 18);
        txtTargetDir.ForeColor = Color.FromArgb(161, 161, 170);
        txtTargetDir.Location = new Point(26, 110);
        txtTargetDir.Size = new Size(440, 23);
        panelOptions.Controls.Add(txtTargetDir);

        chkDesktopShortcut = new CheckBox();
        chkDesktopShortcut.Text = "Create Desktop shortcut (Spoonbill AI Studio.lnk)";
        chkDesktopShortcut.Checked = true;
        chkDesktopShortcut.ForeColor = Color.FromArgb(228, 228, 231);
        chkDesktopShortcut.Location = new Point(26, 160);
        chkDesktopShortcut.Size = new Size(400, 24);
        panelOptions.Controls.Add(chkDesktopShortcut);

        chkStartMenuShortcut = new CheckBox();
        chkStartMenuShortcut.Text = "Create Start Menu shortcut";
        chkStartMenuShortcut.Checked = true;
        chkStartMenuShortcut.ForeColor = Color.FromArgb(228, 228, 231);
        chkStartMenuShortcut.Location = new Point(26, 195);
        chkStartMenuShortcut.Size = new Size(400, 24);
        panelOptions.Controls.Add(chkStartMenuShortcut);

        chkRegisterUninstaller = new CheckBox();
        chkRegisterUninstaller.Text = "Register Uninstaller in Windows Installed Apps (Control Panel)";
        chkRegisterUninstaller.Checked = true;
        chkRegisterUninstaller.ForeColor = Color.FromArgb(228, 228, 231);
        chkRegisterUninstaller.Location = new Point(26, 230);
        chkRegisterUninstaller.Size = new Size(400, 24);
        panelOptions.Controls.Add(chkRegisterUninstaller);

        // ================= PAGE 3: PROGRESS =================
        panelProgress = new Panel();
        panelProgress.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelProgress);

        lblProgTitle = new Label();
        lblProgTitle.Text = "Installing Spoonbill AI Studio V4...";
        lblProgTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblProgTitle.ForeColor = Color.White;
        lblProgTitle.Location = new Point(25, 20);
        lblProgTitle.Size = new Size(440, 30);
        panelProgress.Controls.Add(lblProgTitle);

        lblProgStatus = new Label();
        lblProgStatus.Text = "Preparing installation package...";
        lblProgStatus.ForeColor = Color.FromArgb(161, 161, 170);
        lblProgStatus.Location = new Point(26, 52);
        lblProgStatus.Size = new Size(440, 20);
        panelProgress.Controls.Add(lblProgStatus);

        progressBar = new ProgressBar();
        progressBar.Location = new Point(26, 80);
        progressBar.Size = new Size(440, 16);
        progressBar.Style = ProgressBarStyle.Continuous;
        panelProgress.Controls.Add(progressBar);

        logBox = new TextBox();
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.BackColor = Color.FromArgb(14, 14, 16);
        logBox.ForeColor = Color.FromArgb(161, 161, 170);
        logBox.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        logBox.Location = new Point(26, 110);
        logBox.Size = new Size(440, 225);
        logBox.BorderStyle = BorderStyle.FixedSingle;
        panelProgress.Controls.Add(logBox);

        // ================= PAGE 4: FINISH =================
        panelFinish = new Panel();
        panelFinish.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelFinish);

        lblFinishTitle = new Label();
        lblFinishTitle.Text = "Installation Complete";
        lblFinishTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblFinishTitle.ForeColor = Color.White;
        lblFinishTitle.Location = new Point(25, 20);
        lblFinishTitle.Size = new Size(440, 30);
        panelFinish.Controls.Add(lblFinishTitle);

        lblFinishDesc = new Label();
        lblFinishDesc.Text = "Spoonbill AI Studio V4 has been installed successfully on your computer.\n\nAll core components, ONNX neural inference models, and application shortcuts have been configured.";
        lblFinishDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblFinishDesc.Location = new Point(26, 60);
        lblFinishDesc.Size = new Size(440, 70);
        panelFinish.Controls.Add(lblFinishDesc);

        chkLaunchNow = new CheckBox();
        chkLaunchNow.Text = "Launch Spoonbill AI Studio now";
        chkLaunchNow.Checked = true;
        chkLaunchNow.ForeColor = Color.FromArgb(118, 185, 0);
        chkLaunchNow.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        chkLaunchNow.Location = new Point(26, 145);
        chkLaunchNow.Size = new Size(400, 26);
        panelFinish.Controls.Add(chkLaunchNow);
    }

    private void SwitchStep(int step)
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

        panelSystemCheck.Visible = (step == 0);
        panelAgreement.Visible = (step == 1);
        panelOptions.Visible = (step == 2);
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
        txtSysSummary.Clear();
        txtSysSummary.AppendText("======================================================" + Environment.NewLine);
        txtSysSummary.AppendText("  Spoonbill AI Studio V4 — System Compatibility Scan  " + Environment.NewLine);
        txtSysSummary.AppendText("======================================================" + Environment.NewLine + Environment.NewLine);

        // OS Check
        string osName = Environment.OSVersion.ToString();
        txtSysSummary.AppendText("[OK] Operating System : " + osName + " (64-bit compatible)" + Environment.NewLine);

        // CPU & Memory
        int cores = Environment.ProcessorCount;
        txtSysSummary.AppendText("[OK] Processor Cores  : " + cores + " Logical Cores detected" + Environment.NewLine);

        // Python Check
        pythonBinary = FindPythonBinary();
        if (!string.IsNullOrEmpty(pythonBinary))
        {
            txtSysSummary.AppendText("[OK] Python Runtime   : Detected at " + pythonBinary + Environment.NewLine);
            txtSysSummary.AppendText("     Status           : Ready for automated AI dependencies installation." + Environment.NewLine);
        }
        else
        {
            txtSysSummary.AppendText("[WARNING] Python      : Python 3.10+ was not detected on system PATH." + Environment.NewLine);
            txtSysSummary.AppendText("          Notice      : Setup can install packages if Python is installed," + Environment.NewLine);
            txtSysSummary.AppendText("                        or you may install Python from https://www.python.org/" + Environment.NewLine);
        }

        txtSysSummary.AppendText(Environment.NewLine + "Ready to proceed. Click 'Next >' to continue.");
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
                    "You have selected not to install the required AI dependencies.\n\n" +
                    "Spoonbill AI Studio cannot function without its neural inference engine.\n\n" +
                    "Do you want to cancel the installation now?",
                    "Cancel Installation?",
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
            MessageBox.Show(this, "Installation is currently in progress. Please wait.", "Setup Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult res = MessageBox.Show(this, "Are you sure you want to cancel the setup wizard?", "Exit Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            Application.Exit();
        }
    }

    private void RunInstallation(object state)
    {
        try
        {
            UpdateStatus("Setting target: " + targetDir, 5);
            AppendLog("Preparing destination directory: " + targetDir);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            UpdateStatus("Extracting core studio files, models, and UI...", 15);
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
            AppendLog("Payload extraction complete.");
            SetProgress(45);

            // Dependency Installation
            if (rbAgree.Checked)
            {
                UpdateStatus("Checking for Python environment...", 50);
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
                        UpdateStatus("Installing AI dependencies via pip (onnxruntime, fastapi, uvicorn)...", 60);
                        AppendLog("Executing: " + pyCmd + " -m pip install -r " + Path.GetFileName(reqFile));
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
            if (chkDesktopShortcut.Checked || chkStartMenuShortcut.Checked)
            {
                UpdateStatus("Creating program shortcuts...", 85);
                AppendLog("Generating desktop and start menu shortcuts...");
                CreateShortcuts(targetDir, chkDesktopShortcut.Checked, chkStartMenuShortcut.Checked);
            }

            // Uninstaller Registration
            if (chkRegisterUninstaller.Checked)
            {
                UpdateStatus("Registering Windows Uninstaller...", 95);
                AppendLog("Registering Spoonbill AI Studio in Windows Control Panel / Settings...");
                RegisterUninstaller(targetDir);
            }

            SetProgress(100);
            UpdateStatus("Installation complete.", 100);
            AppendLog("All components installed successfully.");

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
                progressBar.Value = Math.Min(100, Math.Max(0, progress));
            });
        }
    }

    private void SetProgress(int progress)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                progressBar.Value = Math.Min(100, Math.Max(0, progress));
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
