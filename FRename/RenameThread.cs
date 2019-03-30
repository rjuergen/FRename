using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FRename
{
    class RenameThread
    {
        public delegate void Finished();
        public delegate void UpdateProgressGUI(long progress);

        private Thread t;
        private Task renameTask;
        private CancellationTokenSource cts;
        private Finished f;
        private UpdateProgressGUI u_pGUI;
        private string from;
        private string newName;

        public RenameThread(string from, string newName, Finished f, UpdateProgressGUI pg)
        {
            if (!Directory.Exists(from))
                throw new FileNotFoundException("Folder '" + from + "' doesn't exist!");            
            this.from = from;
            this.newName = newName;
            this.f = f;
            this.u_pGUI = pg;
            t = new Thread(StartRename);
            cts = new CancellationTokenSource();
        }


        public void Start()
        {
            t.Start();
        }

        public void Stop()
        {
            t.Abort();
            cts.Cancel();
        }

        /// <summary>
        /// start renaming files 
        /// </summary>
        private void StartRename()
        {
            DirectoryInfo from = new DirectoryInfo(this.from);
            renameTask = Task.Factory.StartNew(() => RenameFiles(from, newName), cts.Token);
            while (!renameTask.IsCompleted)
            {
                Thread.Sleep(1000);
            }
            if (!cts.IsCancellationRequested)
                f();
        }

        private void RenameFiles(DirectoryInfo from, string newName)
        {
            int i = 0;
            IEnumerable<FileInfo> filesFrom = IOUtil.EnumerateFilesSafe(from);
            int indexSize = Math.Max(Convert.ToString(filesFrom.Count()).Length, 3);
            foreach (FileInfo file in filesFrom)
            {
                i++;
                if (cts.IsCancellationRequested)
                    return;
                string name = createName(newName, file.Extension, i, indexSize);
                if (file.Name.Equals(name))
                    continue;
                string moveTo = from.FullName + "\\" + name;
                while (File.Exists(moveTo))
                {
                    FileInfo fi = new FileInfo(moveTo);
                    string s = createTmpName(fi);
                    if (File.Exists(s))
                        continue;
                    IOUtil.MoveFile(fi, s);
                    break;
                }
            }
            i = 0;
            string lastName = "";   
            foreach (FileInfo file in getSorted(IOUtil.EnumerateFilesSafe(new DirectoryInfo(from.FullName))))
            {
                i++;
                u_pGUI(1);
                if (cts.IsCancellationRequested)
                    return;
                if (Path.GetFileNameWithoutExtension(file.FullName).Equals(lastName))
                    i--;
                lastName = Path.GetFileNameWithoutExtension(file.FullName);
                string name = createName(newName, file.Extension, i, indexSize);
                if (file.Name.Equals(name))
                    continue;
                string moveTo = from.FullName + "\\" + name;                
                IOUtil.MoveFile(file, moveTo);                
            }
        }

        private List<FileInfo> getSorted(IEnumerable<FileInfo> files)
        {
            List<FileInfo> sorted = new List<FileInfo>(files);
            sorted.Sort((f1, f2) => f1.Name.CompareTo(f2.Name));
            return sorted;
        }
                

        private string createName(string newName, string extension, int i, int indexSize)
        {
            string s = Convert.ToString(i);
            for(int y = s.Length; y<indexSize; y++)
            {
                s = "0" + s;
            }
            return newName + s + extension;
        }        

        private string createTmpName(FileInfo fi)
        {
            return fi.DirectoryName + "\\" + fi.Name + "a"+ fi.Extension;
        }
    }
}
