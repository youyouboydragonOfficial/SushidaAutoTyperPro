using System;
using System.Runtime.InteropServices;

namespace SushidaAutoTyper.Services
{
    public enum VirtualKeys : ushort
    {
        Shift = 0x10,
        Control = 0x11,
        Alt = 0x12,
        Space = 0x20,
        Backspace = 0x08,
        Return = 0x0D
    }

    public class NativeKeyboard
    {
        #region Win32 API Definitions

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern short VkKeyScan(char ch);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        private const uint MAPVK_VK_TO_VSC = 0;

        #endregion

        /// <summary>
        /// Sends a character input.
        /// If useUnityHardwareMode is true, sends hardware scan code (compatible with Unity DirectInput & WebGL).
        /// </summary>
        public static void SendChar(char ch, bool useUnityHardwareMode = true)
        {
            short vkScan = VkKeyScan(ch);
            if (vkScan == -1)
            {
                // Unicode fallback
                SendUnicodeChar(ch);
                return;
            }

            byte vk = (byte)(vkScan & 0xFF);
            bool shiftNeeded = ((vkScan >> 8) & 1) != 0;

            if (shiftNeeded)
            {
                SendKeyDown((ushort)VirtualKeys.Shift, useUnityHardwareMode);
            }

            SendKeyDown(vk, useUnityHardwareMode);
            SendKeyUp(vk, useUnityHardwareMode);

            if (shiftNeeded)
            {
                SendKeyUp((ushort)VirtualKeys.Shift, useUnityHardwareMode);
            }
        }

        public static void SendKeyDown(ushort vk, bool useScanCode = true)
        {
            ushort scanCode = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT
            {
                wVk = useScanCode ? (ushort)0 : vk,
                wScan = scanCode,
                dwFlags = useScanCode ? KEYEVENTF_SCANCODE : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendKeyUp(ushort vk, bool useScanCode = true)
        {
            ushort scanCode = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT
            {
                wVk = useScanCode ? (ushort)0 : vk,
                wScan = scanCode,
                dwFlags = (useScanCode ? KEYEVENTF_SCANCODE : 0) | KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendUnicodeChar(char ch)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = ch,
                dwFlags = KEYEVENTF_UNICODE,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = ch,
                dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
