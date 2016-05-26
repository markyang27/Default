using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestPractices.Sample
{
    public class ADONetDemo
    {
        static string _connString = @"Data Source=192.168.111.212\pdsql;Initial Catalog=Northwind;Integrated Security=True;Min Pool Size=0;Connection Lifetime=30;";

        public static void AlwaysCloseConnectionAndDataReader_Good()
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT CustomerId, CompanyName FROM Customers";

                conn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Console.WriteLine("{0}\t{1}", dr.GetString(0), dr.GetString(1));
                    }
                } //after using scope, dr is closed.

                if (conn.State != ConnectionState.Closed)
                {
                    Console.WriteLine("ConnectionState.Closed");
                    conn.Close();
                }
                else
                {
 
                }
            } // conn is closed here.
        }

        public static void AlwaysCloseConnectionAndDataReader_Bad()
        {
            SqlConnection conn = new SqlConnection(_connString);

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CustomerId, CompanyName FROM Customers";

            conn.Open();

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Console.WriteLine("{0}\t{1}", dr.GetString(0), dr.GetString(1));
            }

            // close methods will not execute if exception occurs.
            dr.Close();

            conn.Close();
        }

        public static void OptimizeConnectionsWithDataAdapter_Good()
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Customers", conn);
                DataSet ds = new DataSet();

                // perform a single Fill or Update call, 
                // allow the Fill or Update method to open and close the connection implicitly.
                da.Fill(ds, "Customers");
            }
        }

        public static void OptimizeConnectionsWithDataAdapter_Good2()
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Customers", conn);
                DataSet ds = new DataSet();

                conn.Open();

                // perform a multiple Fill or Update calls, 
                // we should avoid allowing the Fill or Update method to open and close the connection implicitly.
                // instead, we open connection explicitly before those calls.
                da.Fill(ds, "Customers");
                da.Update(ds, "Customers");
                da.Update(ds, "Customers");
            }
        }

        public static void AlwaysCloseDataReader_Good()
        {
            using (SqlDataReader reader = GetReader_Good())
            {
                while (reader.Read())
                {
                    Console.WriteLine("{0}\t{1}", reader.GetString(0), reader.GetString(1));
                }
            }
        }

        public static SqlDataReader GetReader_Good()
        {
            SqlConnection conn = new SqlConnection(_connString);

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CustomerId, CompanyName FROM Customers";

            conn.Open();

            // by passing CommandBehavior.CloseConnection, 
            // ensure conn object will be closed when dr's close/dispose is called.
            SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            return dr;
        }
    }
}
