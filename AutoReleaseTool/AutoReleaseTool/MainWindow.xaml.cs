using ARTCore;
using ARTCore.SvnModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoReleaseTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string IndentString = "----";
        const string MergeOpPlaceHolder = "--- Predefined Merge OPs ---";
        const string RepositoryPlaceHolder = "--- Repository ---";
        const string BranchPlaceHolder = "--- Branch ---";

        SvnMan _sm;

        private SynchronizationContext sc;

        private SolidColorBrush warningBrush = new SolidColorBrush(Color.FromRgb(99, 30, 30));

        BackgroundWorker _worker;

        //WinConfirmChangedPaths _cwin;
        //AutoResetEvent _areConfirmWin;
        //bool _confirmResult;

        public MainWindow()
        {
            InitializeComponent();

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            _sm = new SvnMan();
            _sm.WriteLog += LogForSvnMan;

            sc = SynchronizationContext.Current;

            LoadConfig();

            _worker = new BackgroundWorker();
            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
            _worker.ProgressChanged += worker_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
            _worker.WorkerReportsProgress = true;

            //_cwin = new WinConfirmChangedPaths();
            //_areConfirmWin = new AutoResetEvent(false);
            //_confirmResult = false;
        }

        private void LoadConfig()
        {
            SendOrPostCallback action = new SendOrPostCallback(obj =>
            {
                try
                {
                    //
                    List<string> repositories = new List<string>();

                    repositories.Add(RepositoryPlaceHolder);
                    foreach (SvnRepository site in ConfigMan.Repositories)
                    {
                        repositories.Add(site.Name);
                    }

                    cbSrcRepository.ItemsSource = repositories;
                    cbSrcRepository.SelectedIndex = 0;

                    cbDestRepository.ItemsSource = repositories;
                    cbDestRepository.SelectedIndex = 0;

                    //
                    List<SvnMergeOperation> predefinedMergeOps = new List<SvnMergeOperation>();
                    predefinedMergeOps.Add(new SvnMergeOperation { Name = MergeOpPlaceHolder });
                    foreach (SvnMergeOperation op in ConfigMan.PreferMergeOps)
                    {
                        predefinedMergeOps.Add(op);
                    }

                    cbPredefinedMergeOps.ItemsSource = predefinedMergeOps;
                    cbPredefinedMergeOps.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, string.Format("Failed to load configuration due to error: {0}", ex.Message));
                }
            });

            ExecOnUIThreadSync(action, null);
        }

        private void ReloadPage()
        {
            SendOrPostCallback action = new SendOrPostCallback(obj =>
            {
                cbSrcBranch_SelectionChanged(null, null);
                cbDestBranch_SelectionChanged(null, null);
            });

            ExecOnUIThreadSync(action, null);
        }

        private SvnMergeOperation GetSelectedMergeOP()
        {
            SvnMergeOperation selectedMergeOP = cbPredefinedMergeOps.SelectedItem as SvnMergeOperation;

            return selectedMergeOP;
        }

        private SvnRepository GetSelectedSrcRepository()
        {
            string selectedRepos = cbSrcRepository.SelectedItem as string;

            SvnRepository repos = ConfigMan.FindRepository(selectedRepos);

            return repos;
        }

        private SvnRepository GetSelectedDestRepository()
        {
            string selectedRepos = cbDestRepository.SelectedItem as string;

            SvnRepository repos = ConfigMan.FindRepository(selectedRepos);

            return repos;
        }

        private SvnBranch GetSelectedSrcBranch()
        {
            SvnBranch selectedSrcBranch = cbSrcBranch.SelectedItem as SvnBranch;

            return selectedSrcBranch;
        }

        private SvnBranch GetSelectedDestBranch()
        {
            SvnBranch selectedDestBranch = cbDestBranch.SelectedItem as SvnBranch;

            return selectedDestBranch;
        }

        private void Button_ReloadConfig_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void cbPredefinedMergeOps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SvnMergeOperation selectedMergeOP = GetSelectedMergeOP();

            if(selectedMergeOP != null)
            {
                if (selectedMergeOP.SrcRepository != null && cbSrcRepository.Items.Contains(selectedMergeOP.SrcRepository.Name))
                {
                    cbSrcRepository.SelectedItem = selectedMergeOP.SrcRepository.Name;
                    cbSrcBranch.SelectedItem = selectedMergeOP.SrcBranch;
                }

                if (selectedMergeOP.DestRepository != null && cbDestRepository.Items.Contains(selectedMergeOP.DestRepository.Name))
                {
                    cbDestRepository.SelectedItem = selectedMergeOP.DestRepository.Name;
                    cbDestBranch.SelectedItem = selectedMergeOP.DestBranch;
                }
            }
        }

        private void cbSrcBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SvnBranch selectedSrcBranch = GetSelectedSrcBranch();

            RefreshSrcBranchLogs(selectedSrcBranch);
        }

        private void cbDestBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SvnBranch selectedDestBranch = GetSelectedDestBranch();

            RefreshDestBranchLogs(selectedDestBranch);
        }

        private void RefreshSrcBranchLogs(SvnBranch branch)
        {
            if (branch != null)
            {
                this.lblSrcBranchName.Content = string.Format("Source Branch: {0} - [{1}]@{2}", branch.Name, branch.LocalPath, branch.URI);

                List<Log> logs;

                if (!string.IsNullOrEmpty(branch.LocalPath))
                {
                    logs = _sm.GetLogs(branch.LocalPath);
                }
                else
                {
                    logs = _sm.GetLogsFromUri(branch.URI);
                }

                this.dgSrcLogs.ItemsSource = logs;
            }
            else
            {
                this.dgSrcLogs.ItemsSource = null;
            }
        }

        private void RefreshDestBranchLogs(SvnBranch branch)
        {
            if (branch != null)
            {
                this.lblDestBranchName.Content = string.Format("Destination Branch: {0} - [{1}]@{2}", branch.Name, branch.LocalPath, branch.URI);

                List<Log> logs = _sm.GetLogs(branch.LocalPath);

                this.dgDestLogs.ItemsSource = logs;
            }
            else
            {
                this.dgDestLogs.ItemsSource = null;
            }
        }

        private void Button_Merge_Click(object sender, RoutedEventArgs e)
        {
            if (this.tbLog.Document.Blocks.Count > 200)
            {
                this.tbLog.Document.Blocks.Clear();
            }

            if (_worker.IsBusy)
            {
                _worker.CancelAsync();
                Log("Attempt to cancel merge operation...");
            }
            else
            {
                SvnRepository srcRepos = GetSelectedSrcRepository(); 
                SvnBranch srcBranch = GetSelectedSrcBranch();

                SvnRepository destRepos = GetSelectedDestRepository();
                SvnBranch destBranch = GetSelectedDestBranch();

                if (srcRepos.Name.Equals(destRepos.Name) &&
                    srcBranch.Name.Equals(destBranch.Name))
                {
                    MessageBox.Show("Destination repos/branch cannot be same with source repos/branch.");
                    return;
                }


                List<long> revisionsToMerge = new List<long>();

                foreach (var item in this.dgSrcLogs.SelectedItems)
                {
                    Log logItem = item as Log;

                    if (logItem != null)
                    {
                        revisionsToMerge.Add(logItem.Revision);
                    }
                }

                bool shouldReturn = false;

                if (srcRepos == null || srcBranch == null)
                {
                    shouldReturn = true;
                    MessageBox.Show("Source repository or branch cannot be null.");
                }
                else if (destRepos == null || destBranch == null)
                {
                    shouldReturn = true;
                    MessageBox.Show("Destination repository or branch cannot be null.");
                }
                else if (revisionsToMerge.Count == 0)
                {
                    shouldReturn = true;
                    MessageBox.Show("Please select revision(s) from source branch.");
                }
                else
                {
                    // 
                }

                if (shouldReturn)
                {
                    return;
                }

                string alertMsg = string.Format("Merge revision(s) {0} from {1}.{2} to {3}.{4}?", Format(revisionsToMerge), srcRepos.Name, srcBranch.Name, destRepos.Name, destBranch.Name);

                MessageBoxResult mbResult = MessageBox.Show(alertMsg, "Confirm", MessageBoxButton.OKCancel);

                if (mbResult == MessageBoxResult.OK)
                {
                    SetUIStatus(Status.Running);

                    Tuple<SvnRepository, SvnBranch, SvnRepository, SvnBranch, List<long>> args = new Tuple<SvnRepository, SvnBranch, SvnRepository, SvnBranch, List<long>>(
                        srcRepos, srcBranch, destRepos, destBranch, revisionsToMerge);
                    _worker.RunWorkerAsync(args);
                }
            }
        }

        private void SetUIStatus(Status status)
        {
            SendOrPostCallback action = new SendOrPostCallback(obj =>
            {
                switch (status)
                {
                    case Status.Running:
                        btnMerge.Content = "Merge in progress, click to abort.";
                        //btnMerge.Background.Opacity = 0.1;
                        cbPredefinedMergeOps.IsEnabled = false;
                        cbSrcRepository.IsEnabled = false;
                        cbDestRepository.IsEnabled = false;
                        cbSrcBranch.IsEnabled = false;
                        cbDestBranch.IsEnabled = false;
                        dgSrcLogs.IsEnabled = false;
                        dgDestLogs.IsEnabled = false;
                        break;

                    case Status.CannotAbort:
                        //btnMerge.IsEnabled = false;
                        break;

                    default:
                        btnMerge.Content = "Merge";
                        btnMerge.IsEnabled = true;
                        cbPredefinedMergeOps.IsEnabled = true;
                        cbSrcRepository.IsEnabled = true;
                        cbDestRepository.IsEnabled = true;
                        cbSrcBranch.IsEnabled = true;
                        cbDestBranch.IsEnabled = true;
                        dgSrcLogs.IsEnabled = true;
                        dgDestLogs.IsEnabled = true;
                        break;
                }
            });

            ExecOnUIThreadSync(action, null);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SendOrPostCallback action = new SendOrPostCallback(obj =>
            {
                this.pbar.Value = e.ProgressPercentage;
            });

            ExecOnUIThreadSync(action, null);


            
        }

        void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetUIStatus(Status.Default);
        }

        void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                BackgroundWorker worker = sender as BackgroundWorker;

                Tuple<SvnRepository, SvnBranch, SvnRepository, SvnBranch, List<long>> args = e.Argument as Tuple<SvnRepository, SvnBranch, SvnRepository, SvnBranch, List<long>>;
                SvnRepository srcRepos = args.Item1;
                SvnBranch srcBranch = args.Item2;
                SvnRepository destRepos = args.Item3;
                SvnBranch destBranch = args.Item4;
                List<long> revisions = args.Item5;

                string srcBranchShortName = srcBranch.Name;
                string destBranchShortName = destBranch.Name;
                string srcBranchLocalPath = srcBranch.LocalPath;
                string destBranchLocalPath = destBranch.LocalPath;

                // No need to update src local path, bece we get lastest files from svn server.
                List<string> affectedLocalPaths = _sm.GetAffectedDestLocalPaths(srcBranch.URI, destBranch.URI, destBranch.LocalPath, revisions);

                if (affectedLocalPaths == null || affectedLocalPaths.Count == 0)
                {
                    MessageBox.Show(string.Format("No change found from revision(s) {0} of source branch {1}.{2}", Format(revisions), srcRepos.Name, srcBranch.Name));

                    return;
                }

                try 
                {
                    if (ThinkAndContinue(100))
                    {
                        worker.ReportProgress(10);
                        Log("Revert destination paths...");
                        if (ThinkAndContinue(500))
                        {
                            if (CleanupAndRevertLocalPaths(affectedLocalPaths))
                            {
                                worker.ReportProgress(20);
                                Log("Updating destination paths...");
                                if (ThinkAndContinue(1000))
                                {
                                    if (_sm.Update(destBranchLocalPath)) // need to update target path
                                    {
                                        worker.ReportProgress(40);
                                        Log(string.Format("Merge revision(s) {0} from branch {1} to branch {2}", Format(revisions), srcBranchShortName, destBranchShortName));
                                        if (ThinkAndContinue(1000))
                                        {
                                            if (_sm.Merge(srcBranch.URI, destBranch.URI, destBranchLocalPath, revisions))
                                            {
                                                SetUIStatus(Status.CannotAbort);

                                                worker.ReportProgress(80);
                                                Log("Merge complete, now committing...");
                                                if (ThinkAndContinue(1200))
                                                {
                                                    StringBuilder sb = new StringBuilder();
                                                    sb.AppendFormat("Merged revision(s) {0} of branch {1}.{2} to branch {3}.{4}{5}", Format(revisions), srcRepos.Name, srcBranchShortName, destRepos.Name, destBranchShortName, Environment.NewLine);
                                                    sb.AppendLine("----------------------------------------------------");
                                                    sb.AppendLine(_sm.GetLogMessageOfRevisions(srcBranch.URI, revisions));
                                                    string comments = sb.ToString();

                                                    worker.ReportProgress(90);

                                                    string commitResult = _sm.Commit(destBranchLocalPath, affectedLocalPaths, comments, ConfirmChangePaths);

                                                    worker.ReportProgress(100);

                                                    long revisionAfterCommitted;
                                                    if (long.TryParse(commitResult, out revisionAfterCommitted))
                                                    {
                                                        Log(string.Format("Branch merge completed at revision {0}", revisionAfterCommitted));
                                                    }
                                                    else
                                                    {
                                                        Log(string.Format("Branch merge completed, but {0}", commitResult));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Log(LogLevel.Error, "Abort operation because we failed to merge local folders!");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log(LogLevel.Error, "Abort operation because we failed to update local folders!");
                                    }
                                }
                            }
                            else
                            {
                                Log(LogLevel.Error, "Abort operation because we failed to revert local folders!");
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log(LogLevel.Error, string.Format("Failed to merge, error: {0}", ex.Message));
                    if (ex.InnerException != null)
                    {
                        Log(LogLevel.Error, ex.InnerException.Message);
                    }

                    CleanupAndRevertLocalPaths(affectedLocalPaths);
                }

                ReloadPage();
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, string.Format("Merge failed due to error {0}", ex.Message));
                if (ex.InnerException != null)
                {
                    Log(LogLevel.Error, ex.InnerException.Message);
                }
            }

            Log("Done");
        }

        private bool ConfirmChangePaths(List<string> paths)
        {
            return true;

            if (paths == null)
            {
                throw new ArgumentNullException("paths cannot be null.");
            }

            bool confirmResult = false;

            SendOrPostCallback action = new SendOrPostCallback(obj =>
            {
                List<SvnNodeStatus> changePaths = _sm.GetStatusOfChangedPaths(paths);

                WinConfirmChangedPaths cwin = new WinConfirmChangedPaths();
                cwin.Owner = this;
                cwin.RenderPathTreeView(changePaths);
                cwin.ShowDialog();

                confirmResult = cwin.DialogResult == null ? false : cwin.DialogResult.Value;
            });

            ExecOnUIThreadSync(action, null);

            return confirmResult; 
        }

        private bool CleanupAndRevertLocalPaths(List<string> paths)
        {
            List<string> pathsToRevert = new List<string>();

            foreach (string path in paths)
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    pathsToRevert.Add(path);
                }
            }

            if (pathsToRevert.Count > 0)
            {
                if (_sm.Revert(pathsToRevert))
                {
                    foreach (string path in pathsToRevert)
                    {
                        Log(string.Format("Revert path '{0}' successfully.", path));
                    }
                }
                else
                {
                    foreach (string path in pathsToRevert)
                    {
                        Log(LogLevel.Warning, string.Format("Failed to revert paths '{0}'", path));
                    }

                    return false;
                }
            }
            else
            {
                Log("all paths to revert does not exist in target branch, skip revert.");
            }

            return true;
        }

        private bool ThinkAndContinue(int thinkTime)
        {
            //Thread.Sleep(thinkTime);
            
            if (_worker.CancellationPending)
            {
                CancelMerge();

                return false;
            }
            else
            {
                return true;
            }
        }

        private void CancelMerge()
        {
            Log(LogLevel.Warning, "Operation cancelled by user.");
        }

        private void Log(string msg)
        {
            WriteLog(LogVerbosityLevel.Normal, LogLevel.Information, msg);
        }

        private void LogForSvnMan(string msg)
        {
            WriteLog(LogVerbosityLevel.Normal, LogLevel.Warning, msg);
        }

        private void Log(LogLevel level, string msg)
        {
            WriteLog(LogVerbosityLevel.Normal, level, msg);
        }

        private void WriteLog(LogVerbosityLevel verbosityLevel, LogLevel level, string msg)
        {
            string formattedMsg;
            switch (verbosityLevel)
            {
                case LogVerbosityLevel.Normal:
                    formattedMsg = string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
                    break;

                case LogVerbosityLevel.Detailed:
                    formattedMsg = string.Format("{2}{0} {1}", DateTime.Now.ToString("HH:mm:ss"), msg, IndentString);
                    break;

                case LogVerbosityLevel.Diagnostic:
                    formattedMsg = string.Format("{2}{3}{0} {1}", DateTime.Now.ToString("HH:mm:ss"), msg, IndentString, IndentString);
                    break;

                default:
                    formattedMsg = string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
                    break;
            }

            Brush brush;
            switch (level)
            {
                case LogLevel.Information:
                    brush = Brushes.DarkBlue;
                    break;

                case LogLevel.Warning:
                    brush = warningBrush;
                    break;

                case LogLevel.Error:
                    brush = Brushes.Red;
                    break;

                default:
                    brush = Brushes.DarkBlue;
                    break;
            }

            sc.Send(new SendOrPostCallback((para) =>
            {
                // Append Mode
                this.tbLog.Document.Blocks.Add(
                         new Paragraph(
                            new Run(para as string)
                            {
                                Foreground = brush
                            }));

                

                this.tbLog.ScrollToEnd();

            }),
            formattedMsg);
        }

        private string Format(List<long> list)
        {
            StringBuilder sb = new StringBuilder();

            foreach (long v in list)
            {
                sb.AppendFormat("{0},", v);
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private string Format(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return "no content.";
            }

            StringBuilder sb = new StringBuilder();

            foreach (string v in list)
            {
                sb.AppendFormat("{0}{1}", v, Environment.NewLine);
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private object Format(List<SvnNodeStatus> changePaths)
        {
            if (changePaths == null || changePaths.Count == 0)
            {
                return "no content.";
            }

            StringBuilder sb = new StringBuilder();

            foreach (SvnNodeStatus v in changePaths)
            {
                sb.AppendFormat("{0}{1}", v.Format(), Environment.NewLine);
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private void ExecOnUIThreadSync(SendOrPostCallback action, object state)
        {
            sc.Send(action, state);
        }

        private void Button_OpenConfigFile_Click(object sender, RoutedEventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo 
            {
                FileName = "notepad.exe",
                Arguments = string.Format(@"{0}\db.xml", Environment.CurrentDirectory)
            };

            proc.Start();
            proc.WaitForExit();

            LoadConfig();
        }

        private void Button_OpenAppFolder_Click(object sender, RoutedEventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = string.Format(@"{0}", Environment.CurrentDirectory)
            };

            proc.Start();
        }

        private void lblSrcBranchName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SvnBranch branch = GetSelectedSrcBranch();

            if (branch != null)
            {
                Process proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = branch.LocalPath
                };

                proc.Start();
            }
        }

        private void lblDestBranchName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SvnBranch branch = GetSelectedDestBranch();

            if (branch != null)
            {
                Process proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = branch.LocalPath
                };

                proc.Start();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                ReloadPage();
            }
        }

        private void cbSrcRepository_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedRepos = cbSrcRepository.SelectedItem as string;

            SvnRepository repos = ConfigMan.FindRepository(selectedRepos);
            if (repos != null)
            {
                this.cbSrcBranch.ItemsSource = repos.Branches;

                SvnMergeOperation op = GetSelectedMergeOP();
                if (op != null)
                {
                    this.cbSrcBranch.SelectedItem = op.SrcBranch;
                }
            }
            else
            {
                this.cbSrcBranch.ItemsSource = null;
                this.lblSrcBranchName.Content = "Source Branch";
            }
        }

        

        private void cbDestRepository_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedRepos = cbDestRepository.SelectedItem as string;

            SvnRepository repos = ConfigMan.FindRepository(selectedRepos);
            if (repos != null)
            {
                this.cbDestBranch.ItemsSource = repos.Branches;

                SvnMergeOperation op = GetSelectedMergeOP();
                if (op != null)
                {
                    this.cbDestBranch.SelectedItem = op.DestBranch;
                }
            }
            else
            {
                this.cbDestBranch.ItemsSource = null;
                this.lblDestBranchName.Content = "Destination Branch";
            }
        }

    }

    public enum Status 
    {
        Default,
        Running,
        Pause,
        Stop,
        CannotAbort,
    }

    public enum LogVerbosityLevel
    {
        Normal = 0,
        Detailed = 1,
        Diagnostic = 2
    }

    public enum LogLevel
    {
        Information = 0,
        Warning = 1,
        Error = 2
    }
}
