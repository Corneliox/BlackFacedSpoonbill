using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

public class SpoonbillInstaller : Form
{
    private ProgressBar progressBar;
    private Label statusLabel;
    private Label titleLabel;

    public SpoonbillInstaller()
    {
        this.Text = "Spoonbill AI Studio V4 — Setup Wizard";
        this.Size = new Size(540, 250);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 27);
        this.ForeColor = Color.FromArgb(228, 228, 231);

        titleLabel = new Label();
        titleLabel.Text = "Installing Spoonbill AI Studio V4 (Photoshop Edition)...";
        titleLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        titleLabel.ForeColor = Color.White;
        titleLabel.Location = new Point(30, 25);
        titleLabel.Size = new Size(480, 30);
        this.Controls.Add(titleLabel);

        statusLabel = new Label();
        statusLabel.Text = "Preparing installation package...";
        statusLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        statusLabel.ForeColor = Color.FromArgb(161, 161, 170);
        statusLabel.Location = new Point(30, 65);
        statusLabel.Size = new Size(480, 25);
        this.Controls.Add(statusLabel);

        progressBar = new ProgressBar();
        progressBar.Location = new Point(30, 105);
        progressBar.Size = new Size(465, 24);
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 30;
        this.Controls.Add(progressBar);

        this.Shown += (s, e) => {
            ThreadPool.QueueUserWorkItem(RunInstall);
        };
    }

    private void RunInstall(object state)
    {
        try
        {
            UpdateStatus("Setting target: %LOCALAPPDATA%\\Programs\\Spoonbill_Studio...");
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetDir = Path.Combine(localApp, "Programs", "Spoonbill_Studio");

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            UpdateStatus("Extracting core studio files, models, and UI...");
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

            // User agreement for Python AI Dependencies
            bool installDeps = false;
            this.Invoke((MethodInvoker)delegate {
                DialogResult res = MessageBox.Show(
                    this,
                    "Would you like Setup to verify and automatically install/update required AI dependencies?\n\n" +
                    "Included packages: ONNX Runtime, FastAPI, Uvicorn, OpenCV-Headless, Pillow, NumPy (Lean ~50MB)\n\n" +
                    "• Click [Yes] to agree and let Setup run 'pip install -r requirements_lean.txt' automatically.\n" +
                    "• Click [No] to skip if your environment is already configured.",
                    "Spoonbill AI Dependencies Setup Agreement",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                installDeps = (res == DialogResult.Yes);
            });

            if (installDeps)
            {
                UpdateStatus("Checking for Python 3 on system...");
                string pythonExe = FindPythonBinary();
                if (string.IsNullOrEmpty(pythonExe))
                {
                    this.Invoke((MethodInvoker)delegate {
                        MessageBox.Show(
                            this,
                            "Python 3.10+ was not detected on system PATH or default installation directories.\n\n" +
                            "Please install Python 3.10 or newer from https://www.python.org/ (ensure 'Add python.exe to PATH' is checked).\n\n" +
                            "Setup will continue installing Spoonbill Studio files.",
                            "Python Not Detected",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    });
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
                        UpdateStatus("Installing AI dependencies via pip (please wait, ~1-2 min)...");
                        try
                        {
                            ProcessStartInfo pipPsi = new ProcessStartInfo();
                            pipPsi.FileName = pythonExe;
                            pipPsi.Arguments = "-m pip install -r \"" + reqFile + "\"";
                            pipPsi.WorkingDirectory = targetDir;
                            pipPsi.UseShellExecute = false;
                            pipPsi.CreateNoWindow = true;
                            pipPsi.RedirectStandardOutput = true;
                            pipPsi.RedirectStandardError = true;

                            using (Process pipProc = Process.Start(pipPsi))
                            {
                                pipProc.WaitForExit(180000); // 3 minutes timeout
                                if (pipProc.ExitCode == 0)
                                {
                                    UpdateStatus("AI dependencies verified and installed successfully.");
                                }
                                else
                                {
                                    UpdateStatus("Dependency installation finished with warnings.");
                                }
                            }
                        }
                        catch (Exception pex)
                        {
                            UpdateStatus("Could not complete pip install: " + pex.Message);
                        }
                    }
                }
            }

            UpdateStatus("Creating Desktop and Start Menu shortcuts...");
            CreateShortcuts(targetDir);

            UpdateStatus("Launching Spoonbill AI Studio...");
            string exePath = Path.Combine(targetDir, "Spoonbill_Studio.exe");
            if (File.Exists(exePath))
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.WorkingDirectory = targetDir;
                Process.Start(psi);
            }

            this.Invoke((MethodInvoker)delegate {
                this.Hide();
                MessageBox.Show(
                    "Spoonbill AI Studio V4 has been installed successfully!\n\n" +
                    "Installed Location: " + targetDir + "\n" +
                    "A shortcut has been created on your Desktop.\n\n" +
                    "The studio is now starting.",
                    "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                Application.Exit();
            });
        }
        catch (Exception ex)
        {
            this.Invoke((MethodInvoker)delegate {
                MessageBox.Show("Installation Error:\n" + ex.Message, "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
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

    private void UpdateStatus(string text)
    {
        if (this.IsHandleCreated)
        {
            this.Invoke((MethodInvoker)delegate {
                statusLabel.Text = text;
            });
        }
    }

    private void CreateShortcuts(string targetDir)
    {
        try
        {
            string exePath = Path.Combine(targetDir, "Spoonbill_Studio.exe");
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);

            // Desktop shortcut
            dynamic desktopLink = shell.CreateShortcut(Path.Combine(desktop, "Spoonbill AI Studio.lnk"));
            desktopLink.TargetPath = exePath;
            desktopLink.WorkingDirectory = targetDir;
            desktopLink.Description = "Black-Faced Spoonbill AI Studio V4 (Photoshop Edition)";
            desktopLink.Save();

            // Start menu shortcut
            dynamic startMenuLink = shell.CreateShortcut(Path.Combine(startMenu, "Spoonbill AI Studio.lnk"));
            startMenuLink.TargetPath = exePath;
            startMenuLink.WorkingDirectory = targetDir;
            startMenuLink.Description = "Black-Faced Spoonbill AI Studio V4 (Photoshop Edition)";
            startMenuLink.Save();
        }
        catch {}
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SpoonbillInstaller());
    }
}
