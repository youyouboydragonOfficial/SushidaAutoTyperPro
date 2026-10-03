using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SushidaAutoTyper.Services;

namespace SushidaAutoTyper
{
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID_START = 9001;
        private const int HOTKEY_ID_PAUSE = 9002;
        private const int HOTKEY_ID_STOP = 9003;

        private const uint VK_F8 = 0x77;
        private const uint VK_F9 = 0x78;
        private const uint VK_F10 = 0x79;

        private HwndSource? _hwndSource;
        private readonly OcrEngineService _ocrEngineService;

        private Int32Rect _selectedRegion;
        private bool _isRegionSelected = false;

        private CancellationTokenSource? _typingCancellationTokenSource;
        private bool _isRunning = false;
        private bool _isPaused = false;
        private int _typedCharCount = 0;
        private string _lastTypedText = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            _ocrEngineService = new OcrEngineService();
            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;

            Log("🚀 SushidaAutoTyper Pro が起動しました。[F8] キーでタイピング連続自動実行を開始できます。");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource?.AddHook(HwndHook);

            NativeKeyboard.RegisterHotKey(handle, HOTKEY_ID_START, 0, VK_F8);
            NativeKeyboard.RegisterHotKey(handle, HOTKEY_ID_PAUSE, 0, VK_F9);
            NativeKeyboard.RegisterHotKey(handle, HOTKEY_ID_STOP, 0, VK_F10);

            Log("キーボードフック登録完了: [F8] 開始 | [F9] 一時停止 | [F10] 緊急停止");

