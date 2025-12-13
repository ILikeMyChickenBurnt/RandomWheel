using System.Security.Cryptography;

namespace RandomWheel.Services
{
    public class RandomService
    {
        public int NextIndex(int exclusiveUpperBound)
        {
            return RandomNumberGenerator.GetInt32(exclusiveUpperBound);
        }
    }
}
