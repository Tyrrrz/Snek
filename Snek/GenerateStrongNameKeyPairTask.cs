using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Snek;

public class GenerateStrongNameKeyPairTask : Task
{
    [Required]
    public string Seed { get; set; } = "";

    [Required]
    public string CacheDirectory { get; set; } = "";

    [Output]
    public string KeyFilePath { get; set; } = "";

    public override bool Execute()
    {
        string seedHash;
        using (var sha256 = SHA256.Create())
            seedHash = Convert.ToHexString(
                sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Seed))
            );

        var keyFilePath = Path.Combine(CacheDirectory, seedHash + ".snk");

        if (!File.Exists(keyFilePath))
        {
            Directory.CreateDirectory(CacheDirectory);

            var keyPair = StrongNameKeyGenerator.Generate(Seed);

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
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        KeyFilePath = keyFilePath;

        return true;
    }
}
