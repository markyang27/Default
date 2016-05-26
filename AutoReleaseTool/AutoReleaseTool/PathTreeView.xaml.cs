using ARTCore.SvnModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for PathTreeView.xaml
    /// </summary>
    public partial class PathTreeView : UserControl
    {
        private List<SvnNodeStatus> _paths;

        TreeViewItem _root;

        private SolidColorBrush warningBrush = new SolidColorBrush(Color.FromRgb(99, 30, 30));

        public PathTreeView()
        {
            InitializeComponent();

            _root = new TreeViewItem();
            _root.Header = "Root";

            this.tvPaths.Items.Add(_root);
        }

        public void RenderTree(List<SvnNodeStatus> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths cannot be null");
            }

            foreach (SvnNodeStatus svnNode in paths)
            {
                TreeViewItem item = new TreeViewItem { Header = string.Format("[{0}] {1}", svnNode.LocalNodeStatus, svnNode.FullPath) };
    
                //switch (svnNode.NodeKind)
                //{
                //    case "File":
                //        break;

                //    case "Directory":
                //        break;

                //    default:
                //        break;
                //}


                Brush brush = Brushes.Black;

                switch (svnNode.LocalNodeStatus)
                {
                    case "Added":
                        brush = Brushes.DarkGreen;
                        break;

                    case "Deleted":
                        brush = Brushes.DarkRed;
                        break;

                    case "Modified":
                        brush = Brushes.DarkBlue;
                        break;

                    case "Conflicted":
                        brush = Brushes.DarkGreen;
                        break;

                    case "NotVersioned":
                        brush = Brushes.Blue;
                        break;

                    default:
                        brush = Brushes.Black;
                        break;
                }

                item.Foreground = brush;

                _root.Items.Add(item);
            }

            _root.ExpandSubtree();
        }

        //private void InsertNodeToTreeViewItem(SvnNodeStatus svnNode, TreeViewItem tvi)
        //{
        //    if (svnNode == null || tvi == null)
        //    {
        //        return;
        //    }
        //    else
        //    {
        //        TreeViewItem parentTVItem = FindParentTVItem(svnNode, tvi);

        //        parentTVItem.Items.Add(new TreeViewItem { Header = svnNode.FullPath });




        //        bool isParentItemFound = false;


        //        foreach (TreeViewItem item in tvi.Items)
        //        {
        //            string tv = item.Header.ToString();

        //            if (IsChildPath(tv, svnNode.FullPath))
        //            {
        //                isParentItemFound = true;

        //                item.Items.Add(new TreeViewItem { Header = svnNode.FullPath });
        //            }
        //        }

        //        if (!isParentItemFound)
        //        {
        //            tvi.Items.Add(new TreeViewItem { Header = svnNode.FullPath });
        //        }
        //    }
        //}

        //private TreeViewItem FindParentTVItem(SvnNodeStatus svnNode, TreeViewItem tvi)
        //{
        //    if (svnNode == null || tvi == null)
        //    {
        //        return null;
        //    }
        //    else
        //    {
        //        if(IsChildPath(tvi.Header.ToString(), svnNode.FullPath))
        //        {
        //            foreach (TreeViewItem item in tvi.Items)
        //            {
        //                if (IsChildPath(item.Header.ToString(), svnNode.FullPath))
        //                {
        //                    TreeViewItem i = FindParentTVItem(svnNode, item);

        //                    i = 
        //                }
        //                else if (IsChildPath(svnNode.FullPath, item.Header.ToString()))
        //                {
                            
        //                }
        //            }
        //        }
        //    }
        //}

        //private bool IsChildPath(string child, string parent)
        //{

        //}
    }
}
