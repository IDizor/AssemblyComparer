using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssemblyComparer.Console
{
    public class Program
    {
        private static Dictionary<string, bool> Filters = new Dictionary<string, bool>();
        private static int Root1Length;
        private static int Root2Length;

        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("AsmComp v0.1");
                System.Console.WriteLine("Displays the differences of dir-2 comparing to dir-1.");
                System.Console.WriteLine("How to use: asmcomp <dir-1> <dir-2> [name-ext-filter1;name-ext-filter2;...]");
                System.Console.WriteLine("Example:");
                System.Console.WriteLine("  asmcomp \"C:\\Temp1\" \"C:\\Temp2\" -*.pdb;-obj");
                System.Console.ReadKey();
                return;
            }

            var dir1 = args[0];
            var dir2 = args[1];
            dir1 = dir1.TrimEnd('\\');
            dir2 = dir2.TrimEnd('\\');
            Root1Length = dir1.Length;
            Root2Length = dir2.Length;

            // prepare filters
            var filters = args.Length > 2
                ? args[2].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[] { };
            
            foreach (var f in filters)
            {
                var isInverted = f.StartsWith("-");
                var filter = isInverted ? f.Substring(1) : f;
                filter = Regex.Replace(filter, @"\.", @"\.");
                filter = Regex.Replace(filter, @"\?", @".");
                filter = Regex.Replace(filter, @"\*", @".+");
                filter = "^" + filter + "$";
                Filters.Add(filter, !isInverted);
            }

            var isDirComparison = Directory.Exists(dir1) && Directory.Exists(dir2);
            var isFileComparison = !isDirComparison && File.Exists(dir1) && File.Exists(dir2);

            if (!isDirComparison && !isFileComparison)
            {
                System.Console.WriteLine("Error: Specified directory or file does not exist.");
                return;
            }

            if (isDirComparison)
            {
                CompareDirs(dir1, dir2, filters);
            }
            else if (isFileComparison)
            {
                if (AreFilesMatched(dir1, dir2))
                {
                    System.Console.WriteLine("The files are the same.");
                }
                else
                {
                    System.Console.WriteLine("The files are different.");
                }
            }

            System.Console.WriteLine("Done.");
            System.Console.ReadLine();
        }

        private static void CompareDirs(string dir1, string dir2, string[] filters)
        {
            dir1 = dir1.TrimEnd('\\');
            dir2 = dir2.TrimEnd('\\');

            // DIRECTORIES
            var dirs1 = Directory.GetDirectories(dir1, "*", SearchOption.TopDirectoryOnly);
            var dirs2 = Directory.GetDirectories(dir2, "*", SearchOption.TopDirectoryOnly);

            dirs1 = dirs1.Select(d => d.Substring(dir1.Length)).Where(d => ShouldBeIncluded(d, filters)).ToArray();
            dirs2 = dirs2.Select(d => d.Substring(dir2.Length)).Where(d => ShouldBeIncluded(d, filters)).ToArray();
            var alikeDirs = new List<string>();

            // search for deleted directories
            foreach (var d1 in dirs1)
            {
                if (!dirs2.Any(d2 => d2.Equals(d1, StringComparison.InvariantCultureIgnoreCase)))
                {
                    System.Console.WriteLine($"- dir : {(dir1 + d1).Substring(Root1Length)}");
                }
            }

            // search for new directories
            foreach (var d2 in dirs2)
            {
                if (!dirs1.Any(d1 => d1.Equals(d2, StringComparison.InvariantCultureIgnoreCase)))
                {
                    System.Console.WriteLine($"+ dir : {(dir2 + d2).Substring(Root2Length)}");
                }
                else
                {
                    // populate alikeDirs with directories matched by name
                    alikeDirs.Add(d2);
                }
            }

            // compare directories
            foreach (var d in alikeDirs)
            {
                CompareDirs(dir1 + d, dir2 + d, filters);
            }

            // FILES
            var files1 = Directory.GetFiles(dir1, "*", SearchOption.TopDirectoryOnly);
            var files2 = Directory.GetFiles(dir2, "*", SearchOption.TopDirectoryOnly);
            
            files1 = files1.Select(f => f.Substring(dir1.Length)).Where(f => ShouldBeIncluded(f, filters)).ToArray();
            files2 = files2.Select(f => f.Substring(dir2.Length)).Where(f => ShouldBeIncluded(f, filters)).ToArray();
            var alikeFiles = new List<string>();

            // search for deleted files
            foreach (var f1 in files1)
            {
                if (!files2.Any(f2 => f2.Equals(f1, StringComparison.InvariantCultureIgnoreCase)))
                {
                    System.Console.WriteLine($"- file : {(dir1 + f1).Substring(Root1Length)}");
                }
            }

            // search for new files
            foreach (var f2 in files2)
            {
                if (!files1.Any(f1 => f1.Equals(f2, StringComparison.InvariantCultureIgnoreCase)))
                {
                    System.Console.WriteLine($"+ file : {(dir2 + f2).Substring(Root2Length)}");
                }
                else
                {
                    // populate alikeDirs with directories matched by name
                    alikeFiles.Add(f2);
                }
            }

            // compare files
            foreach (var f in alikeFiles)
            {
                if (!AreFilesMatched(dir1 + f, dir2 + f))
                {
                    System.Console.WriteLine($"! file : {(dir2 + f).Substring(Root2Length)}");
                }
            }
        }

        private static bool AreFilesMatched(string file1, string file2)
        {
            var hash1 = HasherFactory.GetHasher(file1).GetHashForComparison(file1);
            var hash2 = HasherFactory.GetHasher(file2).GetHashForComparison(file2);

            return hash1 == hash2;
        }

        private static bool ShouldBeIncluded(string path, string[] filters)
        {
            foreach (var f in Filters)
            {
                return Regex.IsMatch(path.TrimStart('\\'), f.Key) == f.Value;
            }

            return true;
        }
    }
}
