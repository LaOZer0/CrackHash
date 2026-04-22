using System.Text;

namespace Worker.Utils;

public static class CombinationIterator
{
    public static long CountTotal(char[] alphabet, int maxLength)
    {
        long total = 0;
        for (int len = 1; len <= maxLength; len++)
        {
            total += (long)Math.Pow(alphabet.Length, len);
        }
        return total;
    }

    public static string GetByIndex(char[] alphabet, int maxLength, long globalIndex)
    {
        long offset = 0;
            
        for (int length = 1; length <= maxLength; length++)
        {
            var countInLength = (long)Math.Pow(alphabet.Length, length);
                
            if (globalIndex < offset + countInLength)
            {
                var localIndex = globalIndex - offset;
                var sb = new StringBuilder(length);
                var temp = localIndex;
                    
                for (int j = 0; j < length; j++)
                {
                    sb.Append(alphabet[temp % alphabet.Length]);
                    temp /= alphabet.Length;
                }
                    
                return sb.ToString();
            }
                
            offset += countInLength;
        }
            
        throw new ArgumentOutOfRangeException(nameof(globalIndex));
    }
}