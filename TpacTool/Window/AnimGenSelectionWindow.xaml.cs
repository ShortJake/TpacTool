using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TpacTool
{
    public partial class AnimGenSelectionWindow : Window
	{
		public AnimGenSelectionWindow()
		{
			InitializeComponent();
		}

		private void CloseButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

        private void ConfirmButtonClick(object sender, RoutedEventArgs e)
        {
			DialogResult = true;
            Close();
        }
    }
}
