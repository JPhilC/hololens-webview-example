using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using System.Collections;
#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
#endif


public static class ContentService 
{
    private const string ConfigFilename = "content.config";

    public static async Task<ContentData> LoadContent()
    {
        ContentData contentData = LoadCurrentConfig();  // Load the current configuration (last saved configuration)
#if ENABLE_WINMD_SUPPORT
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        // Now process each zip file
        bool needToSave = false;
        var zipFiles = await localFolder.GetFilesAsync(); // Directory.GetFiles(zipFolder, "*.zip");
        foreach (var file in zipFiles.Where(z => z.FileType == ".zip"))
        {
            var fileprops = await file.GetBasicPropertiesAsync();
            DateTimeOffset dateModified = fileprops.DateModified;
            ContentFile contentFile = contentData.Files.FirstOrDefault(f => f.Filename == file.Name);
            if (contentFile == null)
            {
                // Add a new record
                contentFile = new ContentFile { Filename = file.Name };
                contentData.Files.Add(contentFile);
                contentFile.DateModified = dateModified;
                contentFile.Topics = GetTopicsFromZipFile(file.Path);
                contentFile.Exists = true;
                needToSave = true;
            }
            else
            {
                contentFile.Exists = true;
            }

            if (dateModified > contentFile.DateModified)
            {
                // Update existing details.
                contentFile.DateModified = dateModified;
                contentFile.Topics = GetTopicsFromZipFile(file.Path);
                needToSave = true;
            }
        }

        needToSave = (needToSave || contentData.Files.Any(f => !f.Exists));
        if (needToSave)
        {
            // Remove records for zip files that no longer exist.
            ContentFile[] toDelete = contentData.Files.Where(f => !f.Exists).ToArray();
            foreach (var cf in toDelete)
            {
                contentData.Files.Remove(cf);
            }
            SaveContent(contentData);
        }
#endif
        return contentData;
    }

    private static ContentData LoadCurrentConfig()
    {
        ContentData contentData = new ContentData();
        var localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFile = Path.Combine(localAppDataFolder, ConfigFilename);
        if (File.Exists(configFile))
        {
            using (var file = File.OpenText(configFile))
            {
                var serializer = new JsonSerializer();
                contentData = (ContentData)serializer.Deserialize(file, typeof(ContentData));
            }
        }

        return contentData;
    }
    private static List<ContentTopic> GetTopicsFromZipFile(string zipPath)
    {
        List<ContentTopic> topics = new List<ContentTopic>();
#if ENABLE_WINMD_SUPPORT
        StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e => e.FullName == "topics.json");
            if (entry != null)
            {
                // Gets the full path to ensure that relative segments are removed.
                string destinationPath = Path.GetFullPath(Path.Combine(temporaryFolder.Path, entry.FullName));

                // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                // are case-insensitive.
                if (destinationPath.StartsWith(temporaryFolder.Path, StringComparison.Ordinal))
                {
                    entry.ExtractToFile(destinationPath, true);
                    if (File.Exists(destinationPath))
                    {
                        using (var file = File.OpenText(destinationPath))
                        {
                            var serializer = new JsonSerializer();
                            topics = (List<ContentTopic>)serializer.Deserialize(file, typeof(List<ContentTopic>));
                        }
                        File.Delete(destinationPath);
                    }

                }

            }

        }
#endif
        return topics;
    }

    public static void SaveContent(ContentData contentData)
    {
        var localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFile = Path.Combine(localAppDataFolder, ConfigFilename);
        File.WriteAllText(configFile, JsonConvert.SerializeObject(contentData, Formatting.Indented));
    }


    public static async Task<string> UnzipContent(string zipPath)
    {
        string extractedTo = string.Empty;
#if ENABLE_WINMD_SUPPORT
        var localFolder = ApplicationData.Current.LocalFolder;
        // Now process each zip file
        var zipFile = await localFolder.GetFileAsync(zipPath);
        if (zipFile != null)
        {
            StorageFolder stateFolder =
                await localFolder.CreateFolderAsync("Content", CreationCollisionOption.OpenIfExists);

            // Remove the old stuff
            var oldItems = await stateFolder.GetItemsAsync();
            foreach (var item in oldItems)
            {
                await item.DeleteAsync();
            }

            // Extract the new stuff.
            using (ZipArchive archive = ZipFile.OpenRead(zipFile.Path))
            {
                archive.ExtractToDirectory(stateFolder.Path);
                extractedTo = stateFolder.Path;
            }
        }
#endif
        return extractedTo;
    }

    public static string GetZipFilePath()
    {
#if ENABLE_WINMD_SUPPORT
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        return localFolder.Path;
#else
        return string.Empty;
#endif
    }
}

