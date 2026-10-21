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
        this.Size = new Size(520, 240);
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
        titleLabel.Size = new Size(460, 30);
        this.Controls.Add(titleLabel);

        statusLabel = new Label();
        statusLabel.Text = "Preparing installation package...";
        statusLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        statusLabel.ForeColor = Color.FromArgb(161, 161, 170);
        statusLabel.Location = new Point(30, 65);
        statusLabel.Size = new Size(460, 25);
        this.Controls.Add(statusLabel);

        progressBar = new ProgressBar();
        progressBar.Location = new Point(30, 105);
        progressBar.Size = new Size(445, 24);
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
