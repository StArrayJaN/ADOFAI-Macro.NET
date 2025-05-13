using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.System;

namespace ADOFAI_Macro.Source.Utils
{
    public class GlobalKeyboardListener : IDisposable
    {
        // Import necessary Windows API functions
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private bool _listenAllKeys = false;
        private VirtualKey? _specificKey = null;
        // TODO: 可能需要处理null
        private Action<VirtualKey>? _keyboardCallback;
        private Dictionary<VirtualKey, Action> _combinationCallbacks = new Dictionary<VirtualKey, Action>();
        private HashSet<VirtualKey> _currentlyPressedKeys = new HashSet<VirtualKey>();

        /// <summary>
        /// Initializes a new instance of the GlobalKeyboardListener
        /// </summary>
        public GlobalKeyboardListener()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// Starts listening for all keyboard keys
        /// </summary>
        /// <param name="callback">Callback to execute when any key is pressed</param>
        public void ListenAllKeys(Action<VirtualKey> callback)
        {
            if (_hookId != IntPtr.Zero)
                Unhook();

            _listenAllKeys = true;
            _specificKey = null;
            _keyboardCallback = callback;
            _hookId = SetHook(_proc);
        }

        /// <summary>
        /// Starts listening for a specific keyboard key
        /// </summary>
        /// <param name="key">The specific key to listen for</param>
        /// <param name="callback">Callback to execute when the specific key is pressed</param>
        public void ListenSpecificKey(VirtualKey key, Action<VirtualKey> callback)
        {
            if (_hookId != IntPtr.Zero)
                Unhook();

            _listenAllKeys = false;
            _specificKey = key;
            _keyboardCallback = callback;
            _hookId = SetHook(_proc);
        }

        /// <summary>
        /// Registers a callback for a key combination (modifier + key)
        /// </summary>
        /// <param name="modifier">The modifier key (Ctrl, Alt, Shift, etc.)</param>
        /// <param name="key">The main key</param>
        /// <param name="callback">Callback to execute when the combination is pressed</param>
        public void ListenKeyCombination(VirtualKey modifier, VirtualKey key, Action callback)
        {
            if (_hookId == IntPtr.Zero)
            {
                _hookId = SetHook(_proc);
            }

            _combinationCallbacks[key] = callback;
        }

        /// <summary>
        /// Stops listening for keyboard events
        /// </summary>
        public void Unhook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                // TODO: 我也不知道null咋搞就返回了个默认值，记得改QAQ
                if (curModule != null) {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
                return default;
            }
        }

        private bool IsKeyPressed(VirtualKey key)
        {
            return (GetKeyState((int)key) & 0x8000) != 0;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                VirtualKey key = (VirtualKey)vkCode;

                // Update currently pressed keys set
                if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
                {
                    _currentlyPressedKeys.Add(key);
                }
                else if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
                {
                    _currentlyPressedKeys.Remove(key);
                }

                // Check for key combinations first (higher priority)
                if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && _combinationCallbacks.ContainsKey(key))
                {
                    // Check all modifier keys
                    foreach (var modifier in new[] { VirtualKey.Control, VirtualKey.Menu, VirtualKey.Shift, VirtualKey.LeftWindows })
                    {
                        if (IsKeyPressed(modifier) && _combinationCallbacks.TryGetValue(key, out var callback))
                        {
                            callback.Invoke();
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);
                        }
                    }
                }

                // If no combination was pressed, check for single keys
                if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && 
                    (_listenAllKeys || (_specificKey.HasValue && key == _specificKey.Value)) &&
                    _keyboardCallback != null)
                {
                    // TODO: 可能需要处理null
                    _keyboardCallback.Invoke(key);
                }
            }

             return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _combinationCallbacks.Clear();
                }

                Unhook();
                disposedValue = true;
            }
        }

        ~GlobalKeyboardListener()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}