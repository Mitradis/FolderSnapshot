using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FolderSnapshot
{
    public partial class FormMain : Form
    {
        string pathShotsFolder = pathAddSlash(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FolderSnapshot"));
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
                catch (Exception ex)
                {
                    MessageBox.Show("Нет каталога программы и не удалось его создать: " + pathShotsFolder + Environment.NewLine + "Дальнейшая работа приложения невозможна." + Environment.NewLine + ex.Message);
                    Application.Exit();
                }
            }
            else
            {
                snapShotsToListBox();
            }
            folderBrowserDialog1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        void snapShotsToListBox()
        {
            listBox1.Items.Clear();
            foreach (string line in Directory.GetFiles(pathShotsFolder, "*.txt"))
            {
                listBox1.Items.Add(Path.GetFileName(line));
            }
        }

        void buttonSelectFolder_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            pathWorkFolder = null;
            pathWorkFolderName = null;
            buttonSnapshot.Enabled = false;
            buttonClear.Enabled = false;
            DialogResult dialogResult = folderBrowserDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK && folderBrowserDialog1.SelectedPath.Length > 3)
            {
                pathWorkFolder = pathAddSlash(folderBrowserDialog1.SelectedPath);
                pathWorkFolderName = new DirectoryInfo(pathWorkFolder).Name;
                buttonSnapshot.Enabled = true;
                textBox1.Text = pathWorkFolder;
                listBox1_SelectedIndexChanged(this, new EventArgs());
            }
        }

        void buttonSnapshot_Click(object sender, EventArgs e)
        {
            buttonClear.Enabled = false;
            buttonDelete.Enabled = false;
            snapshotList.Add("#FolderSnapshot List");
            searchFolder(pathWorkFolder);
            string file = pathShotsFolder + pathWorkFolderName + "_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".txt";
            try
            {
                File.WriteAllLines(file, snapshotList, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось записать файл: " + file + Environment.NewLine + ex.Message);
            }
            snapshotList.Clear();
            snapShotsToListBox();
        }

        void searchFolder(string path)
        {
            searchFiles(path);
            foreach (string line in Directory.GetDirectories(path))
            {
                if (!new DirectoryInfo(line).Attributes.HasFlag(FileAttributes.System))
                {
                    snapshotList.Add(line);
                    searchFolder(line);
                }
            }
        }

        void searchFiles(string path)
        {
            foreach (string item in Directory.GetFiles(path))
            {
                snapshotList.Add(item);
            }
        }

        void listBox1_SelectedIndexChanged(object sender, EventArgs e)
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
                    StreamReader sr = new StreamReader(pathShotsFolder + selectListItem);
                    buttonClear.Enabled = pathWorkFolderName == text && sr.ReadLine() == "#FolderSnapshot List";
                    buttonDelete.Enabled = true;
                    sr.Close();
                }
                else
                {
                    buttonClear.Enabled = false;
                    buttonDelete.Enabled = false;
                    snapShotsToListBox();
                }
            }
        }

        void buttonDelete_Click(object sender, EventArgs e)
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

        void buttonClear_Click(object sender, EventArgs e)
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

        void clearFolder(string clearpath)
        {
            if (Directory.Exists(clearpath))
            {
                foreach (string line in Directory.EnumerateFiles(clearpath))
                {
                    if (!ignoreFilesList.Exists(s => s.Equals(line, StringComparison.OrdinalIgnoreCase)))
                    {
                        deleteAny(line);
                    }
                }
                foreach (string line in Directory.EnumerateDirectories(clearpath))
                {
                    if (!ignoreFilesList.Exists(s => s.Equals(line, StringComparison.OrdinalIgnoreCase)))
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

        void deleteAny(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось удалить файл: " + path + Environment.NewLine + ex.Message);
                }
            }
            else if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось удалить каталог: " + path + Environment.NewLine + ex.Message);
                }
            }
        }

        static string pathAddSlash(string path)
        {
            if (!path.EndsWith("/") && !path.EndsWith(@"\"))
            {
                if (path.Contains("/"))
                {
                    path += "/";
                }
                else if (path.Contains(@"\"))
                {
                    path += @"\";
                }
            }
            return path;
        }
    }
}
