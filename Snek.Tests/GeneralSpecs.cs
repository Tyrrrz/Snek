using System;
using System.IO;
using System.Security.Cryptography;
using AwesomeAssertions;
using PowerKit;
using Xunit;

namespace Snek.Tests;

public class GeneralSpecs
{
    [Fact]
    public void I_can_run_the_build_and_get_a_key_pair_file()
    {
        // Arrange
        using var dir = TempDirectory.Create();
        var task = new StrongNameTask { Seed = "Hello world!", CacheDirectoryPath = dir.Path };

        // Act
        task.Execute();

        // Assert
        task.KeyFilePath.Should().NotBeNullOrWhiteSpace();

        File.Exists(task.KeyFilePath).Should().BeTrue();
        new FileInfo(task.KeyFilePath).Length.Should().BeGreaterThan(0);

        SHA256
            .HashData(File.ReadAllBytes(task.KeyFilePath))
            .Should()
            .Equal(
                // Generated key file should be the same for the same seed
                Convert.FromHexString(
                    "87f9c102661393670ef26babe9e85a6ad62fdcc88814406d5db4917e9b0d083b"
                )
            );
    }
}
