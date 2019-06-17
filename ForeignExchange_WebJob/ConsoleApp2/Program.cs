using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;

namespace ConsoleApp2
{
    class Program
    {
        private static T Retrive_rates<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                try
                {
                    json_data = w.DownloadString(url);
                    return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
                }
                catch (Exception) {
                    return new T();
                }
                
            }
        }
        public class  cryptoNator
        {
            public Dictionary<string, string> ticker { get; set; }
            public decimal getrate()
            {
                try
                {
                    return decimal.Parse(ticker["price"]);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        static void Main(string[] args)
        {
            int id;
            string from = "", to = "";

            while (true)
            {
                using (SqlConnection connection = new SqlConnection("data source=amanzadafiya.database.windows.net;initial catalog=hooli;user id=[User name];password=[Password];MultipleActiveResultSets=True;"))
                {
                    connection.Open();
                    String sql = "select [Id],[From],[To] from Crypto where Time < convert(datetime,'" + DateTime.Now.AddMinutes(-10) + "')";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("{0} {1} {2}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
                                id = reader.GetInt32(0);
                                from = reader.GetString(1);
                                to = reader.GetString(2);

                                var watch = System.Diagnostics.Stopwatch.StartNew();

                                string urlparms_cryptonator = from + "-" + to;
                                string url_cryptonator = "https://api.cryptonator.com/api/ticker/" + urlparms_cryptonator;

                                string url = "https://min-api.cryptocompare.com/data/price?fsym="+from+"&tsyms="+to;
                                cryptoNator json_cn = Retrive_rates<cryptoNator>(url_cryptonator);
                                Dictionary<string,decimal> json_cc = Retrive_rates<Dictionary<string,decimal>>(url);

                                Dictionary<string, decimal> Exchange_Rates = new Dictionary<string, decimal>();
                                try
                                {
                                    Exchange_Rates.Add("cryptoCompare", Math.Round(json_cc[to], 5));
                                }
                                catch (Exception) { }
                                
                                Exchange_Rates.Add("cryptoNator", Math.Round(json_cn.getrate(), 5));
                                foreach (var item in Exchange_Rates.Where(kvp => kvp.Value == 0).ToList())
                                {
                                    Exchange_Rates.Remove(item.Key);
                                }
                                watch.Stop();
                                var elapsedMs = watch.ElapsedMilliseconds;
                                Console.WriteLine("Time taken(In ms):"+elapsedMs);
                                decimal avg_rate = 0;
                                foreach (var kv in Exchange_Rates)
                                {
                                    avg_rate += kv.Value;
                                }
                                try
                                {
                                    avg_rate /= Exchange_Rates.Count;
                                }
                                catch (Exception)
                                {
                                    avg_rate = 0;
                                    sql = "DELETE FROM Crypto WHERE Id = @id";
                                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                                    {
                                        cmd.Parameters.AddWithValue("@id", id);
                                        int rowsAffected = cmd.ExecuteNonQuery();
                                    }
                                    continue;
                                }
                                
                                string json = JsonConvert.SerializeObject(Exchange_Rates);
                                Console.WriteLine(json);
                                StringBuilder sb = new StringBuilder();
                                sb.Clear();
                                sb.Append("UPDATE Crypto SET Avg_Rate=@avg,Time=@time,Exchange_Rates=@rates WHERE Id = @id");
                                sql = sb.ToString();
                                using (SqlCommand cmd = new SqlCommand(sql, connection))
                                {
                                    cmd.Parameters.AddWithValue("@id", id);
                                    cmd.Parameters.AddWithValue("@avg", Math.Round(avg_rate, 5));
                                    cmd.Parameters.AddWithValue("@rates", json);
                                    cmd.Parameters.AddWithValue("@time", DateTime.Now);
                                    int rowsAffected = cmd.ExecuteNonQuery();
                                }
                                Console.WriteLine("Checking time.");
                            }
                        }
                    }
                }
            }
        }
    }
}
