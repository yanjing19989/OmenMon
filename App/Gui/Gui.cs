  //\\   OmenMon: Hardware Monitoring & Control Utility
 //  \\  Copyright © 2023-2024 Piotr Szczepański * License: GPL3
     //  https://omenmon.github.io/

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using OmenMon.External;
using OmenMon.Library;

namespace OmenMon.AppGui {

    // Implements general GUI-specific functionality
    public static class Gui {

        // Registration handle
        private static IntPtr RegistrationHandle;

        // Registration callback
        private static PowrProf.DeviceNotifyCallbackRoutine RegistrationCallback;

        // State flags
        public static bool IsInitialized { get; private set; }

        // Unique custom message identifier used to
        // tell the GUI to bring itself to the user's attention
        public static uint MessageId;

        // Custom message parameters
        public enum MessageParam : int {
            Default         =   0,  // No parameter specified
            AnotherInstance =   1,  // Another instance has been launched
            Gui             =   2,  // Autorun task has been launched
            Key             =   3,  // Omen Key event has been registered
            NoLastParam     = 255,  // Launched not as a message response
        }

        // Default dialog font name
        public const string DIALOG_FONT = "MS Shell Dlg";

        // Windows application theme registry location
        private const string REG_PERSONALIZE_KEY =
            "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
        private const string REG_APPS_USE_LIGHT_THEME = "AppsUseLightTheme";

        // Message box flags
        public const int MB_SYSTEMMODAL = 0x00001000;  // On top of other topmost windows
        public const int MB_TASKMODAL   = 0x00002000;  // Also prevent interaction with other windows
        public const int MB_TOPMOST     = 0x00004000;  // Stay on top

#region Common Identifiers
        // Type
        public const string T_BAR = "Bar";    // ProgressBar
        public const string T_BTN = "Btn";    // Button
        public const string T_CHK = "Chk";    // CheckBox
        public const string T_CMB = "Cmb";    // ComboBox
        public const string T_FRM = "Form";   // Form
        public const string T_GRP = "Grp";    // GroupBox
        public const string T_LBL = "Lbl";    // Label
        public const string T_LNK = "Lnk";    // LinkLabel
        public const string T_PIC = "Pic";    // PictureBox
        public const string T_RDO = "Rdo";    // RadioButton
        public const string T_RTF = "Rtf";    // RtfText
        public const string T_TRK = "Trk";    // TrackBar
        public const string T_TBL = "Tbl";    // TblLayout
        public const string T_TXT = "Txt";    // TextBox

        // Group
        public const string G_FAN = "Fan";    // Fan (form & menu)
        public const string G_GPU = "Gpu";    // Graphics (menu)
        public const string G_KBD = "Kbd";    // Keyboard (form & menu)
        public const string G_TMP = "Tmp";    // Temperature (form)
        public const string G_SET = "Set";    // Settings (menu)
        public const string G_SYS = "Sys";    // System Status (form)

        // Menu item type
        public const string M_ACT = "Act";    // Action
        public const string M_HDR = "Hdr";    // Header
        public const string M_SUB = "Sub";    // Sub-menu

        // Interfix
        public const string X_UNIT = "Unit";  // Unit

        // Suffix
        public const string S_CAP = "Cap";    // Caption
        public const string S_LVL = "Lvl";    // Level
        public const string S_RTE = "Rte";    // Rate
        public const string S_VAL = "Val";    // Value
#endregion

#region Initialization & Termination
        // Initializes a Windows Forms (GUI) application
        public static void Initialize() {

            // Only do it once
            if(!IsInitialized) {

               // Default rendering settings
               Application.EnableVisualStyles();
               Application.SetCompatibleTextRenderingDefault(false);

               // Register a custom message to communicate between application instances
               // The identifier obtained this way remains unique until user logout
               MessageId = RegisterMessage(Config.GuiMessageId);

               // Load the custom TrueType font from resources
               GdiFont.Add(OmenMon.Resources.FigureFont);

               // Set the state flag
               IsInitialized = true;

               }

        }

