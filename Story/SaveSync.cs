using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private static bool TrySyncMeadowSaveToSingleplayer(int saveSlot, out string resultMessage)
        {
            resultMessage = string.Empty;

            if (saveSlot < 0 || saveSlot > 2)
            {
                resultMessage = "Invalid save slot.";
                return false;
            }

            string slotSuffix = saveSlot == 0
                ? string.Empty
                : (saveSlot + 1).ToString(CultureInfo.InvariantCulture);

            string sourceName = "online_sav" + slotSuffix;
            string targetName = "sav" + slotSuffix;
            string saveDirectory = Application.persistentDataPath;
            string sourcePath = Path.Combine(saveDirectory, sourceName);
            string targetPath = Path.Combine(saveDirectory, targetName);
            string backupDirectory = Path.Combine(saveDirectory, "save-sync-backups");
            string temporaryPath = targetPath + ".sync-tmp";

            if (!File.Exists(sourcePath))
            {
                resultMessage =
                    "No Meadow Story save was found for slot " + (saveSlot + 1) +
                    ". If you are a client, enable Sync Save in the Meadow lobby and save your progress first.";
                return false;
            }

            try
            {
                FileInfo sourceInfo = new FileInfo(sourcePath);
                if (sourceInfo.Length == 0)
                {
                    resultMessage = "The Meadow Story save is empty. Nothing was copied.";
                    return false;
                }

                Directory.CreateDirectory(backupDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                string backupPath = Path.Combine(
                    backupDirectory,
                    targetName + "_" + timestamp + ".bak");

                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                // Stage the source first. The target is not touched until the copy is complete.
                File.Copy(sourcePath, temporaryPath, false);

                FileInfo temporaryInfo = new FileInfo(temporaryPath);
                if (temporaryInfo.Length != sourceInfo.Length)
                {
                    throw new IOException("The temporary save copy has an unexpected size.");
                }

                if (File.Exists(targetPath))
                {
                    // Keep a recoverable copy before replacing the single-player save.
                    File.Copy(targetPath, backupPath, false);

                    try
                    {
                        File.Replace(temporaryPath, targetPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Fallback for runtimes that do not implement File.Replace.
                        File.Copy(temporaryPath, targetPath, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, targetPath);
                }

                resultMessage =
                    "Meadow Story save copied to single-player slot " + (saveSlot + 1) +
                    ". Restart Rain World before loading it." +
                    (File.Exists(backupPath)
                        ? " Previous save backup: save-sync-backups/" + Path.GetFileName(backupPath)
                        : " The single-player slot was empty, so no backup was needed.");

                return true;
            }
            catch (Exception exception)
            {
                resultMessage =
                    "Save synchronization failed: " + exception.Message +
                    " Check the save-sync-backups folder before retrying.";
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch
                {
                    // A leftover temporary file is harmless; the original source remains untouched.
                }
            }
        }
    }
}
