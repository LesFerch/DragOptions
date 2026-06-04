using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Reflection;
using System.Text;

namespace SystemTrayApp
{
    class Program
    {
        static string sTip = "Drag Options";
        static string sChange = "Change drag sensitivity";
        static string sChangeClick = "Change double-click sensitivity";
        static string sDisable = "Disable right-click drag";
        static string sEnable = "Enable right-click drag";
        static string sHelp = "Help";
        static string sExit = "Exit (Ctrl to remove)";
        static string sInput = "Enter the number of pixels the cursor must move to start a drag (default = 4)";
        static string sInputClick = "Enter the number of pixels the cursor is allowed to move between clicks (default = 4)";
        static string sRunning = "Another instance of the application is already running";

        static NotifyIcon notifyIcon;
        static Thread otherProgramThread;
        static bool isOtherProgramRunning = false;
        static Mutex mutex = new Mutex(true, "{2B21EF82-7755-4349-B0A2-80BB049D41E4}");

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoadLanguageStrings();

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                // If the mutex is already locked, another instance is running
                MessageBox.Show(sRunning,sTip);
                return;
            }

            // Register the application to run on startup
            RegisterStartup();

            // Initialize the application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create a system tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = sTip;

            // Handle left-click on the system tray icon
            notifyIcon.MouseClick += NotifyIcon_MouseClick;