        // Closes the Windows Forms (GUI) application
        public static void Close() {

            // Set the state flag
            IsInitialized = false;

        }
#endregion

#region Theme
        // Colors used for the current GUI theme
        public struct ThemeColors {
            public bool IsDark;
            public Color FormBack;
            public Color ControlBack;
            public Color InputBack;
            public Color Text;
            public Color MutedText;
            public Color Border;
            public Color Highlight;
            public Color Link;
        }

        // Checks whether Windows apps are set to use the dark theme
        public static bool IsDarkTheme() {
            try {
                using(RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_PERSONALIZE_KEY))
                    return Convert.ToInt32(key.GetValue(REG_APPS_USE_LIGHT_THEME, 1)) == 0;
            } catch {
                return false;
            }
        }

        // Retrieves colors for the current GUI theme
        public static ThemeColors GetThemeColors() {
            if(IsDarkTheme())
                return new ThemeColors {
                    IsDark = true,
                    FormBack = Color.FromArgb(32, 32, 32),
                    ControlBack = Color.FromArgb(48, 48, 48),
                    InputBack = Color.FromArgb(42, 42, 42),
                    Text = Color.FromArgb(242, 242, 242),
                    MutedText = Color.FromArgb(170, 170, 170),
                    Border = Color.FromArgb(72, 72, 72),
                    Highlight = Color.FromArgb(58, 150, 221),
                    Link = Color.FromArgb(87, 166, 255)
                };

            return new ThemeColors {
                IsDark = false,
                FormBack = SystemColors.Control,
                ControlBack = SystemColors.Control,
                InputBack = SystemColors.Window,
                Text = SystemColors.ControlText,
                MutedText = SystemColors.GrayText,
                Border = SystemColors.ControlDark,
                Highlight = SystemColors.Highlight,
                Link = Color.Empty
            };
        }

        // Applies the current theme to a form
        public static void ApplyTheme(Form form) {
            if(form == null)
                return;

            ThemeColors theme = GetThemeColors();

            form.HandleCreated += delegate {
                ApplyTitleBarTheme(form);
            };

            if(form.IsHandleCreated)
                ApplyTitleBarTheme(form);

            if(!theme.IsDark)
                return;

            form.BackColor = theme.FormBack;
            form.ForeColor = theme.Text;
            ApplyTheme(form.Controls, theme);
        }

        // Applies the current theme to a context menu
        public static void ApplyTheme(ToolStrip toolStrip) {
            if(toolStrip == null)
                return;

            ThemeColors theme = GetThemeColors();
            if(!theme.IsDark)
                return;

            ApplyTheme(toolStrip, theme);
        }

        // Applies theme colors to a menu strip
        private static void ApplyTheme(ToolStrip toolStrip, ThemeColors theme) {
            if(toolStrip == null)
                return;

            toolStrip.BackColor = GetMenuBack(theme);
            toolStrip.ForeColor = theme.Text;
            ApplyTheme(toolStrip.Items, theme);
        }

        // Applies the native title bar theme to a form
        public static void ApplyTitleBarTheme(Form form) {
            if(form == null || !form.IsHandleCreated)
                return;

            int enabled = IsDarkTheme() ? 1 : 0;

            try {
                if(Dwm.DwmSetWindowAttribute(
                    form.Handle,
                    Dwm.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref enabled,
                    Marshal.SizeOf(typeof(int))) == 0)
                    return;

                enabled = IsDarkTheme() ? 1 : 0;
                Dwm.DwmSetWindowAttribute(
                    form.Handle,
                    Dwm.DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY,
                    ref enabled,
                    Marshal.SizeOf(typeof(int)));
            } catch { }
        }

        // Applies theme colors to known control types
        private static void ApplyTheme(Control.ControlCollection controls, ThemeColors theme) {
            foreach(Control control in controls) {
                ApplyTheme(control, theme);

                if(control.Controls.Count > 0)
                    ApplyTheme(control.Controls, theme);
            }
        }

