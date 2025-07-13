using System.Security.Cryptography;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Support;

internal static class RandomPasswordGenerator
{
    public static string Generate(int length)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = validChars[bytes[i] % validChars.Length];
        }

        return new string(chars);
    }
}
