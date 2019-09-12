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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace NewDefExtractor.GUI
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
		public string Test { get { return "This is string"; } }
        public MainWindow()
        {
            InitializeComponent();
			this.DataContext = this;
        }

		public void LoadMods()
		{
			string WorkshopFolderPath = ProgramConfig.instance.WorkshopFolderPath;
			string LocalFolderPath = ProgramConfig.instance.LocalFolderPath;

			List<DirectoryInfo> ModFolders = new List<DirectoryInfo>();
			Directory.GetDirectories(WorkshopFolderPath).ToList().ForEach(item =>
			{
				DirectoryInfo info = new DirectoryInfo(item);
				ModFolders.Add(info);
			});

			Directory.GetDirectories(LocalFolderPath).ToList().ForEach(item =>
			{
				DirectoryInfo info = new DirectoryInfo(item);
				ModFolders.Add(info);
			});

		}
    }
}