        // Applies theme colors to a known control type
        private static void ApplyTheme(Control control, ThemeColors theme) {
            if(control is Button button) {
                button.UseVisualStyleBackColor = false;
                button.BackColor = theme.ControlBack;
                button.ForeColor = theme.Text;
            } else if(control is CheckBox checkBox) {
                checkBox.UseVisualStyleBackColor = false;
                checkBox.BackColor = theme.FormBack;
                checkBox.ForeColor = theme.Text;
            } else if(control is RadioButton radioButton) {
                radioButton.UseVisualStyleBackColor = false;
                radioButton.BackColor = theme.FormBack;
                radioButton.ForeColor = theme.Text;
            } else if(control is ComboBox comboBox) {
                comboBox.BackColor = theme.InputBack;
                comboBox.ForeColor = theme.Text;
            } else if(control is TextBox textBox) {
                textBox.BackColor = theme.InputBack;
                textBox.ForeColor = theme.Text;
            } else if(control is RichTextBox richTextBox) {
                richTextBox.Enabled = true;
                richTextBox.BackColor = theme.FormBack;
                richTextBox.ForeColor = theme.Text;
            } else if(control is LinkLabel linkLabel) {
                linkLabel.BackColor = theme.FormBack;
                linkLabel.ForeColor = theme.Text;
                linkLabel.LinkColor = theme.Link;
                linkLabel.ActiveLinkColor = theme.Highlight;
                linkLabel.VisitedLinkColor = theme.Link;
            } else if(control is Label label) {
                label.BackColor = theme.FormBack;
                label.ForeColor = theme.Text;
            } else if(control is GroupBox groupBox) {
                groupBox.BackColor = theme.FormBack;
                groupBox.ForeColor = theme.Text;
            } else if(control is TableLayoutPanel tableLayoutPanel) {
                tableLayoutPanel.BackColor = theme.FormBack;
                tableLayoutPanel.ForeColor = theme.Text;
            } else if(control is TrackBar trackBar) {
                trackBar.BackColor = theme.FormBack;
                trackBar.ForeColor = theme.Text;
            } else if(control is PictureBox pictureBox) {
                pictureBox.BackColor = theme.FormBack;
            }
        }

        // Applies theme colors to menu items
        private static void ApplyTheme(ToolStripItemCollection items, ThemeColors theme) {
            foreach(ToolStripItem item in items) {
                item.BackColor = GetMenuBack(theme);
                item.ForeColor = item.Enabled ? theme.Text : theme.MutedText;

                if(item is ToolStripMenuItem menuItem && menuItem.DropDown != null)
                    ApplyTheme(menuItem.DropDown, theme);
            }
        }

        // Retrieves the menu background color
        private static Color GetMenuBack(ThemeColors theme) {
            return Color.FromArgb(45, 45, 48);
        }
#endregion

#region Messaging
        // Broadcasts a specific message
        public static bool BroadcastMessage(uint msg, MessageParam param = MessageParam.Default) {
            IntPtr lParam = (IntPtr) param;
            return User32.PostMessage(
                (IntPtr) User32.HWND_BROADCAST,  // Send to all top-most windows
                msg,                             // The message identifier registered beforehand
                (IntPtr) Config.AppProcessId,    // Add a semi-unique identifier to sieve out duplicates
                lParam);                         // Used to distinguish message types
        }

        // Registers a specific message
        public static uint RegisterMessage(string msg) {
            return User32.RegisterWindowMessage(msg);
        }

        // Registers a callback for suspend
        // and resume power event notifications
        public static bool RegisterSuspendResumeNotification(
            Func<IntPtr, uint, IntPtr, uint> Callback) {

            // Retain the registration handle
            RegistrationHandle = new IntPtr();

            // Set up the structure for the received data
            PowrProf.DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS Recipient
                = new PowrProf.DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS();

            // Populate the data structure with the callback function delegate
            RegistrationCallback = new PowrProf.DeviceNotifyCallbackRoutine(Callback);
            Recipient.Callback = RegistrationCallback;
            Recipient.Context = IntPtr.Zero;

            // Obtain a pointer to the recipient structure
            IntPtr RecipientPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Recipient));
            Marshal.StructureToPtr(Recipient, RecipientPtr, false);

            // Register for power suspend and resume notifications
            return PowrProf.PowerRegisterSuspendResumeNotification(
                PowrProf.DEVICE_NOTIFY_CALLBACK,
                ref Recipient, ref RegistrationHandle) == 0;

        }

        // Removes the callback for power event notifications
        public static bool UnregisterSuspendResumeNotification() {
            return RegistrationHandle != null ?
                PowrProf.PowerUnregisterSuspendResumeNotification(RegistrationHandle) == 0
                : true;
        }
