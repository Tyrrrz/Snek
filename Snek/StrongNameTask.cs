using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Snek;

public class StrongNameTask : Task
{
    [Required]
    public string Seed { get; set; } = "";

    [Required]
    public string CacheDirectoryPath { get; set; } = "";

    [Output]
    public string KeyFilePath { get; set; } = "";

    public override bool Execute()
    {
        var seedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Seed)));
        var keyFilePath = Path.Combine(CacheDirectoryPath, seedHash + ".snk");

        if (File.Exists(keyFilePath))
        {
            KeyFilePath = keyFilePath;
            return true;
        }

        Directory.CreateDirectory(CacheDirectoryPath);

        var keyPair = StrongNameKeyPair.Generate(Seed);

        // Write to a unique temp file and move it into place atomically, to tolerate
        // concurrent inner builds (e.g. multi-targeting) racing to produce the same file.
        var tempFilePath = keyFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(tempFilePath, keyPair);

            try
            {
                File.Move(tempFilePath, keyFilePath);
            }
            catch (IOException) when (File.Exists(keyFilePath))
            {
                // Another concurrent build already produced the file; that's fine.
            }
        }
        finally
        {
            File.Delete(tempFilePath);
        }

        KeyFilePath = keyFilePath;

        return true;
    }
}
