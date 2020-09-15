using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Mudak
{
    public class Logger
    {
        static string filePath = "log.txt";
        public static void Log(string message)
        {
            message = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] {message}";
            if (!File.Exists(filePath)) File.Create(filePath);

            try
            {
                File.AppendAllText(filePath, message);
            }
            catch (Exception) { Console.WriteLine("Error writing log"); };
        }
    }
}