            // Start the application and listen for events
            Application.Run();
        }

        // Load language strings from INI file
        static void LoadLanguageStrings()
        {
            string lang = GetLang();
            IniFile iniFile = new IniFile("language.ini");

            sTip = iniFile.ReadString(lang, "sTip", sTip);
            sChange = iniFile.ReadString(lang, "sChange", sChange);
            sDisable = iniFile.ReadString(lang, "sDisable", sDisable);
            sEnable = iniFile.ReadString(lang, "sEnable", sEnable);
            sHelp = iniFile.ReadString(lang, "sHelp", sHelp);
            sExit = iniFile.ReadString(lang, "sExit", sExit);
            sInput = iniFile.ReadString(lang, "sInput", sInput);
            sInputClick = iniFile.ReadString(lang, "sInputClick", sInputClick);
            sChangeClick = iniFile.ReadString(lang, "sChangeClick", sChangeClick);
            sRunning = iniFile.ReadString(lang, "sRunning", sRunning);
        }

        // Get the current system language
        static string GetLang()
        {
            string lang = "en";
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\International");
                if (key != null)
                {
                    lang = key.GetValue("LocaleName") as string;
                    key.Close();
                }
            }
            catch { }

            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop");
                if (key != null)
                {
                    string[] preferredLanguages = key.GetValue("PreferredUILanguages") as string[];
                    if (preferredLanguages != null && preferredLanguages.Length > 0)
                    {
                        lang = preferredLanguages[0];
                    }
                    key.Close();
                }
            }
            catch { }

            return lang.Substring(0, 2).ToLower();
        }

        // Handle both left and right-clicks on the system tray icon
        static void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                TrayMenuForm.Show(
                    new[] { sChange, sChangeClick, sDisable, sEnable, sHelp, sExit },
                    new EventHandler[] {
                        ChangeDragSensitivity_Click,
                        ChangeDoubleClickSensitivity_Click,
                        DisableRightClickDrag_Click,
                        EnableRightClickDrag_Click,
                        Help_Click,
                        Exit_Click
                    }
                );
            }
        }

        // Handle changing the drag sensitivity
        static void ChangeDragSensitivity_Click(object sender, EventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                if (key != null)
                {
                    // Prompt the user to change drag sensitivity and update the registry
                    string currentDragWidth = key.GetValue("DragWidth")?.ToString();
                    string newDragWidth = InputDialog.Show(sInput, sChange, currentDragWidth);

                    if (newDragWidth != null && int.TryParse(newDragWidth, out int newValue))
                    {
                        SetDragWidthHeight(newValue);
                    }
                }
            }
        }

        // Change the size of the rectangle the cursor must move out of to start a drag
        static void SetDragWidthHeight(int value)
        {
            SystemParametersInfo(SPI_SETDRAGWIDTH, (uint)value, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            SystemParametersInfo(SPI_SETDRAGHEIGHT, (uint)value, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        // Handle changing the double-click sensitivity
        static void ChangeDoubleClickSensitivity_Click(object sender, EventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
            {
                if (key != null)
                {
                    // Prompt the user to change double-click sensitivity and update the registry
                    string currentDoubleClickWidth = key.GetValue("DoubleClickWidth")?.ToString();
                    string newDoubleClickWidth = InputDialog.Show(sInputClick, sChangeClick, currentDoubleClickWidth);

                    if (newDoubleClickWidth != null && int.TryParse(newDoubleClickWidth, out int newValue))
                    {
                        SetDoubleClickWidthHeight(newValue);
                    }
                }
            }
        }

        // Change the size of the rectangle within which a second click is considered a double-click
        static void SetDoubleClickWidthHeight(int value)
        {
            SystemParametersInfo(SPI_SETDOUBLECLKWIDTH, (uint)value, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            SystemParametersInfo(SPI_SETDOUBLECLKHEIGHT, (uint)value, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
        
        // Function to run the code to disable right-click drag
        static void RunOtherProgram()
        {
            const int WH_MOUSE_LL = 14;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_RBUTTONUP = 0x0205;

            IntPtr hookID = IntPtr.Zero;
            bool rightButtonDown = false;

            // Set up a low-level mouse hook
            IntPtr SetHook(LowLevelMouseProc proc)
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            // Callback function for the mouse hook
            IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0)
                {
                    if (wParam == (IntPtr)WM_RBUTTONDOWN)
                    {
                        rightButtonDown = true;

                        // Send a fake mouse move event to block right-click drag
                        INPUT[] inputs = new INPUT[1];
                        inputs[0].type = INPUT_MOUSE;
                        inputs[0].mi.dx = 0;
                        inputs[0].mi.dy = 0;
                        inputs[0].mi.mouseData = 0;
                        inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
                        inputs[0].mi.time = 0;
                        inputs[0].mi.dwExtraInfo = IntPtr.Zero;
                        SendInput(1, inputs, Marshal.SizeOf(inputs[0]));
                    }
                    else if (wParam == (IntPtr)WM_RBUTTONUP)
                    {
                        rightButtonDown = false;
                    }
                }

                // Block right-click drag if needed
                if (rightButtonDown)
                {
                    return new IntPtr(1);
                }

                return CallNextHookEx(hookID, nCode, wParam, lParam);
            }

            // Set up the hook and run the event loop
            hookID = SetHook(MouseHookCallback);

            while (isOtherProgramRunning)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }

            // Unhook the mouse hook
            UnhookWindowsHookEx(hookID);
        }

        // Function to disable right-click drag
        static void DisableRightClickDrag_Click(object sender, EventArgs e)
        {
            if (!isOtherProgramRunning)
            {
                isOtherProgramRunning = true;
                otherProgramThread = new Thread(RunOtherProgram);
                otherProgramThread.Start();
            }
        }

        // Function to enable right-click drag
        static void EnableRightClickDrag_Click(object sender, EventArgs e)
        {
            if (isOtherProgramRunning)
            {
                isOtherProgramRunning = false;
                otherProgramThread.Join();
            }
        }

        // Function to open Help page
        static void Help_Click(object sender, EventArgs e)
        {
            Process.Start("https://lesferch.github.io/DragOptions/");
        }

        // Register the application to run on startup
        static void RegisterStartup()
        {
            string keyName = "DragOptions";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    key.SetValue(keyName, Application.ExecutablePath);
                }
            }
        }

        // Function to exit the application
        static void Exit_Click(object sender, EventArgs e)
        {
            // Check if the Ctrl key is held down
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // Remove the application's entry from the startup
                string keyName = "DragOptions";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(keyName, false);
                    }
                }
            }

            if (isOtherProgramRunning)
            {
                isOtherProgramRunning = false;
                otherProgramThread.Join();
            }

            // Hide the system tray icon and exit the application
            notifyIcon.Visible = false;
            Application.Exit();
        }

        // P/Invoke declarations for WinAPI functions
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendInput(int nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

        // Structs and constants for mouse input simulation
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public const uint INPUT_MOUSE = 0;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Constants for drag sensitivity

        const uint SPI_SETDRAGWIDTH = 0x004C;
        const uint SPI_SETDRAGHEIGHT = 0x004D;

        // Constants for double-click sensitivity

        const uint SPI_SETDOUBLECLKWIDTH = 0x001D;
        const uint SPI_SETDOUBLECLKHEIGHT = 0x001E;
        const uint SPIF_UPDATEINIFILE = 0x1;
        const uint SPIF_SENDCHANGE = 0x2;
    }

    public class IniFile
    {
        private readonly string iniFilePath;

        public IniFile(string fileName)
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeDirectory = System.IO.Path.GetDirectoryName(exePath);
            iniFilePath = System.IO.Path.Combine(exeDirectory, fileName);
        }

        public string ReadString(string section, string key, string defaultValue)
        {
            try
            {
                if (File.Exists(iniFilePath))
                {
                    return IniFileParser.ReadValue(section, key, defaultValue, iniFilePath);
                }
            }
            catch { }

            return defaultValue;
        }
    }

    public class TrayMenuForm : Form
    {
        private EventHandler _selectedHandler;

        public static void Show(string[] labels, EventHandler[] handlers)
        {
            EventHandler selected = null;
            using (var form = new TrayMenuForm(labels, handlers))
            {
                form.ShowDialog();
                selected = form._selectedHandler;
            }
            selected?.Invoke(null, EventArgs.Empty);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private TrayMenuForm(string[] labels, EventHandler[] handlers)
        {
            bool dark = InputDialog.IsDark();

            Color backColor = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(240, 240, 240);
            Color foreColor = dark ? Color.White : Color.Black;
            Color hoverColor = dark ? Color.FromArgb(60, 60, 60) : Color.White;

            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(0);
            BackColor = backColor;
            ForeColor = foreColor;
            Font = new Font("Segoe UI", 9);

            Shown += (s, e) =>
            {
                RoundCorners(Handle);
                Activate();
            };
            Deactivate += (s, e) => Close();

            int itemHeight = 32;
            int vPad = 6;
            int minWidth = 120;
            int hPad = 16;
            int yOffset = vPad;

            // Measure the widest label
            int maxWidth = minWidth;
            foreach (string label in labels)
            {
                int w = TextRenderer.MeasureText(label, Font).Width + hPad * 2;
                if (w > maxWidth) maxWidth = w;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                var panel = new Panel
                {
                    Width = maxWidth,
                    Height = itemHeight,
                    Location = new Point(0, yOffset),
                    BackColor = backColor
                };

                var lbl = new Label
                {
                    Text = labels[i],
                    Width = maxWidth,
                    Height = itemHeight,
                    Location = new Point(hPad, 0),
                    AutoSize = false,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Default
                };

                panel.Controls.Add(lbl);
                Controls.Add(panel);
                yOffset += itemHeight;

                var currentPanel = panel;
                var currentLabel = lbl;
                var currentHandler = handlers[i];

                Action setHover = () =>
                {
                    currentPanel.BackColor = hoverColor;
                    currentLabel.BackColor = hoverColor;
                };
                Action clearHover = () =>
                {
                    currentPanel.BackColor = backColor;
                    currentLabel.BackColor = backColor;
                };
                Action click = () =>
                {
                    _selectedHandler = currentHandler;
                    Close();
                };

                currentPanel.MouseEnter += (s, e) => setHover();
                currentPanel.MouseLeave += (s, e) => clearHover();
                currentPanel.Click += (s, e) => click();
                currentLabel.MouseEnter += (s, e) => setHover();
                currentLabel.MouseLeave += (s, e) => clearHover();
                currentLabel.Click += (s, e) => click();
            }

            yOffset += vPad;

            // Position centered under cursor, clamped to screen
            Point cursor = Cursor.Position;
            Screen screen = Screen.FromPoint(cursor);
            int w2 = maxWidth;
            int h2 = yOffset;
            int x = Math.Max(screen.WorkingArea.Left, Math.Min(screen.WorkingArea.Right - w2, cursor.X - w2 / 2));
            int y = Math.Max(screen.WorkingArea.Top, Math.Min(screen.WorkingArea.Bottom - h2, cursor.Y));
            Location = new Point(x, y);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static void RoundCorners(IntPtr hwnd)
        {
            int preference = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref preference, sizeof(int));
        }
    }

    public class InputDialog : Form
    {
        private Label promptLabel;
        private TextBox inputBox;
        private Button okButton;

        private InputDialog(string prompt, string title, string defaultValue)
        {
            bool dark = IsDark();

            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.Manual;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new System.Drawing.Size(360, 130);

            Shown += (s, e) =>
            {
                Point cursor = Cursor.Position;
                Screen screen = Screen.FromPoint(cursor);
                int x = Math.Max(screen.WorkingArea.Left, Math.Min(screen.WorkingArea.Right - Width, cursor.X - Width / 2));
                int y = Math.Max(screen.WorkingArea.Top, Math.Min(screen.WorkingArea.Bottom - Height, cursor.Y - Height / 2));
                Location = new Point(x, y);
            };

            promptLabel = new Label();
            promptLabel.Text = prompt;
            promptLabel.AutoSize = false;
            promptLabel.Size = new System.Drawing.Size(340, 40);
            promptLabel.Location = new System.Drawing.Point(10, 12);

            inputBox = new TextBox();
            inputBox.Text = defaultValue ?? "";
            inputBox.Size = new System.Drawing.Size(340, 23);
            inputBox.Location = new System.Drawing.Point(10, 58);
            inputBox.BorderStyle = BorderStyle.FixedSingle;

            okButton = new Button();
            okButton.Text = "OK";
            okButton.Size = new System.Drawing.Size(80, 26);
            okButton.Location = new System.Drawing.Point((ClientSize.Width - 80) / 2, 92);
            okButton.DialogResult = DialogResult.OK;

            AcceptButton = okButton;

            Controls.Add(promptLabel);
            Controls.Add(inputBox);
            Controls.Add(okButton);

            if (dark)
            {
                DarkTitleBar(Handle);
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.White;
                inputBox.BackColor = Color.FromArgb(60, 60, 60);
                inputBox.ForeColor = Color.White;
                okButton.FlatStyle = FlatStyle.Flat;
                okButton.FlatAppearance.BorderColor = SystemColors.Highlight;
                okButton.FlatAppearance.BorderSize = 1;
                okButton.BackColor = Color.FromArgb(60, 60, 60);
                okButton.FlatAppearance.MouseOverBackColor = Color.Black;
            }
        }

        public static string Show(string prompt, string title, string defaultValue)
        {
            using (var dlg = new InputDialog(prompt, title, defaultValue))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    return dlg.inputBox.Text;
                return null;
            }
        }

        public static bool IsDark()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    object value = key.GetValue(valueName);
                    if (value is int intValue)
                        return intValue == 0;
                }
            }
            return false;
        }

        public enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref int pvAttribute, uint cbAttribute);

        static void DarkTitleBar(IntPtr hWnd)
        {
            var preference = Convert.ToInt32(true);
            DwmSetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(uint));
        }
    }

    public static class IniFileParser
    {
        public static string ReadValue(string section, string key, string defaultValue, string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                string currentSection = null;

                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    }
                    else if (currentSection == section)
                    {
                        var parts = trimmedLine.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2 && parts[0].Trim() == key)
                        {
                            return parts[1].Trim();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return defaultValue;
        }
    }
}
