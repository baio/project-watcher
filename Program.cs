using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Project_Watcher
{
    class Program
    {
        private static string SETTINGS_FILE_NAME = "project-watcher.txt";

        private static string srcPath = null;

        private static string destPath = null;

        private static string [] exts = null;

        static void Main(string[] args)
        {
            var watcher = new FileSystemWatcher();

            exts = ReadSettings(out srcPath, out destPath);

            watcher.Filter = "";
            srcPath = System.IO.Path.GetFullPath(srcPath) + "\\";
            destPath =  System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), destPath)) + "\\";
            watcher.Path = srcPath;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
            watcher.Changed += new FileSystemEventHandler(watcher_Changed);
            watcher.Deleted += new FileSystemEventHandler(watcher_Deleted);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.Renamed += new RenamedEventHandler(watcher_Renamed);
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;

            Log(string.Format("Project-Watcher watching : {0} \nPress q for exit", srcPath));

            Prepare(srcPath, destPath);

            while (Console.Read() != 'q')
            { }
        }


        static bool _doubledFlag = false;

        private static void CreateFile(string FileName, bool ReCreate = false)
        {
            if (System.IO.File.Exists(FileName))
            {
                if (ReCreate) 
                    System.IO.File.Delete(FileName);
                else
                    return;
            }

            CreateDirectory(System.IO.Path.GetDirectoryName(FileName));

            var file = System.IO.File.Create(FileName);
            //file.Write(new [] {(byte)0}, 0, 1);
            file.Close();

            Log("Create file : " + FileName);
        }

        private static void CreateDirectory(string Folder)
        {
            if (!System.IO.Directory.Exists(Folder))
            {
                System.IO.Directory.CreateDirectory(Folder);

                Log("Create directory : " + Folder);
            }                     
        }

        private static void CopyFile(string SrcName, string DestName)
        {
            File.Copy(SrcName, DestName, true);

            Log("File copied : " + DestName);
        }

        private static void DeleteFile(string DestName)
        {
            if (System.IO.File.Exists(DestName))
            {
                File.Delete(DestName);

                Log("File deleted : " + DestName);
            }
        }

        private static string [] ReadSettings(out string SrcProject, out string DestPath)
        {
            var strs = File.ReadAllText(SETTINGS_FILE_NAME).Split(new [] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);

            SrcProject = strs[0];

            DestPath = strs[1];

            return strs.Skip(2).ToArray();
        }

        private static void Log(string Message, bool Error = false)
        {
            if (Error)
                Console.ForegroundColor = ConsoleColor.Blue;

            Console.WriteLine(Message);

            Console.ResetColor();
        }

        private static void Prepare(string SrcFolder, string DestFolder)
        {
            foreach (var sf in System.IO.Directory.GetDirectories(SrcFolder))
            {
                var df = DestFolder + new System.IO.DirectoryInfo(sf).Name + "\\";

                CreateDirectory(df);

                Prepare(sf, df);
            }

            foreach (var sf in System.IO.Directory.GetFiles(SrcFolder))
            {
                var df = DestFolder + System.IO.Path.GetFileName(sf);

                if (CheckFileExt(df))
                {
                    CreateFile(df);

                    CopyFile(sf, df);
                }
            }
        }


        private static bool CheckFileExt(string FileName)
        {
            var fn = System.IO.Path.GetFileName(FileName);
            fn = "*" + fn.Remove(0, fn.LastIndexOf('.'));
            return exts.Contains(fn);
        }

        private static void ReBuild(string FileName, RebuildType RebuildType)
        {
            if (!CheckFileExt(FileName)) return;

            if (RebuildType == Program.RebuildType.Changed)
            {
                if (_doubledFlag)
                {
                    _doubledFlag = false;

                    return;
                }
                else
                {
                    _doubledFlag = true;
                }
            }

            string localName = destPath + FileName.Replace(srcPath, "");

            switch (RebuildType)
            { 
                case Program.RebuildType.Changed:
                case Program.RebuildType.Created:
                    CreateFile(localName);
                    CopyFile(FileName, localName);
                    break;
                case Program.RebuildType.Deleted:
                    DeleteFile(localName);
                    break;
            }
        }

        static void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            ReBuild(e.FullPath, RebuildType.Changed);
        }

        static void watcher_Created(object sender, FileSystemEventArgs e)
        {
            ReBuild(e.FullPath, RebuildType.Created);
        }

        static void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            ReBuild(e.FullPath, RebuildType.Created);
            ReBuild(e.OldFullPath, RebuildType.Deleted);
        }

        static void watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            ReBuild(e.FullPath, RebuildType.Deleted);
        }
/*
private bool FileCompare(string file1, string file2)
{
     int file1byte;
     int file2byte;
     FileStream fs1;
     FileStream fs2;

     // Determine if the same file was referenced two times.
     if (file1 == file2)
     {
          // Return true to indicate that the files are the same.
          return true;
     }
               
     // Open the two files.
     fs1 = new FileStream(file1, FileMode.Open);
     fs2 = new FileStream(file2, FileMode.Open);
          
     // Check the file sizes. If they are not the same, the files 
        // are not the same.
     if (fs1.Length != fs2.Length)
     {
          // Close the file
          fs1.Close();
          fs2.Close();

          // Return false to indicate files are different
          return false;
     }

     // Read and compare a byte from each file until either a
     // non-matching set of bytes is found or until the end of
     // file1 is reached.
     do 
     {
          // Read one byte from each file.
          file1byte = fs1.ReadByte();
          file2byte = fs2.ReadByte();
     }
     while ((file1byte == file2byte) && (file1byte != -1));
     
     // Close the files.
     fs1.Close();
     fs2.Close();

     // Return the success of the comparison. "file1byte" is 
     // equal to "file2byte" at this point only if the files are 
        // the same.
     return ((file1byte - file2byte) == 0);
}
         */

        enum RebuildType
        { 
            Changed,
            Created,
            Deleted
        }

    }
}
