using SharpSvn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARTCore.SvnModel
{
    public class UpdateResult
    {
        public UpdateResult()
        { }

        public UpdateResult(SvnUpdateResult res)
        {
            this.Success = true;
            this.HasResultMap = res.HasResultMap;
            this.HasRevision = res.HasRevision;
            this.Revision = res.Revision;
        }

        public override string ToString()
        {
            return string.Format("Success: {0}, update to revision {1}", Success, Revision);
        }


        public bool Success { get; set; }
        public bool HasResultMap { get; set; }
        public bool HasRevision { get; set; }
        public long Revision { get; set; }
    }
}
