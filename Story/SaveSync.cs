using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        internal static bool TrySyncMeadowSaveToSinglePlayer(int saveSlot, out string resultMessage)
        {
            resultMessage = string.Empty;

            if (saveSlot < 0 || saveSlot > 2)
            {
                resultMessage = "Invalid save slot.";
                return false;
            }

            string suffix = saveSlot == 0
                ? string.Empty
                : (saveSlot + 1).ToString(CultureInfo.InvariantCulture);

            string sourceName = "online_sav" + suffix;
            string targetName = "sav" + suffix;
            string saveDirectory = Application.persistentDataPath;
            string sourcePath = Path.Combine(saveDirectory, sourceName);
            string targetPath = Path.Combine(saveDirectory, targetName);
            string backupDirectory = Path.Combine(saveDirectory, "save-sync-backups");
            string temporaryPath = targetPath + ".sync-tmp";

            if (!File.Exists(sourcePath))
            {
                resultMessage = "No Meadow Story save exists in slot " + (saveSlot + 1) + ".";
                return false;
            }

            string backupPath = null;

            try
            {
                FileInfo sourceInfo = new FileInfo(sourcePath);
                if (sourceInfo.Length == 0)
                {
                    resultMessage = "The Meadow Story save is empty. Nothing was changed.";
                    return false;
                }

                Directory.CreateDirectory(backupDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                backupPath = Path.Combine(backupDirectory, targetName + "_" + timestamp + ".bak");

                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                // Copy to a temporary file first. Do not touch the single-player save until this succeeds.
                File.Copy(sourcePath, temporaryPath, false);

                FileInfo temporaryInfo = new FileInfo(temporaryPath);
                if (temporaryInfo.Length != sourceInfo.Length)
                {
                    throw new IOException("The temporary copy does not match the source file size.");
                }

                if (File.Exists(targetPath))
                {
                    File.Copy(targetPath, backupPath, false);
                    try
                    {
                        File.Replace(temporaryPath, targetPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // The backup already exists, so this fallback remains recoverable.
                        File.Copy(temporaryPath, targetPath, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, targetPath);
                    backupPath = null;
                }

                resultMessage =
                    "Meadow Story progress was copied to single-player slot " + (saveSlot + 1) + "." +
                    (backupPath != null
                        ? "\nPrevious single-player save backed up to: " + Path.GetFileName(backupPath)
                        : "\nThe single-player slot was empty; no backup was needed.") +
                    "\nRestart Rain World before loading the campaign.";

                return true;
            }
            catch (Exception exception)
            {
                resultMessage = "Save synchronization failed: " + exception.Message +
                    "\nThe original Meadow Story save was not modified." +
                    (backupPath != null && File.Exists(backupPath)
                        ? "\nA backup of the previous single-player save exists."
                        : string.Empty);
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
                    // A leftover temporary file is safe to remove manually.
                }
            }
        }
    }
}
