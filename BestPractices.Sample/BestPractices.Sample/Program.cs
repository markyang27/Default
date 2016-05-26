using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestPractices.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ADONetDemo.AlwaysCloseConnectionAndDataReader_Good();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("press enter to clear connection pools.");
            Console.ReadLine();

            try
            {
                ADONetDemo.AlwaysCloseConnectionAndDataReader_Good();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("connection pools clear.");
            Console.ReadLine();

        }
    }
}
