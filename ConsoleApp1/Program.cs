using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        public static double[] ittart = new double[2] { 0, 0 };
        static void Main(string[] args)
        {
            Sync();
            Console.ReadLine();
        }
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
            else if (cserelhetomeghajto.Count > 1)
            {
                Console.WriteLine("Kérem válasszon a meghajtók közűl");
                cserelhetomeghajto.ForEach(d => { Console.WriteLine(d); });
                if (cserelhetomeghajto.Contains(Console.ReadLine())) return Console.ReadLine();
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

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
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
        //return subbdirectory size
        public static long DirSize(DirectoryInfo x)
        {
            return x.GetFiles().ToList().Sum(y => y.Length);
        }

        public static void Sync()
        {
            string elso = GetSongsDirectory();
            string masodik = Cserelheto();
            List<string> honnanitemek = Directory.GetDirectories(elso, "*", SearchOption.TopDirectoryOnly).Select(z => new DirectoryInfo(z).Name.Split(' ').First()).ToList();
            List<string> hovaitemek = Directory.GetDirectories(masodik, "*", SearchOption.TopDirectoryOnly).Select(z => new DirectoryInfo(z).Name.Split(' ').First()).ToList(); ;
            var a = honnanitemek.Where(x => !hovaitemek.Contains(x)).ToList();
            var b = hovaitemek.Where(x => !honnanitemek.Contains(x)).ToList();
            //if usb has enougt free space (could be done reverse but why would i xd
            if (a.Concat(b).Sum(x => DirSize(new DirectoryInfo(x)) + new DirectoryInfo(x).GetDirectories().ToList().Sum(z => DirSize(z))) < new DriveInfo(masodik).TotalFreeSpace)
            {
                Progressbar(1);
                a.Select((z,i)=> new {z,i}).ToDictionary(x=> x.z, x=> x.i).ToList().ForEach(z=> { z.Key.ToList().ForEach(o=> CopyDirectory(elso, masodik, true)); ittart[1]= (z.Value); });
                b.ForEach(z => CopyDirectory(masodik, elso, true));
            }
            else
            {
                Console.WriteLine("A meghajtó megtelt");
                Console.ReadKey();
            }
            //honnanitemek.Where(x => !hovaitemek.Contains(Path.Combine(masodik, x.Substring(elso.Length + 1)))).ToList().ForEach(y => { File.Copy(y, Path.Combine(masodik, y.Substring(elso.Length + 1)), false); });
            //hovaitemek.Where(x => !hovaitemek.Contains(Path.Combine(elso, x.Substring(masodik.Length + 1)))).ToList().ForEach(y => { File.Copy(y, Path.Combine(elso, y.Substring(masodik.Length + 1)), false); });
            Console.WriteLine("Ok");
        }

        public static void Progressbar(int szama)
        {
            while (true)
            {
                var szazalak = Math.Round(ittart[0] / ittart[1]*100,2);
                Console.Clear();
                Console.WriteLine($"Progressbar: {szama}");
                Console.Write(szazalak);
                if(szazalak==100)
                {
                    return;
                }
            }
        }
    }
}