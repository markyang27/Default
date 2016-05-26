using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoReleaseTool
{
    public static class Model
    {
        static List<SvnRepository> _srcRepositories;
        static List<SvnRepository> _destRepositories;

        public static List<SvnRepository> SrcRepositories 
        {
            get 
            {
                if (_srcRepositories == null)
                {
                    _srcRepositories = ConfigMan.Repositories;
                }

                return _srcRepositories;
            }
        }

        public static List<SvnRepository> DestRepositories
        {
            get
            {
                if (_destRepositories == null)
                {
                    _destRepositories = ConfigMan.Repositories;
                }

                return _destRepositories;
            }
        }
    }
}
