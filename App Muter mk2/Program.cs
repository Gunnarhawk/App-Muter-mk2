using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace App_Muter_mk2
{
    public class Program : Form
    {
        private Label lState;
        private Label lApp;
        private Label lBind;
        private Label lVolume;
        private Label lTutoral;
        private Button bEnable;
        private Button bDisable;
        private Button bBind;
        private ComboBox cbApp;
        private NumericUpDown nudVolume;

        // handler initialized and used throughout the appliaction (passed as nessessary through functions)
        public InputEvents eInput;

        // handler initialized and used throughout the appliaction (passed as nessessary through functions)
        public SettingsHandler hSettings;

        // handler initialized and used throughout the appliaction (passed as nessessary through functions)
        private ApplicationHandler hApplication;
        bool listening = false;

        public Program()
        {
            // init mouse event handler
            eInput = new InputEvents();
            hSettings = new SettingsHandler();
            hApplication = new ApplicationHandler(hSettings.app);

            InitializeComponent();

            // Program_Load is called after InitializeComponent
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Program());
        }

        private void Program_Load(object sender, EventArgs e)
        {
            string b = hSettings.bind;
            string a = hSettings.app;
            float t = hSettings.t_vol;

            int m_btn = 0;
            Keys key = 0;

            if (!string.IsNullOrWhiteSpace(b))
            {
                bBind.Text = b;
                if (b.Split(' ')[0] == "Mouse")
                {
                    Int32.TryParse(Regex.Match(b, @"\d+").Value, out m_btn);
                    eInput.mouse_active = true;
                }
                else
                {
                    Enum.TryParse<Keys>(b, out key);
                    eInput.keyboard_active = true;
                }
            }

            cbApp.DropDown += CbApp_DropDown;
            cbApp.SelectedValueChanged += CbApp_SelectedValueChanged;

            cbApp.Items.AddRange(hApplication.GetProcessList().ToArray());
            if (!string.IsNullOrWhiteSpace(a))
            {
                cbApp.SelectedItem = a;
            }

            nudVolume.Value = (decimal)t;
            nudVolume.ValueChanged += NudVolume_ValueChanged;

            eInput.current_mouse_button = m_btn;
            eInput.current_key = key;
            eInput.RI_MOUSE_BUTTON_X_UP = hSettings.u_mask;
            eInput.RI_MOUSE_BUTTON_X_DOWN = hSettings.d_mask;

            Debug.WriteLine($"{key} | {m_btn} | {a} | {hSettings.u_mask} | {hSettings.d_mask}");

            RegisterProcessHandles(true);
        }

        // Override the WndProc method to capture raw input messages
        protected override void WndProc(ref Message m)
        {
            eInput.AddHandler(ref m, hApplication, hSettings);
            base.WndProc(ref m);
        }

        private void bEnable_OnClick(object sender, EventArgs e)
        {
            lState.Text = "Audio Muter Active";
            if (eInput.current_mouse_button != 0)
            {
                eInput.mouse_active = true;
            }
            else if (eInput.current_key != 0)
            {
                eInput.keyboard_active = true;
            }
            else
            {
                Debug.WriteLine("bEnable_OnClick :: Both key and mouse values are 0");
            }
        }

        private void bDisable_OnClick(object sender, EventArgs e)
        {
            lState.Text = "Audio Muter NOT Active";
            eInput.mouse_active = false;
            eInput.keyboard_active = false;
        }

        private void RegisterProcessHandles(bool startup = false)
        {
            eInput.UnregisterRawInput();

            Thread.Sleep(150);

            if (startup)
            {
                eInput.RegisterRawInput(Handle);
            }

            if (eInput.current_mouse_button != 0)
            {
                eInput.RegisterRawInput(Handle); // Register for low-level mouse input
            }
            else if (eInput.current_key != 0)
            {
                // Register for low-level keyboard input
            }
            else
            {
                Debug.WriteLine("RegisterProcessHandles :: Both key and mouse values are 0");
            }
        }

        private void bBind_OnClick(object sender, EventArgs e)
        {
            lTutoral.Enabled = true;
            bBind.Text = "[PRESS ANY BUTTON]";
            eInput.RI_MOUSE_BUTTON_X_DOWN = 0;
            eInput.RI_MOUSE_BUTTON_X_UP = 0;

            listening = true;
            eInput.checking_for_values = true;

            bBind.KeyDown += BTN_KeyDown;
            this.MouseDown += Form_MouseDown;
        }

        private void BTN_KeyDown(object sender, KeyEventArgs e)
        {
            if (!listening) return;

            MessageBox.Show("Keyboard keys are currently not implemented, only mouse buttons. Sorry");
            return;

            eInput.current_key = e.KeyCode;
            bBind.Text = e.KeyCode.ToString();

            hSettings.UpdateSettings(e.KeyCode.ToString());

            listening = false;
            eInput.checking_for_values = false;

            lTutoral.Enabled = false;
            eInput.keyboard_active = true;

            RegisterProcessHandles();

            this.MouseDown -= Form_MouseDown;
            bBind.KeyDown -= BTN_KeyDown;
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (!listening) return;

            int buttonNumber = 0;
            eInput.mButtonReference.TryGetValue(e.Button, out buttonNumber);

            if (buttonNumber == 0) buttonNumber = (int)e.Button - 1;

            if (buttonNumber == 0) MessageBox.Show("Unsupported mouse button. Please try again!");

            eInput.current_mouse_button = buttonNumber;
            bBind.Text = $"Mouse Button {buttonNumber}";
            hSettings.UpdateSettings($"Mouse Button {buttonNumber}");

            listening = false;
            // eInput.checking_for_values set to false in GetUpDownValues

            lTutoral.Enabled = false;
            eInput.mouse_active = true;

            RegisterProcessHandles();

            bBind.KeyDown -= BTN_KeyDown;
            this.MouseDown -= Form_MouseDown;
        }

        private void CbApp_DropDown(object sender, EventArgs e)
        {
            cbApp.Items.Clear();
            cbApp.Items.AddRange(hApplication.GetProcessList().ToArray());
        }

        private void CbApp_SelectedValueChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("here");
            hSettings.UpdateSettings("", cbApp.Text, 0.0f, 0, 0, hApplication);
        }

        private void NudVolume_ValueChanged(object sender, EventArgs e)
        {
            Console.WriteLine((float)nudVolume.Value);
            hSettings.UpdateSettings("", "", (float)nudVolume.Value, 0, 0, hApplication);
        }

        ~Program()
        {
            eInput.UnregisterRawInput();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            eInput.UnregisterRawInput(); // Unregister from low-level mouse input
        }

        private void InitializeComponent()
        {
            this.lState = new System.Windows.Forms.Label();
            this.bEnable = new System.Windows.Forms.Button();
            this.bDisable = new System.Windows.Forms.Button();
            this.lApp = new System.Windows.Forms.Label();
            this.lBind = new System.Windows.Forms.Label();
            this.cbApp = new System.Windows.Forms.ComboBox();
            this.bBind = new System.Windows.Forms.Button();
            this.lVolume = new System.Windows.Forms.Label();
            this.nudVolume = new System.Windows.Forms.NumericUpDown();
            this.lTutoral = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudVolume)).BeginInit();
            this.SuspendLayout();
            // 
            // lState
            // 
            this.lState.AutoSize = true;
            this.lState.Location = new System.Drawing.Point(12, 9);
            this.lState.Name = "lState";
            this.lState.Size = new System.Drawing.Size(97, 13);
            this.lState.TabIndex = 0;
            this.lState.Text = "Audio Muter Active";
            // 
            // bEnable
            // 
            this.bEnable.Location = new System.Drawing.Point(15, 39);
            this.bEnable.Name = "bEnable";
            this.bEnable.Size = new System.Drawing.Size(75, 23);
            this.bEnable.TabIndex = 1;
            this.bEnable.Text = "Enable";
            this.bEnable.UseVisualStyleBackColor = true;
            this.bEnable.Click += new System.EventHandler(this.bEnable_OnClick);
            // 
            // bDisable
            // 
            this.bDisable.Location = new System.Drawing.Point(96, 39);
            this.bDisable.Name = "bDisable";
            this.bDisable.Size = new System.Drawing.Size(75, 23);
            this.bDisable.TabIndex = 2;
            this.bDisable.Text = "Disable";
            this.bDisable.UseVisualStyleBackColor = true;
            this.bDisable.Click += new System.EventHandler(this.bDisable_OnClick);
            // 
            // lApp
            // 
            this.lApp.AutoSize = true;
            this.lApp.Location = new System.Drawing.Point(15, 160);
            this.lApp.Name = "lApp";
            this.lApp.Size = new System.Drawing.Size(59, 13);
            this.lApp.TabIndex = 6;
            this.lApp.Text = "Application";
            // 
            // lBind
            // 
            this.lBind.AutoSize = true;
            this.lBind.Location = new System.Drawing.Point(12, 118);
            this.lBind.Name = "lBind";
            this.lBind.Size = new System.Drawing.Size(28, 13);
            this.lBind.TabIndex = 8;
            this.lBind.Text = "Bind";
            // 
            // cbApp
            // 
            this.cbApp.FormattingEnabled = true;
            this.cbApp.Location = new System.Drawing.Point(15, 176);
            this.cbApp.Name = "cbApp";
            this.cbApp.Size = new System.Drawing.Size(156, 21);
            this.cbApp.TabIndex = 9;
            // 
            // bBind
            // 
            this.bBind.BackColor = System.Drawing.SystemColors.Window;
            this.bBind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bBind.Location = new System.Drawing.Point(15, 134);
            this.bBind.Name = "bBind";
            this.bBind.Size = new System.Drawing.Size(156, 23);
            this.bBind.TabIndex = 10;
            this.bBind.Text = "[click to set]";
            this.bBind.UseVisualStyleBackColor = false;
            this.bBind.Click += new System.EventHandler(this.bBind_OnClick);
            // 
            // lVolume
            // 
            this.lVolume.AutoSize = true;
            this.lVolume.Location = new System.Drawing.Point(15, 200);
            this.lVolume.Name = "lVolume";
            this.lVolume.Size = new System.Drawing.Size(61, 13);
            this.lVolume.TabIndex = 11;
            this.lVolume.Text = "Set Volume";
            // 
            // nudVolume
            // 
            this.nudVolume.Location = new System.Drawing.Point(15, 216);
            this.nudVolume.Name = "nudVolume";
            this.nudVolume.Size = new System.Drawing.Size(156, 20);
            this.nudVolume.TabIndex = 12;
            // 
            // lTutoral
            // 
            this.lTutoral.AutoSize = true;
            this.lTutoral.Enabled = false;
            this.lTutoral.Location = new System.Drawing.Point(15, 69);
            this.lTutoral.Name = "lTutoral";
            this.lTutoral.Size = new System.Drawing.Size(89, 39);
            this.lTutoral.TabIndex = 13;
            this.lTutoral.Text = "1. Wait a second\r\n2. Press and hold\r\n3. Let go";
            // 
            // Program
            // 
            this.ClientSize = new System.Drawing.Size(188, 249);
            this.Controls.Add(this.lTutoral);
            this.Controls.Add(this.nudVolume);
            this.Controls.Add(this.lVolume);
            this.Controls.Add(this.bBind);
            this.Controls.Add(this.cbApp);
            this.Controls.Add(this.lBind);
            this.Controls.Add(this.lApp);
            this.Controls.Add(this.bDisable);
            this.Controls.Add(this.bEnable);
            this.Controls.Add(this.lState);
            this.Name = "Program";
            this.Load += new System.EventHandler(this.Program_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudVolume)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
