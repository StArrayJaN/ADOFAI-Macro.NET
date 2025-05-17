// MainWindow.xaml.cs

using System.Diagnostics;
using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using ADOFAI_Macro.Source.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using WinRT.Interop;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ADOFAI_Macro.Source.Views
{
    public sealed partial class MainWindow
    {
        private static readonly ResourceLoader _resourceLoader = new();
        private GlobalKeyboardListener _keyboardListener = new();
        private ADOFAI _adofai = new();
        private FileWatcher _watcher;
        private string _filePath = "";
        private LevelUtils.StartMacro? _macro;
        public MainWindow()
        {
            Thread.CurrentThread.Name = "Main";
            InitializeComponent();
            ChangeOpenLastMenuItem();
            Title = "ADOFAI-Macro";
#if DEBUG
            AppUtils.LogInfo($"{Title}已启动,窗口大小:X:{800},Y:{600}");
#endif
            AppUtils.Log.AutoLogException();
            Application.Current.UnhandledException += UnhandledException;
            AppWindow.Resize(new(800, 600));
            StartListen();
            Keys.Text = AppData.instance.Read("keyList", "123456");
        }

        void StartListen()
        {
            _keyboardListener.ListenKeyCombination(VirtualKey.Control, VirtualKey.C, delegate
            {
                if (IsCurrentWindowActive()) Application.Current.Exit();
            });
            _keyboardListener.ListenAllKeys(key =>
            {
                if (_macro == null) return;
                switch (key)
                {
                    case VirtualKey.W:
                        _macro.Offset = 0;
                        if (_adofai.IsWindowActive()) _macro.Start();
                        break;
                    case VirtualKey.Left:
                        _macro.Offset -= 2;
                        break;
                    case VirtualKey.Right:
                        _macro.Offset += 2;
                        break;
                    case VirtualKey.Escape:
                        _macro.Stop();
                        break;
                }
            });
#if DEBUG
            AppUtils.LogInfo("正在监听所有按键");
#endif
        }

        async void UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            e.Handled = true;
            ExceptionTextBox.Text = ex.ToString();
            AppUtils.LogError("发生错误:", ex.ToString());
            ExceptionGrid.Visibility = Visibility.Visible;
            await Task.Delay(10000);
            ExceptionGrid.Visibility = Visibility.Collapsed;
        }

        private bool IsCurrentWindowActive()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            IntPtr activeHwnd = WindowsNative.GetForegroundWindow();
            return (hWnd == activeHwnd);
        }

        private async Task ShowMessage(string title, object message)
        {
            await new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            }.ShowAsync();
        }
        #region 控件事件

        private async void OpenFileItem_Click(object sender, RoutedEventArgs e)
        {
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".adofai");

            // 获取窗口句柄
            var windowHandle = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                _filePath = file.Path;
                _watcher?.Stop();
                _watcher = new FileWatcher(_filePath, FileChanged);
                _watcher.Start();
#if DEBUG
                AppUtils.LogInfo($"选择文件:{file.Path}");
#endif
                AppData.instance.Write("lastFile", file.Path);
                ContentTextBox.Text = GetString();
                StartButton.IsEnabled = true;
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        // 开始按钮点击事件
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            // 如果没有选择文件，弹出提示
            if (_filePath.Length == 0)
            {
                await ShowMessage(_resourceLoader.GetString("MainWindow_ErrorMessageTitle"),
                    _resourceLoader.GetString("MainWindow_NullFileMessage"));
                return;
            }

            // 如果没有输入键位，弹出提示
            if (string.IsNullOrWhiteSpace(Keys.Text))
            {
                await ShowMessage(_resourceLoader.GetString("MainWindow_ErrorMessageTitle"),
                    _resourceLoader.GetString("MainWindow_NullKeysMessage"));
                return;
            }

            // 初始化关卡
            ADOFAI.Level level = _adofai.InitLevel(_filePath);
            Process process = _adofai.FindOrRunProcess();
            _macro?._command?.Clear();
            if (CheckBox.IsChecked is false) _macro?._command?.Dispose();
            AppData.instance.Write("keyList", Keys.Text);
            ChangeOpenLastMenuItem();
            _macro = new LevelUtils.StartMacro(Keys.Text.ToCharArray().Select(KeyCodeConverter.GetKeyCode).ToList(),
                LevelUtils.GetNoteTimes(level), CheckBox.IsChecked ?? false);
            Toast("ADOFAI-Macro",
                $"{_resourceLoader.GetString("MainWindow_ReadyMessage")}: {Keys.Text.ToCharArray().Select(KeyCodeConverter.GetKeyCode).ToList().Count}");
#if DEBUG
            AppUtils.LogInfo("启动宏，键位:" + Keys.Text);
#endif
        }

        private void OpenLastFileItem_Click(object sender, RoutedEventArgs e)
        {
            _filePath = AppData.instance.Read("lastFile", "").Replace("\r", "");
            if (_filePath.Length != 0)
            {
                _watcher?.Stop();
                _watcher = new FileWatcher(_filePath, FileChanged);
                _watcher.Start();
                ContentTextBox.Text = GetString();
                StartButton.IsEnabled = true;
            }
        }

        #endregion
        
        private void FileChanged()
        {
            _macro?.ChangeNoteTimes(LevelUtils.GetNoteTimes(_adofai.InitLevel(_filePath)));
        }

        private string GetString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(
                value: $"{_resourceLoader.GetString("MainWindow_CurrentFileMessage")}: {_filePath}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage0")}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage1")}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage2")}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage3")}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage4")}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage5")}");
            stringBuilder.AppendLine(value: $"{_resourceLoader.GetString("MainWindow_RunTipMessage6")}");
            return stringBuilder.ToString();
        }

        private void ChangeOpenLastMenuItem()
        {
            OpenLast.IsEnabled = !string.IsNullOrEmpty(AppData.instance.Read("lastFile", "").Replace("\r", ""));
        }

        public static void Toast(params string[] args)
        {
            ToastContentBuilder builder = new();
            foreach (string arg in args)
            {
                builder.AddText(arg);
            }

            builder.Show();
        }
    }
}