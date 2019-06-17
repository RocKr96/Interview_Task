using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        private static async Task<T> Retrive_rates<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                try
                {
                    json_data =  await Task.Run(() => w.DownloadString(url));
                }
                catch (System.Net.WebException) { }
                catch (Exception) {
                }
                return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
            }
        }

        public class liverates
        {
            public decimal rate { get; set; }
        }
        public class currencyLayer
        {
            public Dictionary<string, decimal> quotes { get; set; }
            public decimal getrate(string from, string to)
            {
                try
                {
                    return quotes.GetValueOrDefault("USD" + to) / quotes.GetValueOrDefault("USD" + from);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        public class exchangerates
        {
            public Dictionary<string, decimal> Rates { get; set; }

            public decimal getrate(string to)
            {
                try
                {
                    return Rates.GetValueOrDefault(to);
                }
                catch (Exception) { }
                return 0;
            }
        }
        public class fixer
        {
            public Dictionary<string, decimal> Rates { get; set; }
            public decimal getrate(string from, string to)
            {
                try
                {
                    return Rates[to] / Rates[from];
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        public class freeforex
        {
            public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }
            public decimal getrate(string urlparams)
            {
                try
                {
                    return Rates[urlparams]["rate"];
                }
                catch (Exception) {
                   
                }
                return 0;
            }
        }
        static async Task Main(string[] args)
        {
            int id;
            string from = "", to = "";

            while (true)
            {
                using (SqlConnection connection = new SqlConnection("data source=amanzadafiya.database.windows.net;initial catalog=hooli;user id=[User Name];password=[Password];MultipleActiveResultSets=True;"))
                {
                    connection.Open();
                    String sql = "select [Id],[From],[To] from [Foreign] where Time < convert(datetime,'" + DateTime.Now.AddMinutes(-10) + "')";
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

                                string urlparms_liverates = from + "_" + to;
                                string url_liverates = "https://www.live-rates.com/api/price?key=e7cc8a2d69&rate=" + urlparms_liverates;

                                string urlparms_cl = from + "," + to;
                                string url_cl = "http://apilayer.net/api/live?access_key=603a43b4db7ec81cf3691c76693dc546&currencies=" + urlparms_cl;

                                //string url_er = "https://api.exchangeratesapi.io/latest?base=" + from + "&symbols=" + to;

                                string url_fixer = "http://data.fixer.io/api/latest?access_key=2f06596247b08a4232bff40b299ef8d1&symbols=" + urlparms_cl;

                                string url_ff = "https://www.freeforexapi.com/api/live?pairs=" + from + to;
                               
                                 var lr = Retrive_rates<List<liverates>>(url_liverates);
                                 var cl = Retrive_rates<currencyLayer>(url_cl);
                                 //var er = Retrive_rates<exchangerates>(url_er);
                                 var fx = Retrive_rates<fixer>(url_fixer);
                                 var ff = Retrive_rates<freeforex>(url_ff);

                                 List<liverates> json_liverates = await lr;
                                 currencyLayer json_cl = await cl;
                                 //exchangerates json_er = await er;
                                 fixer json_fixer = await fx;
                                 freeforex json_ff = await ff;
                                 
                                Dictionary<string, decimal> forex_rates = new Dictionary<string, decimal>();
                                try
                                {
                                    forex_rates.Add("live-rates", Math.Round(json_liverates[0].rate, 5));
                                }
                                catch (Exception) { }
                                
                                forex_rates.Add("currency-layers", Math.Round(json_cl.getrate(from, to), 5));
                                //forex_rates.Add("exchange-rates", Math.Round(json_er.getrate(to), 5));
                                forex_rates.Add("fixerio", Math.Round(json_fixer.getrate(from, to), 5));
                                forex_rates.Add("freeforex", Math.Round(json_ff.getrate(from + to), 5));
                                foreach (var item in forex_rates.Where(kvp => kvp.Value == 0).ToList())
                                {
                                    forex_rates.Remove(item.Key);
                                }
                                watch.Stop();
                                var elapsedMs = watch.ElapsedMilliseconds;
                                Console.WriteLine("Time Elapsed: "+elapsedMs);
                                decimal avg_rate = 0;
                                foreach (var kv in forex_rates)
                                {
                                    avg_rate += kv.Value;
                                }
                                try
                                {
                                    avg_rate /= forex_rates.Count;
                                }
                                catch (Exception)
                                {
                                    avg_rate = 0;
                                    sql = "DELETE FROM [Foreign] WHERE Id = @id";
                                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                                    {
                                        cmd.Parameters.AddWithValue("@id", id);
                                        int rowsAffected = cmd.ExecuteNonQuery();
                                    }
                                    continue;
                                }
                                string json = JsonConvert.SerializeObject(forex_rates);
                                Console.WriteLine(json);
                                StringBuilder sb = new StringBuilder();
                                sb.Clear();
                                sb.Append("UPDATE [Foreign] SET Avg_Rate=@avg,Time=@time,Exchange_Rates=@rates WHERE Id = @id");
                                sql = sb.ToString();
                                using (SqlCommand cmd = new SqlCommand(sql, connection))
                                {
                                    cmd.Parameters.AddWithValue("@id", id);
                                    cmd.Parameters.AddWithValue("@avg", Math.Round(avg_rate, 5));
                                    cmd.Parameters.AddWithValue("@rates", json);
                                    cmd.Parameters.AddWithValue("@time", DateTime.Now);
                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    Console.WriteLine(rowsAffected + " row(s) updated");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
