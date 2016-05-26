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
using System.Windows.Shapes;

namespace AutoReleaseTool
{
    /// <summary>
    /// Interaction logic for WinConfirmChangedPaths.xaml
    /// </summary>
    /// 
    public partial class WinConfirmChangedPaths : Window
    {
        public WinConfirmChangedPaths()
        {
            InitializeComponent();

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }

        public void RenderPathTreeView(List<SvnNodeStatus> paths)
        {
            if (paths != null)
            {
                this.tvPaths.RenderTree(paths);
            }
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
