using Microsoft.Win32;
using System;
using System.Text;

namespace ClaudeAI
{
    /// <summary>
    /// Manages storage and retrieval of settings, particularly the API key
    /// </summary>
    public static class SettingsManager
    {
        private const string REGISTRY_KEY_PATH = @"SOFTWARE\ClaudeAI";
        private const string API_KEY_VALUE_NAME = "ApiKey";

        /// <summary>
        /// Saves the API key to the Windows registry with basic encoding
        /// </summary>
        /// <param name="apiKey">The API key to save</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SaveApiKey(string apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    return DeleteApiKey();
                }

                // Basic encoding (not secure, but better than plain text)
                string encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));

                // Save to registry
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH))
                {
                    if (key != null)
                    {
                        key.SetValue(API_KEY_VALUE_NAME, encodedKey);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the API key from storage
        /// </summary>
        /// <returns>The decoded API key, or null if not found or decoding fails</returns>
        public static string GetApiKey()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH))
                {
                    if (key != null)
                    {
                        string encodedKey = key.GetValue(API_KEY_VALUE_NAME) as string;
                        if (!string.IsNullOrEmpty(encodedKey))
                        {
                            // Decode the API key
                            byte[] decodedBytes = Convert.FromBase64String(encodedKey);
                            return Encoding.UTF8.GetString(decodedBytes);
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes the stored API key
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public static bool DeleteApiKey()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(API_KEY_VALUE_NAME, false);
                        return true;
                    }
                }
                return true; // Consider it successful if the key doesn't exist
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an API key is stored
        /// </summary>
        /// <returns>True if an API key exists, false otherwise</returns>
        public static bool HasApiKey()
        {
            return !string.IsNullOrEmpty(GetApiKey());
        }
    }
}