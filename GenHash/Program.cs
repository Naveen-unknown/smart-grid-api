using System;

namespace GenHash
{
    class Program
    {
        static void Main(string[] args)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            Console.WriteLine("CORRECT_HASH:" + hash);
        }
    }
}
