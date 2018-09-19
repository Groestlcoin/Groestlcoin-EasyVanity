﻿using Groestlcoin_VanityGen_UI.Code;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Groestlcoin_VanityGen_UI {

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        #region Public Fields

        public string CMDFile;
        public string OutputText = string.Empty;
        public string RunSettings;
        public string SaveFileDirectory = "";

        #endregion Public Fields

        #region Private Fields

        private readonly PerformanceCounter CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private readonly DispatcherTimer CpuTimer = new DispatcherTimer();

        private readonly FileSystemWatcher fileWatcher = new FileSystemWatcher(Home + @"\ProgFiles");

        private readonly BackgroundWorker Worker = new BackgroundWorker();
        private bool VanityGenFilesExist = false;

        #endregion Private Fields

        #region Public Constructors

        public MainWindow() {
            InitializeComponent();
            Titlebar.IsMainWindow = true;
            Worker.DoWork += WorkerOnDoWork;
            Worker.WorkerSupportsCancellation = true;

            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Changed += FileWatcherOnChanged;
            fileWatcher.Renamed += FileWatcherOnChanged;
            fileWatcher.Deleted += FileWatcherOnChanged;
            fileWatcher.Created += FileWatcherOnChanged;

            CheckFileExistance();

            DialogYesPrompt = new CommandImplementation(OnYesDialogClick);
            DialogNoPrompt = new CommandImplementation(OnNoDialogClick);

            //Check if the PC is connected to the internet, and show a warning if it is.
            if (NetworkInterface.GetIsNetworkAvailable()) {
                ShowStartDialog("This machine is currently connected to the internet! We would not advise creating or storing Groestlcoin wallets on a machine which is connected to the internet as it poses a risk\n\nAlthough we advise users to use the Generator offline we do allow the application to be run whilst connected to the internet.\n\nWould you like to continue?");
            }
            else {
                DialogHelper.ShowOKDialog(DH, "Online Status: Offline");
            }

            CpuTimer.Interval = new TimeSpan(0, 0, 0, 0, 1024);
            CpuTimer.IsEnabled = true;

            CpuTimer.Tick += (sender, args) => {
                var counterPercent = Convert.ToInt32(CpuCounter.NextValue());
                uxCPULbl.Text = counterPercent + "%";
            };

            Titlebar.Clicked += (sender, args) => ThemeSelector.ThemeSelector.SetCurrentThemeDictionary(this, new Uri((Titlebar.uxThemeSelector.SelectedItem as ComboBoxItem).Tag.ToString(), UriKind.Relative));
        }

        #endregion Public Constructors

        #region Private Delegates

        private delegate void SetTextCallback(string OutputLine);

        #endregion Private Delegates

        #region Public Properties

        public ICommand DialogNoPrompt { get; }
        public ICommand DialogYesPrompt { get; }
        public static string Home => AppDomain.CurrentDomain.BaseDirectory;

        #endregion Public Properties

        #region Public Methods

        public void ShowStartDialog(string message) {
            var card = new Card { Padding = new Thickness(32), Margin = new Thickness(16), Width = 350 };
            var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var textBlock = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };

            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var yesButton = new Button { Content = "YES", Width = 100 };
            var noButton = new Button { Content = "NO", Width = 100 };

            var btnstyle = Application.Current.FindResource("MaterialDesignFlatButton") as Style;
            if (btnstyle != null) {
                yesButton.Style = btnstyle;
                noButton.Style = btnstyle;
            }
            yesButton.Command = DialogYesPrompt;
            noButton.Command = DialogNoPrompt;

            stackPanel.Children.Add(textBlock);
            buttonStack.Children.Add(yesButton);
            buttonStack.Children.Add(noButton);
            stackPanel.Children.Add(buttonStack);
            card.Content = stackPanel;

            DH?.ShowDialog(card);
        }

        #endregion Public Methods

        #region Private Methods

        private bool CheckFileExistance() {
            var filesFound = File.Exists(Home + @"\ProgFiles\vanitygen.exe");

            if (!File.Exists(Home + @"\ProgFiles\oclvanitygen.exe")) {
                filesFound = false;
            }
            if (!File.Exists(Home + @"\ProgFiles\keyconv.exe")) {
                filesFound = false;
            }
            if (!File.Exists(Home + @"\ProgFiles\calc_addrs.cl")) {
                filesFound = false;
            }
            if (filesFound) {
                Dispatcher.Invoke(() => {
                    uxFileWatchBtn.BadgeBackground = Brushes.Green;
                    uxFileWatchBtn.Badge = "VanityGen Files Found";
                });
            }
            else {
                Dispatcher.Invoke(() => {
                    uxFileWatchBtn.BadgeBackground = Brushes.Red;
                    uxFileWatchBtn.Badge = "VanityGen Files Not Found";
                });
            }
            return filesFound;
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs e) {
            CheckFileExistance();
        }

        private void KillProcesses() {
            try {
                if (Worker.IsBusy) {
                    Worker.CancelAsync();
                }
                foreach (var prog in Process.GetProcesses()) {
                    if (prog.ProcessName == CMDFile.Replace(".exe", "")) {
                        prog.Kill();
                    }
                }
            }
            catch (Exception e) {
                //Ignored

                //MessageBox.Show(e.Message);
            }
            uxPhraseTxt.IsEnabled = true;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
            KillProcesses();
        }

        private void OnNoDialogClick(object obj) {
            Application.Current.Shutdown();
        }

        private void OnYesDialogClick(object obj) {
            DH.IsOpen = false;
        }
        private void SetSettings() {
            var sb = new StringBuilder();

            sb.Append("-v");

            if (uxCaseOptChk.IsChecked == false) sb.Append(" -i");
            if (uxKeepFindingOptChk.IsChecked == true) sb.Append(" -k");
            if (uxOutputKeysChk.IsChecked == true) {
                var dlg = new SaveFileDialog();
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";
                if (dlg.ShowDialog() == true) {
                    SaveFileDirectory = dlg.FileName;
                    sb.Append($" -o \"{SaveFileDirectory}\"");
                    //Set UI up for a successful run
                    uxViewFileBtn.IsEnabled = uxOutputKeysChk.IsChecked == true;
                }
                else {
                    return;
                }
            }
            RunSettings = sb.ToString();

            uxPhraseTxt.IsEnabled = false;
            uxPubKeyTxt.Text = string.Empty;
            uxPrivKeyTxt.Text = string.Empty;
            uxSecretPrivKey.Password = string.Empty;
            uxViewOutputBtn.IsEnabled = true;
            uxOutputTxt.Text = string.Empty;
            uxStartBtn.IsEnabled = false;
            uxStopBtn.IsEnabled = true;

            Worker.WorkerReportsProgress = true;
            Worker.RunWorkerAsync();
        }

        private void SetText(string outputLine) {
            string PublicKey;
            string PrivateKey;

            if (!string.IsNullOrEmpty(OutputText)) {
                OutputText += OutputText + Environment.NewLine;
            }

            var outputLineSplit = outputLine.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

            if (outputLineSplit.Length > 4) {
                Dispatcher.Invoke(() => {
                    uxKps.Text = outputLineSplit[0];
                    //  uxTlKeysLbl.Text = outputLineSplit[1].Replace("total", "");
                    uxProbLbl.Text = outputLineSplit[3];
                });
            }

            if (outputLine.Contains("Address: ")) {
                PublicKey = outputLine.Replace("Address: ", "");
                Dispatcher.Invoke(() => uxPubKeyTxt.Text = PublicKey);
            }
            else if (outputLine.Contains("Privkey: ")) {
                PrivateKey = outputLine.Replace("Privkey: ", "");
                Dispatcher.Invoke(() => {
                    uxPrivKeyTxt.Text = PrivateKey;
                    uxSecretPrivKey.Password = PrivateKey;
                });
            }
            Dispatcher.Invoke(() => uxOutputTxt.Text += outputLine + Environment.NewLine);
        }

        private bool TextAllowed(string s) {
            foreach (var c in s) {
                if (char.IsLetterOrDigit(c) || char.IsControl(c)) continue;
                return false;
            }
            return true;
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            try {
                this?.DragMove();
            }
            catch {
                //Ignored
            }
        }

        private void UxCopyBtn_OnClick(object sender, RoutedEventArgs e) {
            var text = uxPrivKeyTxt.Text;

            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            thread.Start();
            thread.Join();

            DialogHelper.ShowOKDialog(DH, "Private Key Copied to Clipboard");
        }

        private void UxOutputKeysChk_OnChecked(object sender, RoutedEventArgs e) {
            uxPrivKeyTxt.Visibility = Visibility.Collapsed;
            uxSecretPrivKey.Visibility = Visibility.Collapsed;
            uxCopyBtn.Visibility = Visibility.Collapsed;
            uxPwdToggleBtn.Visibility = Visibility.Collapsed;
            uxViewFileBtn.Visibility = Visibility.Visible;
        }

        private void UxOutputKeysChk_OnUnchecked(object sender, RoutedEventArgs e) {
            if (uxPwdToggleBtn.IsChecked == true) {
                uxPrivKeyTxt.Visibility = Visibility.Visible;
            }
            else {
                uxSecretPrivKey.Visibility = Visibility.Visible;
            }
            uxCopyBtn.Visibility = Visibility.Visible;
            uxPwdToggleBtn.Visibility = Visibility.Visible;
            uxViewFileBtn.Visibility = Visibility.Collapsed;
        }

        private void UxPhraseTxt_OnPasting(object sender, DataObjectPastingEventArgs e) {
            var s = (string)e.DataObject.GetData(typeof(string));
            if (!TextAllowed(s)) e.CancelCommand();
        }

        private void UxPhraseTxt_OnPreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !TextAllowed(e.Text);
        }

        private void UxPwdToggleBtn_OnChecked(object sender, RoutedEventArgs e) {
            uxPrivKeyTxt.Visibility = Visibility.Visible;
            uxSecretPrivKey.Visibility = Visibility.Collapsed;
        }

        private void UxPwdToggleBtn_OnUnchecked(object sender, RoutedEventArgs e) {
            uxPrivKeyTxt.Visibility = Visibility.Collapsed;
            uxSecretPrivKey.Visibility = Visibility.Visible;
        }

        private void UxSecretPrivKey_OnPreviewKeyDown(object sender, KeyEventArgs e) {
            //Make read-only
            e.Handled = true;
        }

        private void UxSecretPrivKey_OnPreviewTextInput(object sender, TextCompositionEventArgs e) {
            //Make read-only
            e.Handled = true;
        }

        private void UxStopBtn_OnClick(object sender, RoutedEventArgs e) {
            KillProcesses();
            uxPhraseTxt.IsEnabled = true;
            uxStartBtn.IsEnabled = true;
            uxStopBtn.IsEnabled = false;
        }

        private void UxStopStartBtn_OnClick(object sender, RoutedEventArgs e) {
            if (!CheckFileExistance()) {
                DialogHelper.ShowOKDialog(DH, $"Unable to start VanityGen. Please check that the VanityGen files exist in the installation directory and try again.{Environment.NewLine}{Environment.NewLine}The VanityGen files can be found here:{Environment.NewLine}https://github.com/Groestlcoin/vanitygen/releases");
            }

            if (!string.IsNullOrEmpty(uxPhraseTxt.Text)) {
                var illegalChar = string.Empty;
                var legalReplacement = string.Empty;

                var validChars = new[] { "W", "X", "Y", "Z" };
                var firstCharacter = uxPhraseTxt.Text[0];

                if (uxPhraseTxt.Text.Contains("0")) {
                    illegalChar = "0";
                    legalReplacement = "o";
                }
                else if (uxPhraseTxt.Text.Contains("O")) {
                    illegalChar = "O";
                    legalReplacement = "o";
                }
                else if (uxPhraseTxt.Text.Contains("I")) {
                    illegalChar = "I";
                    legalReplacement = "i";
                }
                else if (uxPhraseTxt.Text.Contains("l")) {
                    illegalChar = "l";
                    legalReplacement = "L";
                }
                //If the case sensitive option is checked, check to see if the first character is a valid uppercase character (WXYZ)
                else if (uxCaseOptChk.IsChecked == true || char.IsNumber(firstCharacter)) {
                    if (!char.IsLower(firstCharacter)) {
                        if (!App.StringExtensions.ContainsAny(firstCharacter.ToString(), validChars, StringComparison.Ordinal)) {
                            illegalChar = firstCharacter.ToString();
                            DialogHelper.ShowOKDialog(DH, $"Whoops! You have entered an illegal character \"{illegalChar}\". When using Case Sensitive characters, only lowercase letters, or uppercase 'W', 'X', 'Y' and 'Z' characters are valid for the first character.");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(illegalChar)) {
                    DialogHelper.ShowOKDialog(DH, $"Whoops! You have entered an illegal Base58 character \"{illegalChar}\". You could always try \"{legalReplacement}\" instead.");
                    return;
                }
            }
            if (Worker.IsBusy) {
                KillProcesses();
            }
            else {
                if (uxHwSelect.IsPressed) {
                    CMDFile = "oclvanitygen.exe";
                }
                else if (!uxHwSelect.IsPressed) {
                    CMDFile = "vanitygen.exe";
                }
            }
            SetSettings();
        }

        private void UxViewFileBtn_OnClick(object sender, RoutedEventArgs e) {
            if (!string.IsNullOrEmpty(SaveFileDirectory)) {
                Process.Start("explorer.exe", $"/select,\"{SaveFileDirectory}\"");
            }
        }

        private void WorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs) {
            using (var process = new Process()) {
                process.StartInfo.FileName = "cmd.exe";
                //process.StartInfo.Arguments = "/c";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false)) {
                    process.OutputDataReceived += (s, e) => {
                        if (e.Data == null) {
                            if (outputWaitHandle != null) outputWaitHandle.Set();
                        }
                        else {
                            SetText(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (s, e) => {
                        if (e.Data == null) {
                            if (errorWaitHandle != null) errorWaitHandle.Set();
                        }
                        else {
                            SetText(e.Data);
                        }
                    };

                    var phrase = string.Empty;

                    Dispatcher.Invoke(() => {
                        phrase = uxPhraseTxt.Text;
                        if (uxHwSelect.IsPressed == false) phrase = "F" + phrase;
                    });

                    process.Start();

                    process.StandardInput.WriteLine($"cd {Home}{@"\ProgFiles\"}");
                    process.StandardInput.WriteLine($"{CMDFile} {RunSettings} {phrase}");
                    process.StandardInput.Close();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(3600000) &&
                        outputWaitHandle.WaitOne(3600000) &&
                        errorWaitHandle.WaitOne(3600000)) {
                    }
                    else {
                        KillProcesses();
                    }
                    Dispatcher.Invoke(() => {
                        uxStartBtn.IsEnabled = true;
                        uxStopBtn.IsEnabled = false;
                        uxPhraseTxt.IsEnabled = true;
                    });
                }
            }
        }

        #endregion Private Methods
    }
}