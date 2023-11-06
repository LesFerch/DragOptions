using System;
using System.Diagnostics;
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
        static string sDisable = "Disable right-click drag";
        static string sEnable = "Enable right-click drag";
        static string sHelp = "Help";
        static string sExit = "Exit (Ctrl to remove)";
        static string sInput = "Enter the number of pixels the cursor must move to start a drag (default = 4)";
        static string sRunning = "Another instance of the application is already running";

        static NotifyIcon notifyIcon;
        static Thread otherProgramThread;
        static bool isOtherProgramRunning = false;
        static Mutex mutex = new Mutex(true, "{2B21EF82-7755-4349-B0A2-80BB049D41E4}");

        [STAThread]
        static void Main()
        {
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

            // Create a context menu for the system tray icon
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(sChange, ChangeDragSensitivity_Click);
            contextMenu.MenuItems.Add(sDisable, DisableRightClickDrag_Click);
            contextMenu.MenuItems.Add(sEnable, EnableRightClickDrag_Click);
            contextMenu.MenuItems.Add(sHelp, Help_Click);
            contextMenu.MenuItems.Add(sExit, Exit_Click);
            notifyIcon.ContextMenu = contextMenu;

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
                // Show the context menu when either left or right-clicked
                MethodInfo method = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(notifyIcon, null);
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
                    string newDragWidth = Microsoft.VisualBasic.Interaction.InputBox(sInput, sChange, currentDragWidth);

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
