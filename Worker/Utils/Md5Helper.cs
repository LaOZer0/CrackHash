using System.Security.Cryptography;
using System.Text;

namespace Worker.Utils;

public static class Md5Helper
{
    public static string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}