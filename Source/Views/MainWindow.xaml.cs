// MainWindow.xaml.cs

using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using ABI.Microsoft.UI.Xaml.Documents;
using ABI.Windows.UI.Notifications;
using ADOFAI_Macro.Source.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml.Input;
using WinRT;
using WinRT.Interop;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;

namespace ADOFAI_Macro.Source.Views
{
    public sealed partial class MainWindow : Window
    {
        private GlobalKeyboardListener _keyboardListener = new();
        private ADOFAI _adofai = new();
        private string _filePath = "";
        private LevelUtils.StartMacro _macro;
        public MainWindow()
        {
            InitializeComponent();
            Title = "ADOFAI-Macro";
            Application.Current.UnhandledException += UnhandledException;
            _keyboardListener.ListenKeyCombination(VirtualKey.Control,VirtualKey.C, delegate
            {
                if (IsCurrentWindowActive()) Application.Current.Exit();
            });
            AppWindow.Resize(new (550,400));
            _keyboardListener.ListenAllKeys(key =>
            {
                if (_macro == null) return;
                switch (key)
                {
                    case VirtualKey.W :
                        _macro.Offset = 0;
                        _macro.Start();
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
            Keys.Text = AppData.instance.Read("keyList", "123456");
        }
        async void UnhandledException(object sender,Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            e.Handled = true;
            ExceptionTextBox.Text = ex.ToString();
            ExceptionGrid.Visibility = Visibility.Visible;
            await Task.Delay(10000);
            ExceptionGrid.Visibility = Visibility.Collapsed;
        }
        
        public bool IsCurrentWindowActive()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            IntPtr activeHwnd = WindowsNative.GetForegroundWindow();
            return (hWnd == activeHwnd);
        }

        private async Task ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "关闭"
            };
            await dialog.ShowAsync();
        }

        private async Task ShowMessage(string title,object message)
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
                    AppData.instance.Write("lastFile",file.Path);
                    ContentTextBox.Text = GetString();
                    StartButton.IsEnabled = true;
                }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_filePath.Length == 0)
            {
                ShowMessage("错误", "请选择文件");
                return;
            }
            if (string.IsNullOrEmpty(Keys.Text))
            {
                ShowMessage("错误", "请输入键位");
                return;
            }

            ADOFAI.Level level = _adofai.InitLevel(_filePath);
            Process process = _adofai.FindOrRunProcess();
            _macro?._command?.Clear();
            if (!CheckBox.IsChecked.Value) _macro?._command?.Dispose(); 
            AppData.instance.Write("keyList", Keys.Text);
            _macro = new LevelUtils.StartMacro(Keys.Text.ToCharArray().Select(KeyCodeConverter.GetKeyCode).ToList(), LevelUtils.GetNoteTimes(level),CheckBox.IsChecked.Value);
            Toast("ADOFAI-Macro", $"已就绪,键位数：{Keys.Text.ToCharArray().Select(KeyCodeConverter.GetKeyCode).ToList().Count}");
            WindowsNative.SwitchToThisWindow(process.MainWindowHandle);
        }
        
        private void OpenLastFileItem_Click(object sender, RoutedEventArgs e)
        {
            _filePath = AppData.instance.Read("lastFile", "").Replace("\r", "");
            if (_filePath.Length != 0)
            {
                ContentTextBox.Text = GetString();
                StartButton.IsEnabled = true;
            }
        }
        #endregion

        private string GetString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"当前文件:{_filePath}");
            stringBuilder.AppendLine("点击下方按钮运行，之后会自动切换到游戏窗口");
            stringBuilder.AppendLine("播放后，在第一个轨道按W开始，按←和→可以调整偏移");
            stringBuilder.AppendLine("按Esc停止,停止之后可以按W重新开始");
            stringBuilder.AppendLine("你可以再次选择文件来切换关卡");
            stringBuilder.AppendLine("启用控制台输出可能会导致性能下降");
            stringBuilder.AppendLine("按Ctrl+C或右上角退出程序");
            return stringBuilder.ToString();
        }

        private void Toast(params string[] args)
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