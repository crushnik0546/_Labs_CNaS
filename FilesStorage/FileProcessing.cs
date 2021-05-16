using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FilesStorage
{
    public static class FileProcessing
    {
        private const string Root = @"C:\file_storage";
        public enum PutCodes
        {
            SourceFileDoesntExist = 0,
            DestDirectoryDoesntExist = 1,
            DoneCopy = 2
        }

        public static List<string> ProcessDirectory(string directory)
        {
            List<string> result = new List<string>();

            string[] directoryEntities = Directory.GetDirectories(directory);
            foreach(string directoryName in directoryEntities)
            {
                result.Add($"Directory: {new DirectoryInfo(directoryName).Name}");
            }

            string[] fileEntries = Directory.GetFiles(directory);
            foreach (string fileName in fileEntries)
            {
                result.Add($"File: {Path.GetFileName(fileName)} ");
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

        public static PutCodes CopyFile(string srcPath, string destPath)
        {
            srcPath = Path.Combine(Root, srcPath);

            if (!File.Exists(srcPath))
            {
                return PutCodes.SourceFileDoesntExist;
            }

            //FileAttributes attributes = File.GetAttributes(destPath);
            //((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
            {
                return PutCodes.DestDirectoryDoesntExist;
            }

            if (srcPath != destPath)
            {
                using FileStream sourceFile = new FileStream(srcPath, FileMode.Open);
                using FileStream destFile = new FileStream(destPath, FileMode.OpenOrCreate);
                sourceFile.CopyTo(destFile);
            }
            
            return PutCodes.DoneCopy;
        }

    }
}