#endregion

#region Scaling
        // Calculate the DPI from the reported device capabilities
        public static float GetDeviceCapsDpi(IntPtr handle) {
            IntPtr hDC = User32.GetDC(handle);
            try {

                // Note: currently seems to always report 96 dpi,
                // which makes this approach no longer very useful
                return 96f * Gdi32.GetDeviceCaps(hDC, Gdi32.DeviceCap.DESKTOPHORZRES)
                    / Gdi32.GetDeviceCaps(hDC, Gdi32.DeviceCap.HORZRES);

            } finally {

                // Release the device context
                User32.ReleaseDC(handle, hDC);

            }
        }

        // Retrieve the DPI from a device context
        public static float GetDeviceContextDpi(IntPtr handle) {
            IntPtr hDC = User32.GetDC(handle);
            try {
                using(Graphics g = Graphics.FromHdc(hDC))

                    // Note: this reports the DPI setting at launch,
                    // not taking into account any changes afterwards,
                    // regardless whether passed a window handle or zero
                    return g.DpiX;

            } finally {

                // Release the device context
                User32.ReleaseDC(handle, hDC);

            }
        }
#endregion

#region Visual
        // Shows a dialog window with error information
        public static void ShowError(string message, Exception e = null) {
            if(e == null)

                // Use a variation of the about form for a nicer error dialog
                // unless exception details are available
                GuiOp.About(
                    Config.Locale.Get(Config.L_GUI_ABOUT + "TitleError"),
                    Config.Locale.Get(Config.L_GUI_ABOUT + "TextErrorPrefix")
                        + message.Replace("\\", "\\\\")
                        + Config.Locale.Get(Config.L_GUI_ABOUT + "TextErrorSuffix"));

            else

                // Alternatively, pop up a standard message box
                MessageBox.Show(

                    // To report all the exception details
                    message + Environment.NewLine 
                        + Environment.NewLine + e.Source + ": " + e.TargetSite 
                        + Environment.NewLine + Environment.NewLine + e.StackTrace,

                    Config.AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    (MessageBoxOptions) MB_SYSTEMMODAL);

        }

        // Shows a text input dialog
        public static string ShowPromptInputText(string title, string value = "", IWin32Window owner = null) {

            // Dimension settings [px]
            const int PADDING = 6;      // Inner form margin from each side
            const int BTN_WIDTH = 25;   // Button width
            const int BTN_HEIGHT = 21;  // Button height
            const int BTN_SPACE = 2;    // Space between buttons
            const int TXT_HEIGHT = 20;  // Text box height (width is calculated)
            const int TXT_SPACE = 7;    // Space between text box and a button

            // Relative vertical co-ordinate adjustment for text box,
            // which is slightly smaller in height than a button
            const int TXT_TOP_ADJ = 2 * (BTN_HEIGHT - TXT_HEIGHT);

            // Form width (height is calculated)
            Size size = new Size(200, 2 * PADDING + TXT_TOP_ADJ + TXT_HEIGHT);

            // Instantiate the form and components
            Form FormInputText = new Form();
            Button BtnAccept = new Button();
            Button BtnCancel = new Button();
            TextBox TxtInput = new TextBox();
            ToolTip Tip = new ToolTip();

            // Suspend the layout before applying the settings
            FormInputText.SuspendLayout();

            // Selects all text when the textbox is first brought into focus
            EventHandler EventTxtInputClick =
                new EventHandler(delegate(object sender, EventArgs e) {
                    ((TextBox) sender).SelectAll(); });

            // Text input settings
            TxtInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            TxtInput.Click += EventTxtInputClick;
            TxtInput.Location = new Point(PADDING, PADDING + TXT_TOP_ADJ);
            TxtInput.MaxLength = 32;
            TxtInput.Name = T_TXT + "Input";
            TxtInput.Size = new Size(size.Width - BTN_SPACE - 2 * BTN_WIDTH - PADDING - TXT_SPACE, TXT_HEIGHT);
            TxtInput.TabIndex = 0;
            TxtInput.Text = value;

            // Cancel button settings
            BtnCancel.DialogResult = DialogResult.Cancel;
            BtnCancel.Location = new Point(size.Width - BTN_SPACE - 2 * BTN_WIDTH - PADDING, PADDING);
            BtnCancel.Name = T_BTN + "Cancel";
            BtnCancel.Size = new Size(BTN_WIDTH, BTN_HEIGHT);
            BtnCancel.TabIndex = 1;
            BtnCancel.Text = Config.Locale.Get(Config.L_GUI + T_BTN + "Del");
            BtnCancel.UseVisualStyleBackColor = true;

            // Accept button settings
            BtnAccept.DialogResult = DialogResult.OK;
            BtnAccept.Location = new Point(size.Width - BTN_WIDTH - PADDING, PADDING);
            BtnAccept.Name = T_BTN + "Accept";
            BtnAccept.Size = new Size(BTN_WIDTH, BTN_HEIGHT);
            BtnAccept.TabIndex = 2;
            BtnAccept.Text = Config.Locale.Get(Config.L_GUI + T_BTN + "Set");
            BtnAccept.UseVisualStyleBackColor = true;

            // Form components
            FormInputText.Controls.Add(BtnCancel);
            FormInputText.Controls.Add(BtnAccept);
            FormInputText.Controls.Add(TxtInput);

            // Form settings
            FormInputText.AcceptButton = BtnAccept;
            FormInputText.AutoScaleDimensions = new SizeF(6F, 13F);
            FormInputText.AutoScaleMode = AutoScaleMode.Font;
            FormInputText.CancelButton = BtnCancel;
            FormInputText.ClientSize = size;
            FormInputText.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            FormInputText.Icon = OmenMon.Resources.IconTray;
            FormInputText.MaximizeBox = false;
            FormInputText.MinimizeBox = false;
            FormInputText.Name = T_FRM + "InputText";
            FormInputText.SizeGripStyle = SizeGripStyle.Hide;
            FormInputText.StartPosition = FormStartPosition.CenterParent;
            FormInputText.Text = title;
            ApplyTheme(FormInputText);

            // Tool tips
            Tip.SetToolTip(TxtInput, Config.Locale.Get(Config.L_GUI_TIP + T_TXT + "Input"));
            Tip.SetToolTip(BtnAccept, Config.Locale.Get(Config.L_GUI_TIP + T_BTN + "Accept"));
            Tip.SetToolTip(BtnCancel, Config.Locale.Get(Config.L_GUI_TIP + T_BTN + "Cancel"));

            // Resume the layout
            FormInputText.ResumeLayout(false);

            // Show the form, and return the result
            try {

                // Return the value if the user accepted
                if(FormInputText.ShowDialog(owner) == DialogResult.OK)
                    return TxtInput.Text;

                // Return an empty string otherwise
                else
                    return "";

            // Clean up
            } finally {

                // Unregister the event handler
                TxtInput.Click -= EventTxtInputClick;

                // Dispose of the tool tip (not a form component)
                Tip.Dispose();

                // Dispose of the form
                FormInputText.Close();

            }

        }

        // Shows a dialog prompting for system reboot
        public static void ShowPromptReboot() {
            if(MessageBox.Show(Config.Locale.Get(Config.L_GUI + "PromptReboot"),
                Config.AppName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Asterisk,
                MessageBoxDefaultButton.Button1,
                (MessageBoxOptions) MB_SYSTEMMODAL) == DialogResult.Yes)

            // Proceed with restart if confirmed
            Os.RestartSystem();

        }

        // Restores a window and brings it to the front
        public static void ShowToFront(IntPtr window) {

            User32.ShowWindow(window, User32.SW_MINIMIZE);
            User32.ShowWindow(window, User32.SW_RESTORE);
            User32.ShowWindow(window, User32.SW_SHOWNORMAL);
            User32.SetForegroundWindow(window);
            User32.SwitchToThisWindow(window, false);

        }
#endregion

    }

}
