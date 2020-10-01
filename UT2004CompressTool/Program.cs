using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UT2004CompressTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing variables...");

            string utLocation = ConfigurationManager.AppSettings["utLocation"];
            string destination = ConfigurationManager.AppSettings["destination"];
            string compressExtension = ConfigurationManager.AppSettings["compressExtension"];
            string[] extensionsToCompress = ConfigurationManager.AppSettings["extensionsToCompress"].Split(';');
            List<string> candidatesForCompression = new List<string>();
            List<string> existingFiles = new List<string>();

            Console.WriteLine("Looking for candidates for compression...");

            foreach (string extension in extensionsToCompress)
            {
                candidatesForCompression.AddRange(Directory.GetFiles(utLocation, "*." + extension, SearchOption.AllDirectories).ToList());
            }

            Console.WriteLine(candidatesForCompression.Count + " candidates discovered");

            Console.WriteLine("Looking for already existing files...");

            existingFiles.AddRange(Directory.GetFiles(destination, "*." + compressExtension).ToList());

            Console.WriteLine(existingFiles.Count + " already existing");

            Console.WriteLine("Launching compression process...");


            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.UseShellExecute = false;

            proc.Start();

            using (StreamWriter sw = proc.StandardInput)
            {
                sw.WriteLine(utLocation.First() + ":");
                sw.WriteLine(@"cd " + utLocation + "System");

                // Step 1: Compress all files
                foreach (string fileToCompress in candidatesForCompression)
                {

                    FileInfo originalFile = new FileInfo(fileToCompress);
                    FileInfo compressedFile = new FileInfo(fileToCompress + "." + compressExtension);

                    // Check if the file is not already compressed in the destination nor in the source
                    if (!existingFiles.Exists(x => x.Contains(originalFile.Name)) && !compressedFile.Exists)
                    {
                        sw.WriteLine("ucc compress \"" + fileToCompress + "\"");                        
                    }
                }

                sw.WriteLine(@"exit");
            }
            
            bool processEnded = proc.WaitForExit(2000);

            Console.WriteLine("Compression completed. Now copying files to destination...");

            int iteration = 0;

            // Step 2: Copy all files to destination
            foreach (string file in candidatesForCompression)
            {
                iteration++;

                FileInfo compressedFile = new FileInfo(file + "." + compressExtension);

                if (compressedFile.Exists)
                {
                    try
                    {
                        File.Copy(compressedFile.FullName, Path.Combine(destination, compressedFile.Name));

                        File.Delete(compressedFile.FullName);

                        Console.WriteLine(iteration + "/" + candidatesForCompression.Count + " completed: " + compressedFile.Name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + ex.InnerException + ex.StackTrace);
                    }
                }
            }

            Console.WriteLine("Process completed");
            Console.ReadLine();
        }
    }
}
