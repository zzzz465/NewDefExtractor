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
using System.Windows.Forms;

namespace NewDefExtractor.GUI
{
	/// <summary>
	/// ConfigWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ConfigWindow : Window
	{
		public ConfigWindow()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var btn = sender as System.Windows.Controls.Button;
			FolderBrowserDialog dialog = new FolderBrowserDialog();
			dialog.ShowDialog();
			btn.DataContext = dialog.SelectedPath;
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			ProgramConfig.instance.LocalFolderPath = LocalFolderButton.DataContext as string;
			ProgramConfig.instance.WorkshopFolderPath = WorkshopFolderButton.DataContext as string;
			this.Close();
		}
	}
}
