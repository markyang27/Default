using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoReleaseTool
{
    public class SvnMergeOperation
    {
        public string Name { get; set; }
        public SvnRepository SrcRepository { get; set; }
        public SvnBranch SrcBranch { get; set; }
        public SvnRepository DestRepository { get; set; }
        public SvnBranch DestBranch { get; set; }
    }
}
