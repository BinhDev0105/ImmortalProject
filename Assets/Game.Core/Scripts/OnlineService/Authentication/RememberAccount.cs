using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Unity.Logging;
using Unity.SharpZipLib.Zip;

namespace Game.Core.Scripts.OnlineService.Authentication
{
    /// <summary>
    /// A class for managing the storage and reading of user login credentials using ZIP compression
    /// </summary>
    public static class RememberAccount
    {
        [Serializable]
        public struct RememberData
        {
            public bool active;
            public string username;
            public string password;
        }
        
        private const string FileName = "user_data.zip";
        private const string EntryName = "user_data.dat";
        private static string SavePath => Path.Combine(UnityEngine.Application.persistentDataPath, FileName);
        private const string Salt = "Immortal_Security_Salt";

        /// <summary>
        /// Save account data to zip file
        /// </summary>
        /// <param name="data">Data of account</param>
        /// <returns></returns>
        public static bool SaveAccountData(RememberData data)
        {
            try
            {
                // Create or write Zip file
                using (FileStream zipFileStream = new FileStream(SavePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                    {
                        // Set compression level
                        zipStream.SetLevel(9);
                        
                        // Create a new entry for the zip file
                        ZipEntry entry = new ZipEntry(EntryName)
                        {
                            DateTime = DateTime.Now
                        };

                        // Add a new entry for the zip file
                        zipStream.PutNextEntry(entry);
                        
                        // Serialize the object directly into ZipOutputStream
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(zipStream, data);
                        
                        // Close entry
                        zipStream.CloseEntry();
                    }
                }
#if UNITY_EDITOR
                Log.Debug("Saved data to " + SavePath);
#endif
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Log.Error($"Error saving user data: {e.Message}");
#endif
                return false;
            }
        }

        /// <summary>
        /// Load account data to zip file
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool LoadAccountData(out RememberData data)
        {
            data = new RememberData();
            
            // Check data file is exist?
            if (!File.Exists(SavePath))
            {
#if UNITY_EDITOR
                Log.Warning("No user data found...");
#endif
                return false;
            }

            try
            {
                using (FileStream zipFileStream = new FileStream(SavePath, FileMode.Open, FileAccess.Read))
                using (ZipFile zipFile = new ZipFile(zipFileStream))
                {
                    // Find entry from zip file
                    ZipEntry entry = zipFile.GetEntry(EntryName);
                
                    if (entry != null)
                    {
                        // Get stream from entry
                        using (Stream zipStream = zipFile.GetInputStream(entry))
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            data = (RememberData)formatter.Deserialize(zipStream);
#if UNITY_EDITOR
                            Log.Debug("Loaded user data from " + SavePath);
#endif
                            return true;
                        }
                    }
                    else
                    {
#if UNITY_EDITOR
                        Log.Warning("No user data found...");
#endif
                        return false;
                    }
                }
            }
            catch (SerializationException se)
            {
#if UNITY_EDITOR
                Log.Error($"Error data format: {se.Message}");        
#endif
                return false;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Log.Error($"Error loading user data: {e.Message}");
#endif
                return false;
            }
        }
        
        /// <summary>
        /// Delete account data from zip file
        /// </summary>
        /// <returns></returns>
        public static bool DeleteAccountData()
        {
            if (!File.Exists(SavePath))
            {
                return true;
            }
        
            try
            {
                File.Delete(SavePath);
#if UNITY_EDITOR
                Log.Debug("Deleted user data from " + SavePath);
#endif
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Log.Error($"Error deleting user data: {e.Message}");
#endif
                return false;
            }
        }
    }
}