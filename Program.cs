using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DiskSpaceManager
{
    static class Program
    {
        //AutoSorted List Implementation Where T IComparable
        public class AutoSortedList<T> : List<T> where T : IComparable<T>
        {
            public new void Add(T item)
            {
                base.Add(item);
                Sort();
            }
            //To set get
            public new T this[int index]
            {
                get { return base[index]; }
                set
                {
                    base[index] = value;
                    Sort();
                }
            }
        }

        public static long GetDirectorySize(this System.IO.DirectoryInfo directoryInfo, bool recursive = true)
        {
            try
            {
                var startDirectorySize = default(long);
                if (directoryInfo == null || !directoryInfo.Exists)
                    return startDirectorySize; //Return 0 while Directory does not exist.

                //Add size of files in the Current Directory to main size.
                foreach (var fileInfo in directoryInfo.GetFiles())
                    System.Threading.Interlocked.Add(ref startDirectorySize, fileInfo.Length);

                if (recursive) //Loop on Sub Direcotries in the Current Directory and Calculate it's files size.
                    System.Threading.Tasks.Parallel.ForEach(directoryInfo.GetDirectories(), (subDirectory) =>
                        System.Threading.Interlocked.Add(ref startDirectorySize,
                            GetDirectorySize(subDirectory, recursive)));
                return startDirectorySize;
            }
            catch{}
            return 0;
        }
        static string Ask(string q)
        {
            Console.Write($"{q}:");
            return Console.ReadLine();
        }
        static string ToPercent(double d)
        {
            return $"{(d * 100f):F1}%";
        }
        static string BytesToMax(long b)
        {
            if (b < 1024)
                return $"{b} B";
            if (b < 1024 * 1024)
                return $"{b / 1024f:F1}KB";
            if (b < 1024 * 1024 * 1024)
                return $"{b / 1024f / 1024f:F1}MB";
            if (b < 1024L * 1024 * 1024 * 1024)
                return $"{b / 1024f / 1024f / 1024f:F1}GB";
            if (b < 1024L * 1024 * 1024 * 1024 * 1024)
                return $"{b / 1024f / 1024f / 1024f / 1024f:F1}TB";
            return $"{b / 1024f / 1024f / 1024f / 1024f / 1024f:F1}PB";
        }
        class DirectoryInfoSize : IComparable<DirectoryInfoSize>
        {
            public int CompareTo(DirectoryInfoSize other)
            {
                if (other == null)
                    return 1;
                return other.Size.CompareTo(Size);
            }

            public DirectoryInfoSize(DirectoryInfo d)
            {
                Dir = d;
                Size = d.GetDirectorySize();
            }

            public DirectoryInfoSize(DirectoryInfo dir, long size)
            {
                Dir = dir;
                Size = size;
            }

            public DirectoryInfo Dir;
            public long Size;
        }
        class FileInfoSize : IComparable<FileInfoSize>
        {
            public int CompareTo(FileInfoSize other)
            {
                if (other == null)
                    return 1;
                return other.Size.CompareTo(Size);
            }

            public FileInfoSize(FileInfo d)
            {
                File = d;
                Size = d.Length;
            }

            public FileInfoSize(FileInfo file, long size)
            {
                File = file;
                Size = size;
            }

            public FileInfo File;
            public long Size;
        }


        public static DirectoryInfo Current;

        static void Main(string[] args)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            List<string> AviableRoots = new List<string>();
            int ie = 0;
            for (int i = 0; i < drives.Length; i++)
            {
                var d = drives[i];
                if (d.DriveType != DriveType.Fixed) continue;
                AviableRoots.Add(d.Name);
                Console.WriteLine($"({ie})[{d.Name}]{d.VolumeLabel} {BytesToMax(d.TotalFreeSpace)}({ToPercent(d.TotalFreeSpace / (double)d.TotalSize)})");
                ie++;
            }
            var root = AviableRoots[int.Parse(Ask("Select Disk"))];
            Console.Clear();
            DriveInfo drive = new DriveInfo(root);
            Current = drive.RootDirectory;
            Dir(Current);
        }

        static void Dir(DirectoryInfo dir, bool c = false)
        {
            Console.Clear();
            Console.WriteLine(Current.FullName);
            AutoSortedList<DirectoryInfoSize> dirs = new AutoSortedList<DirectoryInfoSize>();
            foreach (var d in dir.GetDirectories())
            {
                long s = 0;
                if (c)
                {
                    s = d.GetDirectorySize();
                    if (s < 1024 * 1024 * 100) continue;
                }
                dirs.Add(new DirectoryInfoSize(d, s));
            }

            AutoSortedList<FileInfoSize> files = new AutoSortedList<FileInfoSize>();
            foreach (var f in dir.GetFiles())
            { 
                long s = 0;
                s = f.Length;
                if (s < 1024 * 1024 * 100) continue;
                files.Add(new FileInfoSize(f, s));
            }

            //for dirs
            for (int i = 0; i < dirs.Count; i++)
            {
                var d = dirs[i];
                Console.WriteLine($"-({i})[{BytesToMax(d.Size)}]{d.Dir.Name}");
            }

            foreach (var f in files)
            {
                Console.WriteLine($"-[{BytesToMax(f.Size)}]{f.File.Name}");
            }
            Console.Write("~$");
            var cmd = Console.ReadLine();
            //switch for cmds like 0-999(as dir id) or delete
            if (cmd == "del")
            {
                var d = Ask("Enter Directory Id");
                if (int.TryParse(d, out int id))
                {
                    if (id < dirs.Count)
                    {
                        var dirr = dirs[id].Dir;
                        Console.WriteLine($"Deleting {dirr.FullName}");
                        try
                        {
                            dirr.Delete(true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        Console.WriteLine("Done");
                    }
                }
            }
            else if (cmd == ".")
            {
                if (Current.Parent != null)
                {
                    Current = Current.Parent;
                }
                else
                {
                    Console.WriteLine("You are in root");
                    Console.ReadKey();
                }
            }
            else if (cmd == "exit")
            {
                Environment.Exit(0);
            }
            else if (cmd == "help")
            {
                Console.WriteLine("del - delete directory");
                Console.WriteLine(". - go to parent directory");
                Console.WriteLine("(0-...) - go to directory");
                Console.WriteLine("show - show current directory in explorer");
                Console.WriteLine("c - enable/disable directory size calculation (May be slow!)");
                Console.WriteLine("exit - exit");
                Console.WriteLine("help - show this");
                Console.ReadKey();
            }
            else if (cmd == "show")
            {
                Process.Start(new ProcessStartInfo("explorer.exe", Current.FullName));
            }
            else if (cmd == "c")
            {
                c = !c;
            }
            else
            {
                int id;
                if (int.TryParse(cmd, out id))
                {
                    if (id < dirs.Count)
                    {
                        Current = dirs[id].Dir;
                    }
                }
            }
            Dir(Current, c);
        }
    }
}
