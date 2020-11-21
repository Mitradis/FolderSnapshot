using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FolderSnapshot
{
    public partial class FormMain : Form
    {
        static string pathExecuting = Process.GetCurrentProcess().MainModule.FileName;
        string pathShotsFolder = pathAddSlash(Path.GetDirectoryName(pathExecuting)) + @"FolderSnapshot\";
        string pathWorkFolder = null;
        string pathWorkFolderName = null;
        string selectListItem = null;
        List<string> snapshotList = new List<string>();
        List<string> ignoreFilesList = new List<string>();

        public FormMain()
        {
            InitializeComponent();
            if (!Directory.Exists(pathShotsFolder))
            {
                try
                {
                    Directory.CreateDirectory(pathShotsFolder);
                }
                catch
                {
                    MessageBox.Show("Нет каталога и не удалось его создать: " + pathShotsFolder + Environment.NewLine + "Дальнейшая работа приложения невозможна.");
                    Environment.Exit(0);
                }
            }
            else
            {
                snapShotsToListBox();
            }
            folderBrowserDialog1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void snapShotsToListBox()
        {
            listBox1.Items.Clear();
            foreach (string line in Directory.GetFiles(pathShotsFolder, "*.txt"))
            {
                listBox1.Items.Add(Path.GetFileName(line));
            }
        }

        private void buttonSelectFolder_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            pathWorkFolder = null;
            pathWorkFolderName = null;
            buttonSnapshot.Enabled = false;
            buttonClear.Enabled = false;
            DialogResult dialogResult = folderBrowserDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                pathWorkFolder = pathAddSlash(folderBrowserDialog1.SelectedPath);
                pathWorkFolderName = new DirectoryInfo(pathWorkFolder).Name;
                buttonSnapshot.Enabled = true;
                textBox1.Text = pathWorkFolder;
            }
        }

        private void buttonSnapshot_Click(object sender, EventArgs e)
        {
            buttonClear.Enabled = false;
            buttonDelete.Enabled = false;
            snapshotList.Add("#FolderSnapshot List");
            searthFiles(pathWorkFolder);
            searthFolder(pathWorkFolder);
            string file = pathShotsFolder + pathWorkFolderName + "_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".txt";
            try
            {
                File.WriteAllLines(file, snapshotList, new UTF8Encoding(false));
            }
            catch
            {
                MessageBox.Show("Не удалось записать файл: " + file);
            }
            snapshotList.Clear();
            snapShotsToListBox();
        }

        private void searthFolder(string path)
        {
            foreach (string text in Directory.GetDirectories(path))
            {
                if (!text.Contains("$RECYCLE.BIN") && !text.Contains("System Volume Information"))
                {
                    snapshotList.Add(text);
                    searthFiles(text);
                    searthFolder(text);
                }
            }
        }

        private void searthFiles(string path)
        {
            foreach (string item in Directory.GetFiles(path))
            {
                snapshotList.Add(item);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                selectListItem = listBox1.SelectedItem.ToString();
                string text = selectListItem;
                for (int i = 0; i < 6; i++)
                {
                    int index = text.LastIndexOf("_");
                    if (index != -1)
                    {
                        text = text.Remove(index);
                    }
                    else
                    {
                        break;
                    }
                }
                if (File.Exists(pathShotsFolder + selectListItem))
                {
                    buttonClear.Enabled = pathWorkFolderName == text && File.ReadLines(pathShotsFolder + selectListItem).First() == "#FolderSnapshot List";
                    buttonDelete.Enabled = true;
                }
                else
                {
                    buttonClear.Enabled = false;
                    buttonDelete.Enabled = false;
                    snapShotsToListBox();
                }
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Удалить выбранный снимок?", "Подтверждение", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                buttonClear.Enabled = false;
                buttonDelete.Enabled = false;
                deleteAny(pathShotsFolder + selectListItem);
                snapShotsToListBox();
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Удалить все \"чужие\" файлы в выбранном каталоге в соответствии с выбранным снимком?", "Подтверждение", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (File.Exists(pathShotsFolder + selectListItem))
                {
                    buttonClear.Enabled = false;
                    ignoreFilesList.AddRange(File.ReadAllLines(pathShotsFolder + selectListItem));
                    clearFolder(pathWorkFolder);
                    ignoreFilesList.Clear();
                    buttonClear.Enabled = true;
                }
                else
                {
                    buttonClear.Enabled = false;
                    buttonDelete.Enabled = false;
                    snapShotsToListBox();
                }
            }
        }

        private void clearFolder(string clearpath)
        {
            if (Directory.Exists(clearpath))
            {
                foreach (string line in Directory.EnumerateFiles(clearpath))
                {
                    if (!ignoreFilesList.Contains(line, StringComparer.OrdinalIgnoreCase))
                    {
                        deleteAny(line);
                    }
                }
                foreach (string line in Directory.EnumerateDirectories(clearpath))
                {
                    if (!ignoreFilesList.Contains(line, StringComparer.OrdinalIgnoreCase))
                    {
                        deleteAny(line);
                    }
                    else
                    {
                        clearFolder(line);
                    }
                }
            }
        }

        private void deleteAny(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    MessageBox.Show("Не удалось удалить файл: " + path);
                }
            }
            else if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch
                {
                    MessageBox.Show("Не удалось удалить каталог: " + path);
                }
            }
        }

        private static string pathAddSlash(string path)
        {
            if (path.EndsWith("/") || path.EndsWith(@"\"))
            {
                return path;
            }
            if (path.Contains("/"))
            {
                return path + "/";
            }
            if (path.Contains(@"\"))
            {
                return path + @"\";
            }
            return path;
        }
    }
}
