using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;

namespace App_Muter_mk2
{
    public class InputEvents
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public int dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RAWMOUSE
        {
            [FieldOffset(0)]
            public ushort usFlags;
            [FieldOffset(4)]
            public ushort usButtonFlags;
            [FieldOffset(6)]
            public ushort usButtonData;
            [FieldOffset(8)]
            public uint ulRawButtons;
            [FieldOffset(12)]
            public int lLastX;
            [FieldOffset(16)]
            public int lLastY;
            [FieldOffset(20)]
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
        }

        public readonly Dictionary<MouseButtons, int> mButtonReference = new Dictionary<MouseButtons, int>()
        {
            { MouseButtons.Left, 1 },
            { MouseButtons.Right, 2 },
            { MouseButtons.Middle, 3 },
            { MouseButtons.XButton1, 4 },
            { MouseButtons.XButton2, 5 },
        };

        // Native methods and structures for RAWINPUT API
        private const int WM_INPUT = 0x00FF;
        private const int RIDEV_INPUTSINK = 0x00000100;

        // Constant for raw input device registration
        private const int RID_INPUT = 0x10000003;

        // Constants for raw input related values
        private const int RIM_TYPEMOUSE = 0;
        private const int RIDEV_REMOVE = 0x00000001;
        private const int RI_MOUSE_BUTTON_5_DOWN = 0x0100; // keep for comparison (working hex value for m5)
        private const int RI_MOUSE_BUTTON_5_UP = 0x0200; // keep for comparison (working hex value for m5)

        public bool mouse_active = false;
        public bool keyboard_active = false;
        public bool checking_for_values = false;

        public int current_mouse_button = 0;
        public Keys current_key = 0;

        public int RI_MOUSE_BUTTON_X_DOWN = 0;
        public int RI_MOUSE_BUTTON_X_UP = 0;

        public void AddHandler(ref Message m, ApplicationHandler _hApplication, SettingsHandler _hSettings)
        {
            if (m.Msg == WM_INPUT)
            {
                // Retrieve the raw input data
                uint dataSize = 0;
                GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref dataSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

                IntPtr data = Marshal.AllocHGlobal((int)dataSize);
                try
                {
                    if (GetRawInputData(m.LParam, RID_INPUT, data, ref dataSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>()) == dataSize)
                    {
                        RAWINPUT rawInput = Marshal.PtrToStructure<RAWINPUT>(data);
                        if (rawInput.header.dwType == RIM_TYPEMOUSE)
                        {
                            // Handle the raw mouse input
                            HandleRawMouseInput(rawInput.mouse, _hApplication);
                            GetUpDownValues(rawInput.mouse, _hSettings);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(data);
                }
            }
        }

        private void GetUpDownValues(RAWMOUSE mouse, SettingsHandler _hSettings)
        {
            if (!checking_for_values) return;
            if (RI_MOUSE_BUTTON_X_DOWN != 0 && RI_MOUSE_BUTTON_X_UP != 0) return;

            /*
             
            mouse.usButtonFlags will return 0 when nothing is happening
            when a button is pressed it will return a number (x1)
            when a button is released it will return a number (x2)
            IMPORTANT :: 0 < x1 < x2
            x1 and x2 will only be returned once when the state changed
            x1 and x2 will also be multiples of 2 and will be 2 ^ n (i haven't completely worked out n yet)
             
             */

            if (mouse.usButtonFlags != 0)
            {
                Debug.WriteLine("here " + mouse.usButtonFlags);
                if (RI_MOUSE_BUTTON_X_DOWN == 0)
                {
                    RI_MOUSE_BUTTON_X_DOWN = mouse.usButtonFlags;
                }

                if (mouse.usButtonFlags != RI_MOUSE_BUTTON_X_DOWN && RI_MOUSE_BUTTON_X_DOWN != 0)
                {
                    RI_MOUSE_BUTTON_X_UP = mouse.usButtonFlags;
                }

                if (RI_MOUSE_BUTTON_X_DOWN != 0 && RI_MOUSE_BUTTON_X_UP != 0)
                {
                    _hSettings.UpdateSettings("", "", 0.0f, RI_MOUSE_BUTTON_X_UP, RI_MOUSE_BUTTON_X_DOWN);
                    checking_for_values = false;
                }
            }

            Debug.WriteLine(mouse.usButtonFlags + " | " + RI_MOUSE_BUTTON_X_DOWN + " | " + RI_MOUSE_BUTTON_X_UP);
        }

        private void HandleRawMouseInput(RAWMOUSE mouse, ApplicationHandler _hApplication)
        {
            if (!mouse_active) return;

            // mouse down is 1 << (btn - 1)
            // mouse up is 1 << (btn + 2)

            //int down_value = 1 << (current_mouse_button + 3);
            //int up_value = 1 << (current_mouse_button + 4);

            if ((mouse.usButtonFlags & RI_MOUSE_BUTTON_X_DOWN) != 0)
            {
                Debug.WriteLine($"mouse button {current_mouse_button} is pressed.");
                //_hApplication.SetApplicationMute(true);
                _hApplication.SetApplicationVolume(); // set to target volume
            }
            if ((mouse.usButtonFlags & RI_MOUSE_BUTTON_X_UP) != 0)
            {
                Debug.WriteLine($"mouse button {current_mouse_button} is released.");
                //_hApplication.SetApplicationMute(false);
                _hApplication.ReturnApplicationToVolume(); // set to base volume
            }
        }

        public void RegisterRawInput(IntPtr _handle)
        {
            RAWINPUTDEVICE[] rawInputDevices = new RAWINPUTDEVICE[1];

            rawInputDevices[0].usUsagePage = 0x01; // HID_USAGE_PAGE_GENERIC
            rawInputDevices[0].usUsage = 0x02; // HID_USAGE_GENERIC_MOUSE
            rawInputDevices[0].dwFlags = RIDEV_INPUTSINK;
            rawInputDevices[0].hwndTarget = _handle;

            if (!RegisterRawInputDevices(rawInputDevices, (uint)rawInputDevices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                MessageBox.Show("Failed to register for raw input.");
            }
        }

        public void UnregisterRawInput()
        {
            RAWINPUTDEVICE[] rawInputDevices = new RAWINPUTDEVICE[1];

            rawInputDevices[0].usUsagePage = 0x01; // HID_USAGE_PAGE_GENERIC
            rawInputDevices[0].usUsage = 0x02; // HID_USAGE_GENERIC_MOUSE
            rawInputDevices[0].dwFlags = RIDEV_REMOVE;
            rawInputDevices[0].hwndTarget = IntPtr.Zero;

            if (!RegisterRawInputDevices(rawInputDevices, (uint)rawInputDevices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                MessageBox.Show("Failed to unregister from raw input.");
            }
        }
    }
}
