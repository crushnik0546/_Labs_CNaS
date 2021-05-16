using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FilesStorage
{
    public static class FileProcessing
    {
        private const string Root = @"C:\file_storage";

        public static List<string> ProcessDirectory(string directory)
        {
            List<string> result = new List<string>();

            string[] fileEntries = Directory.GetFiles(directory);
            foreach(string fileName in fileEntries)
            {
                result.Add($"File: {Path.GetFileName(fileName)} ");
            }

            string[] directoryEntities = Directory.GetDirectories(directory);
            foreach(string directoryName in directoryEntities)
            {
                result.Add($"Directory: {new DirectoryInfo(directoryName).Name}");
            }

            return result;
        }
        
        public static string GetFullPath(string userPath)
        {
            if (userPath == null)
            {
                return Root;
            }
            else
            {
                return Path.Combine(Root, userPath);
            }
        }

        public static Dictionary<string, string> FileHeader(string filePath)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            FileInfo fileInfo = new FileInfo(filePath);

            header.Add("Name: ", fileInfo.Name);
            header.Add("Size: ", fileInfo.Length.ToString());
            header.Add("Extension: ", fileInfo.Extension);
            header.Add("Creation time: ", fileInfo.CreationTime.ToString());
            header.Add("Last write time:", fileInfo.LastWriteTime.ToString());
            header.Add("Last access time: ", fileInfo.LastAccessTime.ToString());

            return header;
        }

    }
}
