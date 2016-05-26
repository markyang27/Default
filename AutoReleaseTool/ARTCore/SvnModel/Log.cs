using SharpSvn;
using SharpSvn.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARTCore.SvnModel
{
    public class Log
    {
        public static List<Log> FromSvnLogEventArgsCollection(ICollection<SvnLogEventArgs> args)
        {
            List<Log> logs = new List<Log>();

            foreach (SvnLogEventArgs arg in args)
            {
                Log log = new Log(arg);
                logs.Add(log);
            }

            return logs;
        }

        public Log()
        { }

        public Log(SvnLogEventArgs arg)
        {
            if (arg != null)
            {
                this.Time = arg.Time;
                this.Revision = arg.Revision;
                this.Author = arg.Author;
                this.LogMessage = arg.LogMessage;
                this.ChangedPaths = arg.ChangedPaths;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}", Time.ToString(), Revision, Author, LogMessage);
        }

        public DateTime Time { get; set; }

        public long Revision { get; set; }

        public string Author { get; set; }

        public string LogMessage { get; set; }

        public SvnChangeItemCollection ChangedPaths { get; set; }
    }
}
