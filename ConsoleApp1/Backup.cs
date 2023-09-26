using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using EventTrigger = Microsoft.Win32.TaskScheduler.EventTrigger;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Sync();
            Console.ReadLine();
        }
        //search for removable drive (could be any type)
        public static string Cserelheto()
        {
            List<string> cserelhetomeghajto = new List<string>();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.DriveType.ToString() == "Removable")
                {
                    cserelhetomeghajto.Add(d.RootDirectory.ToString());
                }
            }
            if (cserelhetomeghajto.Count() < 1)
            {
                throw new Exception("Nincsen cserelheto meghajto");
            }
            //if there is more than one 
            else if (cserelhetomeghajto.Count > 1)
            {
                Console.WriteLine("Kérem válasszon a meghajtók közűl");
                cserelhetomeghajto.ForEach(d => { Console.WriteLine(d); });
                var valami = Console.ReadLine();
                if (cserelhetomeghajto.Contains(valami)) return valami;
                else throw new Exception("Fail");
            }
            return cserelhetomeghajto.First();
        }
        public static string GetSongsDirectory()
        {
            const string keyName1 = @"HKEY_CLASSES_ROOT\osu\shell\open\command";
            const string keyName2 = @"HKEY_CLASSES_ROOT\osu!\shell\open\command";
            string path = string.Empty, songsPath = string.Empty;
            try
            {
                path = Microsoft.Win32.Registry.GetValue(keyName1, string.Empty, string.Empty).ToString();
                if (path == string.Empty)
                    path = Microsoft.Win32.Registry.GetValue(keyName2, string.Empty, string.Empty).ToString();
                if (path != string.Empty)
                {
                    path = path.Remove(0, 1);
                    path = path.Split('\"')[0];
                    path = Path.GetDirectoryName(path);

                    string iniPath = Path.Combine(path, "osu!." + Environment.UserName + ".cfg");
                    StreamReader s = File.OpenText(iniPath);

                    while (!s.EndOfStream && !(songsPath != string.Empty))
                    {
                        string l = s.ReadLine();
                        if (l.StartsWith("#"))
                            continue;

                        string[] separator = { " = " };
                        string[] kv = l.Split(separator, StringSplitOptions.None);
                        if (kv[0] == "BeatmapDirectory")
                            songsPath = kv[1];
                    }

                    if (songsPath == string.Empty)
                        songsPath = Path.Combine(path, "Songs");
                }
            }
            catch
            {
                //probaly security or io exception or unexpected value from registry
                //Beatmap directory undetected
                return "";
            }
            if (System.IO.Path.IsPathRooted(songsPath))
                return songsPath;
            else
                return System.IO.Path.Combine(path, songsPath);
        }
        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);


            // Get information about the destination directory
            var dest = new DirectoryInfo(destinationDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Check if the source directory exists
            if (!dest.Exists)
                Directory.CreateDirectory(destinationDir);

            // Cache directories before we start copying
            DirectoryInfo[] dests = dest.GetDirectories();

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories().Where(x=> !dest.GetDirectories().Contains(x)).ToArray();

            // Get the files in the source directory and copy to the destination directory
            var tmp = dir.GetFiles().ToList().Where(x => !dest.GetFiles().Contains(x));

            foreach (FileInfo file in tmp)
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                try
                {
                    file.CopyTo(targetFilePath);
                    atmasolt += file.Length;
                    szazalek = Math.Round((atmasolt / osszesmasolando) * 100, 2);
                }
                catch {
                    atmasolt += file.Length;
                    szazalek = Math.Round((atmasolt / osszesmasolando) * 100, 2);
                }
                
            }

            // If recursive and copying subdirectories, recursively call this method 
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }

        }
        //return subbdirectory size with filesize
        public static long DirSize(DirectoryInfo x)
        {
            return x.GetFiles().ToList().Sum(y => y.Length) + x.GetDirectories().Sum(z => DirSize(z));
        }

        public static double szazalek = 0.0;
        public static double osszesmasolando = 0;
        public static double atmasolt = 0;
        public static void Sync()
        {
            string elso = GetSongsDirectory();
            string masodik = Cserelheto();
            List<DirectoryInfo> honnanitemek = Directory.GetDirectories(elso, "*", SearchOption.TopDirectoryOnly).Select(z => new DirectoryInfo(z)).ToList();
            List<DirectoryInfo> hovaitemek = Directory.GetDirectories(masodik, "*", SearchOption.TopDirectoryOnly).Select(z => new DirectoryInfo(z)).ToList(); ;
            //if usb has enougt free space (could be done reverse but why would i xd
            osszesmasolando = honnanitemek.Where(x => !hovaitemek.Contains(x)).ToList().Union(hovaitemek.Where(x => !honnanitemek.Contains(x)).ToList()).Sum(x => DirSize(x));
            if (honnanitemek.Where(x => !hovaitemek.Contains(x)).ToList().Sum(y=> DirSize(y)) < new DriveInfo(masodik).TotalFreeSpace)
            {
                atmasolt = 0;
                Progressbar(1).Start();
                CopyDirectory(elso, masodik, true);
                szazalek = 100;
                atmasolt = 0;
                Progressbar(2).Start();
                CopyDirectory(masodik, elso, true);
                szazalek = 100;
            }
            else
            {
                Console.WriteLine("A meghajtó megtelt");
                Console.ReadKey();
            }
            Console.Clear();
            Console.WriteLine("Ok");
        }
        //paralel running thread for the bar
        public static Thread Progressbar(int szama)
        {
            Thread szal = new Thread(() =>
            {
                IProgress<double> progress = new Progress<double>(x => szazalek = x);

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine($"Progressbar: {szama} ");
                    Console.Write('[');
                    Console.ForegroundColor = ConsoleColor.White;
                    for (int i = 0; i < 10; i++)
                    {
                        if (szazalek > i* 10)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        Console.Write('|');
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("]");
                    Console.Write(szazalek+"%");
                    Thread.Sleep(2000);
                    if (szazalek == 100)
                    {
                        Thread.CurrentThread.Join();
                    }
                }
            });
            return szal;
        }
        public void TaskScedulerAd()
        {
            using (TaskService ts = new TaskService())
            {
                Console.WriteLine("N");
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "teszteles";

                //add trigger
                EventTrigger et = new EventTrigger();
                et.SetBasic("System", "User32", 1074);
                et.Enabled = true;
                td.Triggers.Add(et);
                //add action
                ExecAction ta = new ExecAction();
                ta.Path = "F:\\Asztal\\osu import export\\ConsoleApp2\\ConsoleApp2\\bin\\Debug\\ConsoleApp2.exe";
                td.Actions.Add(ta);

                if (ts.RootFolder.AllTasks.Where(x => x.Name == "Test").Count() < 1)
                {
                    //register task
                    ts.RootFolder.RegisterTaskDefinition("Test", td);
                    Console.WriteLine("ok");
                }
            }
        }
    }
}