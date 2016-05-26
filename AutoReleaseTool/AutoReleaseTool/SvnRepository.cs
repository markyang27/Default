using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoReleaseTool
{
    public class SvnRepository
    {
        public string Name { get; set; }

        public List<SvnBranch> Branches { get; set; }

        public SvnBranch GetBranchByName(string branchName)
        {
            foreach (var branch in Branches)
            {
                if (branch.Name.Equals(branchName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return branch;
                }
            }

            return null;
        }
    }
}