            // Check if Desktop shortcut already exists
            CheckDesktopShortcutPrompt();
        }

        private void CheckDesktopShortcutPrompt()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "SushidaAutoTyper Pro.lnk");

                if (!File.Exists(shortcutPath))
                {
                    ShortcutPromptBanner.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        private void CreateShortcutYes_Click(object sender, RoutedEventArgs e)
        {
            bool success = CreateDesktopShortcut();
            ShortcutPromptBanner.Visibility = Visibility.Collapsed;

            if (success)
            {
                Log("✅ ホーム画面（デスクトップ）にショートカットを作成しました！次回からいつでも簡単に起動できます。");
            }
            else
            {
                Log("⚠️ ショートカット作成に失敗しました。");
            }
        }

        private void CreateShortcutNo_Click(object sender, RoutedEventArgs e)
        {
            ShortcutPromptBanner.Visibility = Visibility.Collapsed;
            Log("ホーム画面へのショートカット追加をスキップしました。");
        }

        private bool CreateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "SushidaAutoTyper Pro.lnk");
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                if (string.IsNullOrEmpty(exePath)) return false;

                string workDir = Path.GetDirectoryName(exePath) ?? "";

                string psScript = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}');" +
                                  $"$s.TargetPath='{exePath}';" +
                                  $"$s.WorkingDirectory='{workDir}';" +
                                  $"$s.Description='Sushida & Typing Game Auto-Typer Pro';" +
                                  $"$s.Save()";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit();
                return File.Exists(shortcutPath);
            }
            catch
            {
                return false;
            }
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            NativeKeyboard.UnregisterHotKey(handle, HOTKEY_ID_START);
            NativeKeyboard.UnregisterHotKey(handle, HOTKEY_ID_PAUSE);
            NativeKeyboard.UnregisterHotKey(handle, HOTKEY_ID_STOP);
            _hwndSource?.RemoveHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_START)
                {
                    Dispatcher.Invoke(() => StartAutoTyping());
                    handled = true;
                }
                else if (id == HOTKEY_ID_PAUSE)
                {
                    Dispatcher.Invoke(() => TogglePauseAutoTyping());
                    handled = true;
                }
                else if (id == HOTKEY_ID_STOP)
                {
                    Dispatcher.Invoke(() => StopAutoTyping());
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        #region Region Selection & OCR Test

        private void SelectRegionBtn_Click(object sender, RoutedEventArgs e)
        {
            OverlayWindow overlay = new OverlayWindow();
            if (overlay.ShowDialog() == true && overlay.IsRegionSelected)
            {
                _selectedRegion = overlay.SelectedRegion;
                _isRegionSelected = true;
                RegionStatusText.Text = $"選択範囲: ({_selectedRegion.X}, {_selectedRegion.Y}) | サイズ: {_selectedRegion.Width} x {_selectedRegion.Height}";
                Log($"画面領域を設定しました: X={_selectedRegion.X}, Y={_selectedRegion.Y}, W={_selectedRegion.Width}, H={_selectedRegion.Height}");

                TestOcr();
            }
        }

        private void TestOcrBtn_Click(object sender, RoutedEventArgs e)
        {
            TestOcr();
        }

        private async void TestOcr()
        {
            if (!_isRegionSelected)
            {
                Log("⚠️ まず「🎯 領域を選択」ボタンからタイピング枠を選択してください。");
                return;
            }

            try
            {
                using var bmp = _ocrEngineService.CaptureScreenRegion(_selectedRegion.X, _selectedRegion.Y, _selectedRegion.Width, _selectedRegion.Height);
                if (bmp != null)
                {
                    CapturedScreenImage.Source = ConvertBitmapToBitmapImage(bmp);
                }

                bool forceLower = ForceLowercaseCheckBox.IsChecked == true;
                var (raw, romaji) = await _ocrEngineService.RecognizeScreenRegionAsync(
                    _selectedRegion.X, _selectedRegion.Y, _selectedRegion.Width, _selectedRegion.Height, true, forceLower);

                string resultText = string.IsNullOrEmpty(romaji) ? (string.IsNullOrEmpty(raw) ? "(文字が検出されませんでした)" : raw) : romaji;
                OcrPreviewBox.Text = resultText;
                Log($"🔍 OCRテスト結果: 原文='{raw}', 抽出Text='{romaji}' ({resultText.Length} 文字)");
            }
            catch (Exception ex)
            {
                Log($"⚠️ テストエラー: {ex.Message}");
            }
        }

        #endregion

        #region Auto-Typing Logic

        private void StartBtn_Click(object sender, RoutedEventArgs e) => StartAutoTyping();
        private void PauseBtn_Click(object sender, RoutedEventArgs e) => TogglePauseAutoTyping();
        private void StopBtn_Click(object sender, RoutedEventArgs e) => StopAutoTyping();

        private void StartAutoTyping()
        {
            if (_isRunning) return;

            _isRunning = true;
            _isPaused = false;
            _typedCharCount = 0;
            _lastTypedText = string.Empty;
            UpdateTypedCountDisplay();

            StartBtn.IsEnabled = false;
            PauseBtn.IsEnabled = true;
            StopBtn.IsEnabled = true;

            _typingCancellationTokenSource = new CancellationTokenSource();
            var token = _typingCancellationTokenSource.Token;

            if (_isRegionSelected && AutoFocusCheckBox.IsChecked == true)
            {
                bool focused = NativeKeyboard.FocusTargetWindow(_selectedRegion.X + 10, _selectedRegion.Y + 10);
                if (focused)
                {
                    Log("🎯 ゲーム画面にフォーカスを自動移動しました。");
                }
            }

            int selectedTab = 0;
            Dispatcher.Invoke(() =>
            {
                TabControl? tc = FindVisualChild<TabControl>(this);
                if (tc != null) selectedTab = tc.SelectedIndex;
            });

            if (selectedTab == 0)
            {
                if (!_isRegionSelected)
                {
                    Log("⚠️ エラー: 画面OCR領域が選択されていません。「🎯 領域を選択」ボタンを押して枠を指定してください。");
                    StopAutoTyping();
                    return;
                }
                Log("▶ [OCR連続自動連打モード] 無制限連続タイピングを開始しました (停止はF10)");
                Task.Run(() => RunOcrAutoTypingLoop(token), token);
            }
            else
            {
                string textToType = string.Empty;
                Dispatcher.Invoke(() => textToType = CustomScriptInput.Text);

                if (string.IsNullOrWhiteSpace(textToType))
                {
                    Log("⚠️ エラー: 自動送信するテキストが入力されていません。");
                    StopAutoTyping();
                    return;
                }
                Log("▶ [カスタムテキストモード] 高速自動タイピングを開始しました (F8)");
                Task.Run(() => RunCustomTextTypingLoop(textToType, token), token);
            }
        }

        private void TogglePauseAutoTyping()
        {
            if (!_isRunning) return;
            _isPaused = !_isPaused;
            PauseBtn.Content = _isPaused ? "▶ 再開 (F9)" : "⏸ 一時停止 (F9)";
            Log(_isPaused ? "⏸ 自動タイピングを一時停止しました (F9)" : "▶ 自動タイピングを再開しました (F9)");
        }

        private void StopAutoTyping()
        {
            if (!_isRunning) return;

            _typingCancellationTokenSource?.Cancel();
            _isRunning = false;
            _isPaused = false;

            StartBtn.IsEnabled = true;
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            PauseBtn.Content = "⏸ 一時停止 (F9)";

            Log("⏹ 自動タイピングを停止しました (F10)");
        }

        private async Task RunOcrAutoTypingLoop(CancellationToken token)
        {
            int scanInterval = 80;
            InputMethodMode inputMode = InputMethodMode.CombinedVkScan;
            bool forceLower = true;

            while (!token.IsCancellationRequested && _isRunning)
            {
                if (_isPaused)
                {
                    await Task.Delay(100, token);
                    continue;
                }

                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        scanInterval = (int)ScanIntervalSlider.Value;
                        int selectedModeIndex = InputModeComboBox.SelectedIndex;
                        inputMode = selectedModeIndex switch
                        {
                            1 => InputMethodMode.HardwareScanCode,
                            2 => InputMethodMode.LegacyKeybdEvent,
                            _ => InputMethodMode.CombinedVkScan
                        };
                        forceLower = ForceLowercaseCheckBox.IsChecked == true;
                    });

                    using var capturedBmp = _ocrEngineService.CaptureScreenRegion(
                        _selectedRegion.X, _selectedRegion.Y, _selectedRegion.Width, _selectedRegion.Height);

                    if (capturedBmp != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            CapturedScreenImage.Source = ConvertBitmapToBitmapImage(capturedBmp);
                        });
                    }

                    var (rawText, targetText) = await _ocrEngineService.RecognizeScreenRegionAsync(
                        _selectedRegion.X, _selectedRegion.Y, _selectedRegion.Width, _selectedRegion.Height, true, forceLower);

                    if (!string.IsNullOrWhiteSpace(targetText))
                    {
                        Dispatcher.Invoke(() => OcrPreviewBox.Text = targetText);

                        if (targetText != _lastTypedText)
                        {
                            _lastTypedText = targetText;
                            Log($"⚡ 検出・打鍵中 ({targetText.Length}文字): '{targetText}'");

                            int delay = 20;
                            Dispatcher.Invoke(() => delay = (int)DelaySlider.Value);

                            foreach (char ch in targetText)
                            {
                                if (token.IsCancellationRequested || !_isRunning || _isPaused) break;

                                NativeKeyboard.SendChar(ch, inputMode);
                                Interlocked.Increment(ref _typedCharCount);
                                Dispatcher.Invoke(UpdateTypedCountDisplay);

                                if (delay > 0)
                                {
                                    await Task.Delay(delay, token);
                                }
                            }

                            await Task.Delay(80, token);
                            _lastTypedText = string.Empty;
                        }
                    }
                    else
                    {
                        _lastTypedText = string.Empty;
                    }
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    Log($"OCRループ例外: {ex.Message}");
                }

                await Task.Delay(scanInterval, token);
            }
        }

        private async Task RunCustomTextTypingLoop(string text, CancellationToken token)
        {
            int delay = 20;
            InputMethodMode inputMode = InputMethodMode.CombinedVkScan;
            bool humanize = false;

            Dispatcher.Invoke(() =>
            {
                delay = (int)DelaySlider.Value;
                int selectedModeIndex = InputModeComboBox.SelectedIndex;
                inputMode = selectedModeIndex switch
                {
                    1 => InputMethodMode.HardwareScanCode,
                    2 => InputMethodMode.LegacyKeybdEvent,
                    _ => InputMethodMode.CombinedVkScan
                };
                humanize = HumanizerCheckBox.IsChecked == true;
            });

            Random rnd = new Random();

            foreach (char ch in text)
            {
                if (token.IsCancellationRequested || !_isRunning) break;

                while (_isPaused && !token.IsCancellationRequested)
                {
                    await Task.Delay(100, token);
                }

                NativeKeyboard.SendChar(ch, inputMode);
                Interlocked.Increment(ref _typedCharCount);
                Dispatcher.Invoke(UpdateTypedCountDisplay);

                int currentDelay = delay;
                if (humanize && delay > 10)
                {
                    currentDelay += rnd.Next(-8, 9);
                    currentDelay = Math.Max(1, currentDelay);
                }

                if (currentDelay > 0)
                {
                    await Task.Delay(currentDelay, token);
                }
            }

            Dispatcher.Invoke(() => StopAutoTyping());
        }

        #endregion

        #region UI Event Handlers & Helpers

        private void ScanIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ScanIntervalValText != null)
                ScanIntervalValText.Text = $"{ (int)e.NewValue } ms";
        }

        private void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DelayValText != null)
            {
                int delayMs = (int)e.NewValue;
                int cpm = delayMs > 0 ? 60000 / delayMs : 60000;
                DelayValText.Text = $"{delayMs} ms ({cpm:N0} CPM)";
            }
        }

        private void ConvertKanaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CustomScriptInput.Text))
            {
                string converted = RomajiConverter.ConvertToRomaji(CustomScriptInput.Text);
                CustomScriptInput.Text = converted;
                Log("ひらがな文をローマ字に自動変換しました。");
            }
        }

        private void ClearTextBtn_Click(object sender, RoutedEventArgs e)
        {
            CustomScriptInput.Clear();
        }

        private void UpdateTypedCountDisplay()
        {
            TypedCountText.Text = $"{_typedCharCount:N0} chars";
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogConsole.AppendText($"[{timestamp}] {message}\n");
                LogConsole.ScrollToEnd();
            });
        }

        private BitmapImage ConvertBitmapToBitmapImage(Bitmap src)
        {
            using MemoryStream ms = new MemoryStream();
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Position = 0;
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
        {
            if (obj == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t)
                    return t;
                else if (child != null)
                {
                    T? childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        #endregion
    }
}