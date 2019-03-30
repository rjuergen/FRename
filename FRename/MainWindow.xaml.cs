using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace FRename
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool renameing = false;
        private RenameThread rename;
        private long renamed = 0;

        public MainWindow()
        {
            InitializeComponent();

            //txtSource.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        private void btnSelectSource_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.SelectedPath = txtSource.Text;
            bool? b = dialog.ShowDialog();
            if (b.HasValue && b.Value)
                txtSource.Text = dialog.SelectedPath;
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            renamed = 0;
            txtState.Content = "";
            renameing = !renameing;

            if (renameing)
            {
                btnRename.Content = "Stop";
                txtProgressBar.Visibility = System.Windows.Visibility.Visible;
                txtSource.IsEnabled = btnSelectSource.IsEnabled = txtNewName.IsEnabled = false;
                try
                {
                    rename = new RenameThread(txtSource.Text, txtNewName.Text, Finished, UpdateProgressGUI);
                    rename.Start();
                    DirectoryInfo from = new DirectoryInfo(txtSource.Text);
                    Task.Factory.StartNew(() =>
                    {
                        IEnumerable<FileInfo> filesFrom = IOUtil.EnumerateFilesSafe(from);                        
                        Dispatcher.Invoke(() =>
                        {
                            pbRenameState.Maximum = Math.Max(1, filesFrom.Count());
                            txtState.Content = filesFrom.Count() + " files to rename";
                        });
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    StopRename();
                }
            }
            else
            {
                StopRename();
                txtState.Content = "Cancelled!";
            }
        }


        private void UpdateProgressGUI(long renamed)
        {
            Dispatcher.Invoke(() =>
            {
                this.renamed += renamed;
                if (pbRenameState.Maximum > 1.1)
                {
                    double progress = this.renamed;
                    pbRenameState.Value = progress;
                }
            });
        }

        private void Finished()
        {
            Dispatcher.Invoke(() => {
                StopRename();
                txtState.Content = "Finished!";
                pbRenameState.Value = pbRenameState.Maximum;
            });
        }


        private void StopRename()
        {
            renameing = false;
            btnRename.Content = "Start rename";
            pbRenameState.Value = 0;
            txtProgressBar.Visibility = System.Windows.Visibility.Hidden;
            txtSource.IsEnabled = btnSelectSource.IsEnabled = txtNewName.IsEnabled = true;
            if (rename != null)
                rename.Stop();
            rename = null;
        }
    }
}
