using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AutoReleaseTool
{
    public class ConfigMan
    {
        const string DBFile = "db.xml";

        static List<SvnMergeOperation> _preferMergeOps = null;
        static List<SvnRepository> _repositories = null;

        public static List<SvnMergeOperation> PreferMergeOps
        {
            get 
            {
                XDocument xDoc = XDocument.Load(DBFile);

                var query = from op in xDoc.Element("config").Element("preferMergeOps").Descendants("operation")
                             select new SvnMergeOperation
                             {
                                 Name = op.Attribute("name").Value,
                                 SrcRepository = FindRepository(op.Attribute("srcRepo").Value),
                                 SrcBranch = FindBranch(op.Attribute("srcRepo").Value, op.Attribute("srcBranch").Value),
                                 DestRepository = FindRepository(op.Attribute("destRepo").Value),
                                 DestBranch = FindBranch(op.Attribute("destRepo").Value, op.Attribute("destBranch").Value),
                             };

                _preferMergeOps = query.ToList();

                return _preferMergeOps;
            }

        }

        public static List<SvnRepository> Repositories
        {
            get
            {
                XDocument xDoc = XDocument.Load(DBFile);

                var query = from site in xDoc.Element("config").Element("repositories").Descendants("repository")
                            select new SvnRepository
                            {
                                Name = site.Attribute("name").Value,
                                Branches = (from branch in site.Descendants("branch")
                                            select new SvnBranch
                                            {
                                                Name = branch.Attribute("name").Value,
                                                URI = branch.Attribute("uri").Value,
                                                LocalPath = branch.Attribute("localPath").Value,
                                            }).ToList(),
                            };

                if (query != null)
                {
                    _repositories = query.ToList();
                }

                return _repositories;
            }

        }

        private static SvnBranch FindBranch(string repositoryName, string branchName)
        {
            SvnRepository repos = null;
            
            foreach (var r in _repositories)
            {
                if (r.Name.Equals(repositoryName, StringComparison.InvariantCultureIgnoreCase))
                {
                    repos = r;
                }
            }

            if (repos != null)
            {
                return repos.GetBranchByName(branchName);
            }
            else
            {
                return null;
            }
        }

        public static SvnRepository FindRepository(string repositoryName)
        {
            foreach(var site in _repositories)
            {
                if (site.Name.Equals(repositoryName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return site;
                }
            }

            return null;
        }
    }
}
