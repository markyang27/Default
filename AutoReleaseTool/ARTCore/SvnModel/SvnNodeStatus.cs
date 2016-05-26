using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARTCore.SvnModel
{
    public class SvnNodeStatus
    {
        public string FullPath { get; set; }

        public string NodeKind { get; set; }

        public string LocalNodeStatus { get; set; }

        public bool Versioned { get; set; }

        public bool Conflicted { get; set; }

        public string Format()
        {
            return string.Format("[{0}] {1}", LocalNodeStatus, FullPath);
        }
    }
}
