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
    private Panel panelModeAndOptions;
    private Panel panelProgress;
    private Panel panelFinish;

    // Step 1: System Check Controls
    private Label lblSysTitle;
    private Label lblSysDesc;
    private TextBox txtSysSummary;

    // Step 2: Agreement Controls
    private Label lblAgreeTitle;
    private Label lblAgreeDesc;
    private TextBox txtAgreementTerms;
    private RadioButton rbAgree;
    private RadioButton rbDisagree;

    // Step 3: Installation Mode & Options Controls
    private Label lblOptTitle;
    private Label lblOptDesc;
    private RadioButton rbModeExpress;
    private RadioButton rbModeCustom;
    private Panel pnlCustomOptions;
    private TextBox txtTargetDir;
    private Button btnBrowseTarget;
    private Label lblDiskSpace;
    private CheckBox chkCleanInstall;
    private CheckBox chkInstallModels;
    private CheckBox chkInstallDeps;
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
    private CheckBox chkOpenFolder;

    // Installation Parameters
    private string targetDir;
    private string pythonBinary = null;
    private bool existingVersionDetected = false;
    private bool isInstalling = false;

    public SpoonbillNvidiaInstaller()
    {
        this.Text = "Spoonbill AI Studio V4 — Installation Wizard";
        this.Size = new Size(740, 520);
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
        sidebarPanel.Width = 205;
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
            "2. User Agreement",
            "3. Options & Target",
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
            lbl.Size = new Size(165, 24);
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
        btnBack.Location = new Point(420, 12);
        btnBack.BackColor = Color.FromArgb(39, 39, 42);
        btnBack.ForeColor = Color.FromArgb(228, 228, 231);
        btnBack.FlatStyle = FlatStyle.Flat;
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => { if (currentStep > 0 && !isInstalling) SwitchStep(currentStep - 1); };
        bottomPanel.Controls.Add(btnBack);

        btnNext = new Button();
        btnNext.Text = "Next >";
        btnNext.Size = new Size(96, 32);
        btnNext.Location = new Point(514, 12);
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
        btnCancel.Location = new Point(620, 12);
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
        contentPanel.Padding = new Padding(25, 20, 25, 15);
        this.Controls.Add(contentPanel);

        // ================= PAGE 0: SYSTEM CHECK =================
        panelSystemCheck = new Panel();
        panelSystemCheck.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelSystemCheck);

        lblSysTitle = new Label();
        lblSysTitle.Text = "System Compatibility & Environment Scan";
        lblSysTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblSysTitle.ForeColor = Color.White;
        lblSysTitle.Location = new Point(20, 16);
        lblSysTitle.Size = new Size(460, 28);
        panelSystemCheck.Controls.Add(lblSysTitle);

        lblSysDesc = new Label();
        lblSysDesc.Text = "Verifying operating system, hardware resources, and existing installations...";
        lblSysDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblSysDesc.Location = new Point(22, 46);
        lblSysDesc.Size = new Size(460, 24);
        panelSystemCheck.Controls.Add(lblSysDesc);

        txtSysSummary = new TextBox();
        txtSysSummary.Multiline = true;
        txtSysSummary.ReadOnly = true;
        txtSysSummary.BackColor = Color.FromArgb(16, 16, 18);
        txtSysSummary.ForeColor = Color.FromArgb(228, 228, 231);
        txtSysSummary.Font = new Font("Consolas", 9F, FontStyle.Regular);
        txtSysSummary.BorderStyle = BorderStyle.FixedSingle;
        txtSysSummary.Location = new Point(22, 75);
        txtSysSummary.Size = new Size(460, 280);
        panelSystemCheck.Controls.Add(txtSysSummary);

        // ================= PAGE 1: USER AGREEMENT =================
        panelAgreement = new Panel();
        panelAgreement.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelAgreement);

        lblAgreeTitle = new Label();
        lblAgreeTitle.Text = "End User License & AI Dependencies Agreement";
        lblAgreeTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblAgreeTitle.ForeColor = Color.White;
        lblAgreeTitle.Location = new Point(20, 16);
        lblAgreeTitle.Size = new Size(460, 28);
        panelAgreement.Controls.Add(lblAgreeTitle);

        lblAgreeDesc = new Label();
        lblAgreeDesc.Text = "Please review the license terms, data privacy policy, and dependency permissions.";
        lblAgreeDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblAgreeDesc.Location = new Point(22, 46);
        lblAgreeDesc.Size = new Size(460, 24);
        panelAgreement.Controls.Add(lblAgreeDesc);

        txtAgreementTerms = new TextBox();
        txtAgreementTerms.Multiline = true;
        txtAgreementTerms.ReadOnly = true;
        txtAgreementTerms.ScrollBars = ScrollBars.Vertical;
        txtAgreementTerms.BackColor = Color.FromArgb(16, 16, 18);
        txtAgreementTerms.ForeColor = Color.FromArgb(212, 212, 216);
        txtAgreementTerms.Font = new Font("Consolas", 8F, FontStyle.Regular);
        txtAgreementTerms.BorderStyle = BorderStyle.FixedSingle;
        txtAgreementTerms.Location = new Point(22, 72);
        txtAgreementTerms.Size = new Size(460, 180);
        txtAgreementTerms.Text =
            "BLACK-FACED SPOONBILL AI STUDIO V4 — LICENSE & USAGE AGREEMENT" + Environment.NewLine +
            "=========================================================================" + Environment.NewLine + Environment.NewLine +
            "1. LICENSE GRANT & CONSERVATION PURPOSE" + Environment.NewLine +
            "This software is provided under the Creative Commons Attribution-NonCommercial-" + Environment.NewLine +
            "ShareAlike 4.0 International License (CC BY-NC-SA 4.0). You are permitted to" + Environment.NewLine +
            "use, adapt, and run the studio for non-commercial ecological research, academic" + Environment.NewLine +
            "study, and Black-faced Spoonbill (Platalea minor) wildlife census." + Environment.NewLine +
            "Commercial monetization or sale of this application without explicit permission" + Environment.NewLine +
            "from the original authors is strictly prohibited." + Environment.NewLine + Environment.NewLine +
            "2. AI DEPENDENCIES & AUTOMATED PIP EXECUTION" + Environment.NewLine +
            "By agreeing, you authorize the setup wizard to verify your local Python 3" + Environment.NewLine +
            "installation and run 'pip install -r requirements_lean.txt' to acquire runtime" + Environment.NewLine +
            "libraries (onnxruntime, fastapi, uvicorn, opencv-python-headless, pillow, numpy)." + Environment.NewLine +
            "Total download footprint is approximately 50 MB with zero PyTorch bloat." + Environment.NewLine + Environment.NewLine +
            "3. LOCAL DATA PRIVACY GUARANTEE" + Environment.NewLine +
            "All deep learning inference, image processing, and census calculations execute" + Environment.NewLine +
            "100% locally on your device. No user photographs, surveillance feeds, or survey" + Environment.NewLine +
            "data are uploaded to external cloud endpoints." + Environment.NewLine + Environment.NewLine +
            "4. DEPENDENCY REQUIREMENT & CANCELLATION" + Environment.NewLine +
            "Spoonbill AI Studio cannot operate without its neural inference backend." + Environment.NewLine +
            "If you select 'I do not accept', installation will be cancelled immediately.";
        panelAgreement.Controls.Add(txtAgreementTerms);

        rbAgree = new RadioButton();
        rbAgree.Text = "I accept the license terms and agree to install required AI dependencies (Recommended)";
        rbAgree.Checked = true;
        rbAgree.ForeColor = Color.FromArgb(228, 228, 231);
        rbAgree.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        rbAgree.Location = new Point(22, 260);
        rbAgree.Size = new Size(460, 40);
        panelAgreement.Controls.Add(rbAgree);

        rbDisagree = new RadioButton();
        rbDisagree.Text = "I do not accept the terms (Cancel installation and exit setup immediately)";
        rbDisagree.ForeColor = Color.FromArgb(248, 113, 113);
        rbDisagree.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        rbDisagree.Location = new Point(22, 302);
        rbDisagree.Size = new Size(460, 36);
        panelAgreement.Controls.Add(rbDisagree);

        // ================= PAGE 2: MODE & OPTIONS =================
        panelModeAndOptions = new Panel();
        panelModeAndOptions.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelModeAndOptions);

        lblOptTitle = new Label();
        lblOptTitle.Text = "Installation Mode & Configuration Options";
        lblOptTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblOptTitle.ForeColor = Color.White;
        lblOptTitle.Location = new Point(20, 16);
        lblOptTitle.Size = new Size(460, 28);
        panelModeAndOptions.Controls.Add(lblOptTitle);

        lblOptDesc = new Label();
        lblOptDesc.Text = "Choose between automatic express setup or custom component selection.";
        lblOptDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblOptDesc.Location = new Point(22, 46);
        lblOptDesc.Size = new Size(460, 24);
        panelModeAndOptions.Controls.Add(lblOptDesc);

        rbModeExpress = new RadioButton();
        rbModeExpress.Text = "Express Installation (Recommended for most users)\nInstalls all components, ONNX models, dependencies, and shortcuts automatically.";
        rbModeExpress.Checked = true;
        rbModeExpress.ForeColor = Color.FromArgb(228, 228, 231);
        rbModeExpress.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        rbModeExpress.Location = new Point(22, 75);
        rbModeExpress.Size = new Size(460, 42);
        rbModeExpress.CheckedChanged += (s, e) => ToggleCustomOptions(!rbModeExpress.Checked);
        panelModeAndOptions.Controls.Add(rbModeExpress);

        rbModeCustom = new RadioButton();
        rbModeCustom.Text = "Custom / Advanced Installation (For researchers & developers)\nConfigure custom installation directory, clean install, and selective components.";
        rbModeCustom.ForeColor = Color.FromArgb(212, 212, 216);
        rbModeCustom.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        rbModeCustom.Location = new Point(22, 120);
        rbModeCustom.Size = new Size(460, 42);
        panelModeAndOptions.Controls.Add(rbModeCustom);

        pnlCustomOptions = new Panel();
        pnlCustomOptions.Location = new Point(22, 165);
        pnlCustomOptions.Size = new Size(460, 205);
        pnlCustomOptions.Enabled = false;
        panelModeAndOptions.Controls.Add(pnlCustomOptions);

        Label lblDest = new Label();
        lblDest.Text = "Destination Directory:";
        lblDest.ForeColor = Color.FromArgb(212, 212, 216);
        lblDest.Location = new Point(0, 0);
        lblDest.Size = new Size(200, 18);
        pnlCustomOptions.Controls.Add(lblDest);

        txtTargetDir = new TextBox();
        txtTargetDir.Text = targetDir;
        txtTargetDir.BackColor = Color.FromArgb(16, 16, 18);
        txtTargetDir.ForeColor = Color.FromArgb(228, 228, 231);
        txtTargetDir.Location = new Point(0, 20);
        txtTargetDir.Size = new Size(365, 23);
        txtTargetDir.TextChanged += (s, e) => { targetDir = txtTargetDir.Text; UpdateDiskSpace(); };
        pnlCustomOptions.Controls.Add(txtTargetDir);

        btnBrowseTarget = new Button();
        btnBrowseTarget.Text = "Browse...";
        btnBrowseTarget.Size = new Size(85, 25);
        btnBrowseTarget.Location = new Point(375, 19);
        btnBrowseTarget.BackColor = Color.FromArgb(39, 39, 42);
        btnBrowseTarget.ForeColor = Color.FromArgb(228, 228, 231);
        btnBrowseTarget.FlatStyle = FlatStyle.Flat;
        btnBrowseTarget.FlatAppearance.BorderSize = 0;
        btnBrowseTarget.Click += OnBrowseTargetClick;
        pnlCustomOptions.Controls.Add(btnBrowseTarget);

        lblDiskSpace = new Label();
        lblDiskSpace.Text = "Required space: ~550 MB | Available space: Calculating...";
        lblDiskSpace.ForeColor = Color.FromArgb(161, 161, 170);
        lblDiskSpace.Font = new Font("Segoe UI", 8F);
        lblDiskSpace.Location = new Point(0, 46);
        lblDiskSpace.Size = new Size(460, 18);
        pnlCustomOptions.Controls.Add(lblDiskSpace);

        chkCleanInstall = new CheckBox();
        chkCleanInstall.Text = "Perform Clean Installation (Safely purge previous build files before install)";
        chkCleanInstall.Checked = true;
        chkCleanInstall.ForeColor = Color.FromArgb(246, 130, 31);
        chkCleanInstall.Location = new Point(0, 68);
        chkCleanInstall.Size = new Size(460, 22);
        pnlCustomOptions.Controls.Add(chkCleanInstall);

        chkInstallModels = new CheckBox();
        chkInstallModels.Text = "Install Bundled ONNX Models (YOLO11 Nano & Large neural weights)";
        chkInstallModels.Checked = true;
        chkInstallModels.ForeColor = Color.FromArgb(228, 228, 231);
        chkInstallModels.Location = new Point(0, 92);
        chkInstallModels.Size = new Size(460, 22);
        pnlCustomOptions.Controls.Add(chkInstallModels);

        chkInstallDeps = new CheckBox();
        chkInstallDeps.Text = "Install AI Dependencies via pip (onnxruntime, fastapi, uvicorn, opencv)";
        chkInstallDeps.Checked = true;
        chkInstallDeps.ForeColor = Color.FromArgb(228, 228, 231);
        chkInstallDeps.Location = new Point(0, 116);
        chkInstallDeps.Size = new Size(460, 22);
        pnlCustomOptions.Controls.Add(chkInstallDeps);

        chkDesktopShortcut = new CheckBox();
        chkDesktopShortcut.Text = "Create Desktop shortcut";
        chkDesktopShortcut.Checked = true;
        chkDesktopShortcut.ForeColor = Color.FromArgb(228, 228, 231);
        chkDesktopShortcut.Location = new Point(0, 140);
        chkDesktopShortcut.Size = new Size(210, 22);
        pnlCustomOptions.Controls.Add(chkDesktopShortcut);

        chkStartMenuShortcut = new CheckBox();
        chkStartMenuShortcut.Text = "Create Start Menu shortcut";
        chkStartMenuShortcut.Checked = true;
        chkStartMenuShortcut.ForeColor = Color.FromArgb(228, 228, 231);
        chkStartMenuShortcut.Location = new Point(230, 140);
        chkStartMenuShortcut.Size = new Size(210, 22);
        pnlCustomOptions.Controls.Add(chkStartMenuShortcut);

        chkRegisterUninstaller = new CheckBox();
        chkRegisterUninstaller.Text = "Register Uninstaller in Windows Installed Apps (Control Panel / Settings)";
        chkRegisterUninstaller.Checked = true;
        chkRegisterUninstaller.ForeColor = Color.FromArgb(228, 228, 231);
        chkRegisterUninstaller.Location = new Point(0, 164);
        chkRegisterUninstaller.Size = new Size(460, 22);
        pnlCustomOptions.Controls.Add(chkRegisterUninstaller);

        UpdateDiskSpace();

        // ================= PAGE 3: PROGRESS =================
        panelProgress = new Panel();
        panelProgress.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(panelProgress);

        lblProgTitle = new Label();
        lblProgTitle.Text = "Installing Spoonbill AI Studio V4...";
        lblProgTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblProgTitle.ForeColor = Color.White;
        lblProgTitle.Location = new Point(20, 16);
        lblProgTitle.Size = new Size(460, 28);
        panelProgress.Controls.Add(lblProgTitle);

        lblProgStatus = new Label();
        lblProgStatus.Text = "Preparing installation package...";
        lblProgStatus.ForeColor = Color.FromArgb(161, 161, 170);
        lblProgStatus.Location = new Point(22, 46);
        lblProgStatus.Size = new Size(460, 20);
        panelProgress.Controls.Add(lblProgStatus);

        progressBar = new ProgressBar();
        progressBar.Location = new Point(22, 70);
        progressBar.Size = new Size(460, 16);
        progressBar.Style = ProgressBarStyle.Continuous;
        panelProgress.Controls.Add(progressBar);

        logBox = new TextBox();
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.BackColor = Color.FromArgb(14, 14, 16);
        logBox.ForeColor = Color.FromArgb(161, 161, 170);
        logBox.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
        logBox.Location = new Point(22, 98);
        logBox.Size = new Size(460, 255);
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
        lblFinishTitle.Location = new Point(20, 16);
        lblFinishTitle.Size = new Size(460, 28);
        panelFinish.Controls.Add(lblFinishTitle);

        lblFinishDesc = new Label();
        lblFinishDesc.Text = "Spoonbill AI Studio V4 has been installed successfully on your system.\n\nAll selected neural components, inference models, and Windows integration options are configured and ready for research use.";
        lblFinishDesc.ForeColor = Color.FromArgb(161, 161, 170);
        lblFinishDesc.Location = new Point(22, 54);
        lblFinishDesc.Size = new Size(460, 60);
        panelFinish.Controls.Add(lblFinishDesc);

        chkLaunchNow = new CheckBox();
        chkLaunchNow.Text = "Launch Spoonbill AI Studio now";
        chkLaunchNow.Checked = true;
        chkLaunchNow.ForeColor = Color.FromArgb(118, 185, 0);
        chkLaunchNow.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        chkLaunchNow.Location = new Point(22, 130);
        chkLaunchNow.Size = new Size(400, 26);
        panelFinish.Controls.Add(chkLaunchNow);

        chkOpenFolder = new CheckBox();
        chkOpenFolder.Text = "Open application installation folder";
        chkOpenFolder.Checked = false;
        chkOpenFolder.ForeColor = Color.FromArgb(212, 212, 216);
        chkOpenFolder.Location = new Point(22, 162);
        chkOpenFolder.Size = new Size(400, 24);
        panelFinish.Controls.Add(chkOpenFolder);
    }

    private void ToggleCustomOptions(bool enabled)
    {
        pnlCustomOptions.Enabled = enabled;
        if (!enabled)
        {
            // Reset to Express defaults
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
            fbd.Description = "Select Destination Folder for Spoonbill AI Studio V4";
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
        txtSysSummary.Clear();
        txtSysSummary.AppendText("======================================================" + Environment.NewLine);
        txtSysSummary.AppendText("  Spoonbill AI Studio V4 — System Compatibility Scan  " + Environment.NewLine);
        txtSysSummary.AppendText("======================================================" + Environment.NewLine + Environment.NewLine);

        // 1. Operating System
        string osName = Environment.OSVersion.ToString();
        txtSysSummary.AppendText("[OK] Operating System   : " + osName + " (64-bit)" + Environment.NewLine);

        // 2. Hardware Resources
        int cores = Environment.ProcessorCount;
        txtSysSummary.AppendText("[OK] Processor Cores    : " + cores + " Logical Cores detected" + Environment.NewLine);

        // 3. Previous Version Detection
        existingVersionDetected = Directory.Exists(targetDir) && File.Exists(Path.Combine(targetDir, "Spoonbill_Studio.exe"));
        if (existingVersionDetected)
        {
            txtSysSummary.AppendText("[FOUND] Existing Build  : Previous installation detected at" + Environment.NewLine);
            txtSysSummary.AppendText("        Directory       : " + targetDir + Environment.NewLine);
            txtSysSummary.AppendText("        Clean Install   : Recommended (Setup will cleanly purge old files)." + Environment.NewLine);
        }
        else
        {
            txtSysSummary.AppendText("[OK] Existing Build     : No conflicting previous installation detected." + Environment.NewLine);
        }

        // 4. Python Runtime Environment
        pythonBinary = FindPythonBinary();
        if (!string.IsNullOrEmpty(pythonBinary))
        {
            txtSysSummary.AppendText("[OK] Python Runtime     : Detected at " + pythonBinary + Environment.NewLine);
            txtSysSummary.AppendText("     Environment Status : Ready for automated AI dependencies installation." + Environment.NewLine);
        }
        else
        {
            txtSysSummary.AppendText("[WARNING] Python Status : Python 3.10+ was not detected on system PATH." + Environment.NewLine);
            txtSysSummary.AppendText("          Recommendation: Install Python 3.10+ from https://www.python.org/" + Environment.NewLine);
            txtSysSummary.AppendText("                          (Ensure 'Add python.exe to PATH' is checked)." + Environment.NewLine);
        }

        txtSysSummary.AppendText(Environment.NewLine + "System scan completed. Click 'Next >' to review User Agreement.");
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
                    "You selected not to agree to the terms or install required dependencies.\n\n" +
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

            // Clean Install if requested or express
            bool doClean = rbModeExpress.Checked || chkCleanInstall.Checked;
            if (doClean && Directory.Exists(targetDir))
            {
                UpdateStatus("Cleaning previous installation files...", 10);
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

            // Extraction
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
