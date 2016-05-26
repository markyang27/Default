using ARTCore.SvnModel;
using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace ARTCore
{
    public class SvnMan
    {
        const string slash = @"/";
        const string backSlash = @"\";

        public event Action<string> WriteLog;

        public bool Update(ICollection<string> localPaths)
        {
            if (localPaths == null || localPaths.Count == 0)
            {
                throw new ArgumentNullException("localPaths cannot be empty.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnUpdateArgs args = new SvnUpdateArgs();
                args.Depth = SvnDepth.Empty;

                SvnUpdateResult result;

                if (sc.Update(localPaths, args, out result))
                {
                    return true; 
                }
                else
                {
                    throw args.LastException;
                }
            }
        }


        public bool Update(string destBranchLocalPath)
        {
            if (string.IsNullOrEmpty(destBranchLocalPath))
            {
                throw new ArgumentNullException("destBranchLocalPath cannot be empty.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnUpdateArgs args = new SvnUpdateArgs();
                args.Depth = SvnDepth.Infinity;

                SvnUpdateResult result;

                if (sc.Update(destBranchLocalPath, args, out result))
                {
                    return true;
                }
                else
                {
                    throw args.LastException;
                }
            }
        }

        //public bool Revert(ICollection<string> paths)
        //{
        //    if (paths == null)
        //    {
        //        throw new ArgumentNullException("paths cannot be null.");
        //    }

        //    using (SvnClient sc = new SvnClient())
        //    {
        //        SvnRevertArgs args = new SvnRevertArgs();
        //        args.Depth = SvnDepth.Empty;
        //        args.ClearChangelists = true;

        //        return sc.Revert(paths, args);
        //    }
        //}

        public bool Revert(ICollection<string> paths)
        {
            //throw new Exception("revert does not work.");

            if (paths == null)
            {
                throw new ArgumentNullException("paths cannot be null.");
            }

            if (paths.Count > 0)
            {
                foreach (string path in paths)
                {
                    CleanUp(path);
                }

                using (SvnClient sc = new SvnClient())
                {
                    SvnRevertArgs args = new SvnRevertArgs();
                    args.Depth = SvnDepth.Children;
                    args.ClearChangelists = true;

                    List<string> sortedPaths = Sort(paths);

                    return sc.Revert(sortedPaths, args);
                }
            }

            return true;
        }

        private List<string> Sort(ICollection<string> paths)
        {
            SortedList<string, string> sl = new SortedList<string, string>();

            foreach (string path in paths)
            {
                sl.Add(path, path);
            }

            List<string> result = new List<string>();

            foreach (var v in sl.Values)
            {
                result.Insert(0, v);
            }

            return result;
        }

        private bool CleanUp(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                throw new ArgumentNullException("localPath cannot be null.");
            }

            using (SvnClient sc = new SvnClient())
            {
                if (IsDir(localPath) && sc.GetUriFromWorkingCopy(localPath) != null)
                {
                    SvnCleanUpArgs args = new SvnCleanUpArgs();
                    args.BreakLocks = true;
                    args.ClearDavCache = true;
                    args.IncludeExternals = true;
                    args.VacuumPristines = true;

                    return sc.CleanUp(localPath, args);
                }
                else
                {
                    return true;
                }
            }
        }

        private bool IsDir(string path)
        {
            return Directory.Exists(path);
        }

        public bool Merge(string srcBranchUri, string destBranchUri, string destLocalFolder, List<long> revisions)
        {
            if (string.IsNullOrEmpty(srcBranchUri))
            {
                throw new ArgumentNullException("srcURI cannot be null.");
            }
            if (string.IsNullOrEmpty(destBranchUri))
            {
                throw new ArgumentNullException("destURI cannot be null.");
            }
            if (string.IsNullOrEmpty(destLocalFolder))
            {
                throw new ArgumentNullException("destLocalFolder cannot be null.");
            }
            if (revisions == null || revisions.Count == 0)
            {
                throw new ArgumentNullException("revisions cannot be empty.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnTarget src = SvnTarget.FromString(srcBranchUri);
                SvnTarget dest = SvnTarget.FromString(destBranchUri);

                Collection<SvnRevisionRange> revisionRanges = new Collection<SvnRevisionRange>();
                List<long> revisionsFiltered = new List<long>();
                foreach (int r in revisions)
                {
                    if (BranchHasRevision(srcBranchUri, r))
                    {
                        if (!RevisionHasMerged(destBranchUri, srcBranchUri, r))
                        {
                            SvnRevisionRange range = new SvnRevisionRange(r - 1, r);
                            revisionRanges.Add(range);
                            revisionsFiltered.Add(r);
                        }
                        else
                        {
                            OnWriteLog(string.Format("Revision {0} has been merged, skip it.", r));
                        }
                    }
                    else
                    {
                        OnWriteLog(string.Format("Revision {0} does not exist in source branch {1}, skip it.", r, srcBranchUri));
                    }
                }

                if (revisionRanges.Count > 0)
                {
                    SvnMergeArgs mergeArgs = new SvnMergeArgs();
                    mergeArgs.Force = false;
                    mergeArgs.ThrowOnError = true;
                    mergeArgs.Conflict += mergeArgs_Conflict;
                    mergeArgs.Depth = SvnDepth.Infinity;

                    if (sc.Merge(destLocalFolder, src, revisionRanges, mergeArgs))
                    {
                        return true;
                    }
                    else
                    {
                        List<string> affectedPaths = GetAffectedDestLocalPaths(srcBranchUri, destBranchUri, destLocalFolder, revisions);
                        Revert(affectedPaths);

                        throw mergeArgs.LastException;
                    }
                }
                else
                {
                    OnWriteLog("No revision(s) need to be mreged, abort operation.");
                }

                return false;
            }
        }

        public string Commit(string destLocalPath, List<string> affectedPaths, string logMessage, Func<List<string>, bool> OnConfirmChangePaths)
        {
            if (string.IsNullOrEmpty(destLocalPath))
            {
                throw new ArgumentNullException("destLocalPath cannot be null.");
            }
            if (affectedPaths == null)
            {
                throw new ArgumentNullException("affectedPaths cannot be null.");
            }
            if (string.IsNullOrEmpty(logMessage))
            {
                throw new ArgumentNullException("logMessage cannot be null.");
            }

            if (affectedPaths.Count == 0)
            {
                return "no paths changed, abort commit operation.";
            }
            else
            {
                using (SvnClient sc = new SvnClient())
                {
                    // Fill in external dir paths.
                    SvnListChangeListArgs args1 = new SvnListChangeListArgs();
                    args1.Depth = SvnDepth.Infinity;

                    Collection<SvnListChangeListEventArgs> list;
                    if (sc.GetChangeList(destLocalPath, args1, out list))
                    {
                        if (list == null || list.Count == 0)
                        {
                            return string.Format("change list of dest folder '{0}' is empty, skip commit operation.", destLocalPath);
                        }

                        foreach (SvnListChangeListEventArgs item in list)
                        {
                            string strPath = item.Path.TrimEnd(backSlash.ToCharArray());

                            List<string> dirPaths = new List<string>();
                            foreach (string path in affectedPaths)
                            {
                                if (path.IndexOf(strPath) == 0)
                                {
                                    dirPaths.Add(strPath);
                                }
                            }

                            foreach (var path in dirPaths)
                            {
                                if (!affectedPaths.Contains(path))
                                {
                                    affectedPaths.Add(path);
                                }
                            }
                        }
                    }
                    else
                    {
                        OnWriteLog(string.Format("failed to get change list from dest folder '{0}'.", destLocalPath));
                    }

                    if (!OnConfirmChangePaths(affectedPaths))
                    {
                        Revert(affectedPaths);

                        return "operation aborted by user, because change list not confirmed.";
                    }
                    else
                    {
                        SvnCommitResult result = null;

                        SvnCommitArgs args = new SvnCommitArgs();
                        args.Depth = SvnDepth.Empty;
                        args.LogMessage = logMessage;
                        args.IncludeDirectoryExternals = true;
                        args.IncludeFileExternals = true;

                        if (sc.Commit(affectedPaths, args, out result))
                        {
                            if (result != null)
                            {
                                return result.Revision.ToString();
                            }
                            else
                            {
                                return "no new revision, because there is no changes since last commit.";
                            }
                        }
                        else
                        {
                            Revert(affectedPaths);

                            throw args.LastException;
                        }
                    }
                }
            }
        }

        public List<string> GetAffectedDestLocalPaths(string srcBranchUri, string destBranchUri, string destLocalPath, List<long> revisions)
        {
            List<string> paths = new List<string>();

            using (SvnClient sc = new SvnClient())
            {
                foreach (long r in revisions)
                {
                    SvnLogArgs args = new SvnLogArgs();
                    args.Range = new SvnRevisionRange(r, r);
                    args.RetrieveAllProperties = true;
                    args.RetrieveChangedPaths = true;

                    Collection<SvnLogEventArgs> logs;
                    if (sc.GetLog(new Uri(srcBranchUri), args, out logs))
                    {
                        foreach (var log in logs)
                        {
                            foreach (var path in log.ChangedPaths)
                            {
                                string realLocalPath = MapToLocalPath(path, srcBranchUri, destLocalPath);

                                if (!paths.Contains(realLocalPath))
                                {
                                    paths.Add(realLocalPath);
                                }
                            }
                        }

                    }
                    else
                    {
                        throw args.LastException;
                    }
                }
            }

            return paths;
        }

        public string GetLogMessageOfRevision(string srcBranch, long revisionNumber)
        {
            if (string.IsNullOrEmpty(srcBranch))
            {
                throw new ArgumentNullException("srcBranch cannot be null.");
            }
            if (revisionNumber < 1)
            {
                throw new ArgumentException("revisionNumber must greater than 1.");
            }

            using (SvnClient sc = new SvnClient())
            {
                if (BranchHasRevision(srcBranch, revisionNumber))
                {
                    SvnLogArgs args = new SvnLogArgs();
                    args.Range = new SvnRevisionRange(revisionNumber, revisionNumber);

                    Collection<SvnLogEventArgs> logs;
                    if (sc.GetLog(new Uri(srcBranch), args, out logs))
                    {
                        SvnLogEventArgs log = logs[0];

                        string message = string.Format("Revision #{0} by {1} at {2} {3} {4}", log.Revision, log.Author, log.Time.ToString(), Environment.NewLine, log.LogMessage);

                        return message;
                    }
                    else
                    {
                        throw args.LastException;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public string GetLogMessageOfRevisions(string srcBranch, List<long> revisions)
        {
            if (string.IsNullOrEmpty(srcBranch))
            {
                throw new ArgumentNullException("srcBranch cannot be null.");
            }
            if (revisions == null || revisions.Count == 0)
            {
                throw new ArgumentNullException("revisions cannot be empty.");
            }

            StringBuilder sb = new StringBuilder();

            foreach (long revision in revisions)
            {
                sb.AppendLine(GetLogMessageOfRevision(srcBranch, revision));
            }

            return sb.ToString();
        }

        public List<Log> GetLogs(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                throw new ArgumentNullException("localPath cannot be null.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnLogArgs args = new SvnLogArgs();
                args.Limit = 50;

                Collection<SvnLogEventArgs> logs;
                if (sc.GetLog(localPath, args, out logs))
                {
                    //return JsonConvert.SerializeObject(logs);

                    List<Log> rv = new List<Log>();

                    foreach (var log in logs)
                    {
                        rv.Add(new Log(log));
                    }

                    //return rv.FindAll(l => { return (l.Time.AddDays(7) > DateTime.Now); });

                    return rv;
                }
                else
                {
                    throw args.LastException;
                }
            }
        }

        public List<Log> GetLogsFromUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException("uri cannot be null.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnLogArgs args = new SvnLogArgs();
                args.Limit = 50;

                Collection<SvnLogEventArgs> logs;
                if (sc.GetLog(new Uri(uri), args, out logs))
                {
                    List<Log> rv = new List<Log>();

                    foreach (var log in logs)
                    {
                        rv.Add(new Log(log));
                    }

                    return rv;
                }
                else
                {
                    throw args.LastException;
                }
            }
        }

        public List<SvnNodeStatus> GetStatusOfChangedPaths(List<string> paths)
        {
            if (paths == null || paths.Count == 0)
            {
                throw new ArgumentNullException("paths cannot be null.");
            }

            List<SvnNodeStatus> sList = new List<SvnNodeStatus>();

            using (SvnClient sc = new SvnClient())
            {
                SvnStatusArgs args = new SvnStatusArgs();
                args.Depth = SvnDepth.Children;
                args.IgnoreExternals = true;
                args.RetrieveAllEntries = true;

                foreach (string path in paths)
                {
                    SvnNodeStatus s = new SvnNodeStatus();
                    s.FullPath = path;

                    Collection<SvnStatusEventArgs> statusList;
                    if (sc.GetStatus(path, args, out statusList))
                    {
                        if (statusList.Count >= 1)
                        {
                            SvnStatusEventArgs status = statusList[0];

                            s.FullPath = status.FullPath;
                            s.NodeKind = Enum.GetName(typeof(SvnNodeKind), status.NodeKind);
                            s.LocalNodeStatus = Enum.GetName(typeof(SvnStatus), status.LocalNodeStatus);
                            s.Versioned = status.Versioned;
                            s.Conflicted = status.Conflicted;
                        }
                    }
                    else
                    {
                        throw args.LastException;
                    }

                    sList.Add(s);
                }
            }

            return sList;
        }

        public long GetLastestRevision(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                throw new ArgumentNullException("localPath cannot be null.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnLogArgs args = new SvnLogArgs();
                Collection<SvnLogEventArgs> logs;
                if (sc.GetLog(localPath, args, out logs))
                {
                    if (logs.Count > 0)
                    {
                        return logs[0].Revision;
                    }
                }

                throw args.LastException;
            }
        }

        private void OnWriteLog(string msg)
        {
            if (this.WriteLog != null)
            {
                WriteLog(msg);
            }
        }

        private bool BranchHasRevision(string srcURI, long revision)
        {
            if (string.IsNullOrEmpty(srcURI))
            {
                throw new ArgumentNullException("localPath cannot be null.");
            }

            using (SvnClient sc = new SvnClient())
            {
                SvnLogArgs args = new SvnLogArgs();
                args.Range = new SvnRevisionRange(revision - 1, revision);
                args.RetrieveMergedRevisions = true;
                args.RetrieveAllProperties = true;

                Collection<SvnLogEventArgs> logs;
                if (sc.GetLog(new Uri(srcURI), args, out logs))
                {
                    foreach (SvnLogEventArgs log in logs)
                    {
                        if (log.Revision == revision)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    throw args.LastException;
                }
            }

            return false;
        }

        private bool RevisionHasMerged(SvnTarget target, SvnTarget source, long revision)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target cannot be null.");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source cannot be null.");
            }

            using (SvnClient sc = new SvnClient())
            {
                Uri targetReposRoot = sc.GetRepositoryRoot(new Uri(target.TargetName));
                Uri sourceReposRoot = sc.GetRepositoryRoot(new Uri(source.TargetName));

                if (targetReposRoot.AbsoluteUri.Equals(sourceReposRoot.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase))
                {
                    SvnMergesMergedArgs args = new SvnMergesMergedArgs();
                    args.Depth = SvnDepth.Infinity;
                    args.Range = new SvnRevisionRange(revision, revision);

                    Collection<SvnMergesMergedEventArgs> list;

                    if (sc.GetMergesMerged(target, source, args, out list))
                    {
                        foreach (SvnMergesMergedEventArgs l in list)
                        {
                            if (l.Revision == revision)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //
                    }
                }
                else
                {
                    OnWriteLog("Cross repository merge detected, skip checking merged revisions.");
                }

                return false;
            }

        }

        private void mergeArgs_Conflict(object sender, SvnConflictEventArgs e)
        {
            OnWriteLog("Conflict!!!");
            string msg = string.Format("Conflicts {0} occurred at file '{1}'", e.Conflict.ConflictReason, e.Conflict.FullPath);

            throw new Exception(msg);
        }

        private string MapToLocalPath(SvnChangeItem srcPath, string srcBranchUri, string destLocalPath)
        {
            using (SvnClient sc = new SvnClient())
            {
                Uri srcRepoRoot = sc.GetRepositoryRoot(new Uri(srcBranchUri));
                Uri localRepoRoot = sc.GetRepositoryRoot(destLocalPath);


                string strSrcRepoRoot = srcRepoRoot.AbsoluteUri;
                if (strSrcRepoRoot.EndsWith(slash))
                {
                    strSrcRepoRoot = strSrcRepoRoot.Substring(0, strSrcRepoRoot.Length - 1);
                }

                string strSrcRelativePath = srcPath.Path;
                if (strSrcRelativePath.StartsWith(slash))
                {
                    strSrcRelativePath = strSrcRelativePath.Substring(1, strSrcRelativePath.Length - 1);
                }

                string strDestLocalPath = destLocalPath;
                if (strDestLocalPath.EndsWith(backSlash))
                {
                    strDestLocalPath = strDestLocalPath.Substring(0, strDestLocalPath.Length - 1);
                }

                string srcFullPath = string.Format(@"{0}/{1}", strSrcRepoRoot, strSrcRelativePath);

                string destFullLocalPath = srcFullPath.Replace(srcBranchUri, destLocalPath);

                destFullLocalPath = destFullLocalPath.Replace(slash, backSlash).TrimEnd(backSlash.ToCharArray());

                return destFullLocalPath;
            }
        }



    }
}
