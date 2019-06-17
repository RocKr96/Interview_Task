using Newtonsoft.Json;
using Rennga_task.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Rennga_task.Controllers
{
    public class V2Controller : ApiController
    {
        public class db
        {
            public string From { get; set; }
            public string To { get; set; }
            public Nullable<double> Avg_Rate { get; set; }
            public Dictionary<string, float> Exchange_Rates { get; set; }

        }
        public class JsonError
        {
            public string Message { get; set; }
        }
        public List<db> convertvalue(IEnumerable<dynamic> x)
        {
            List<db> js = new List<db>();
            foreach (var item in x)
            {
                db d = new db();
                d.Avg_Rate = item.Avg_Rate;
                d.From = item.From;
                d.To = item.To;
                d.Exchange_Rates = JsonConvert.DeserializeObject<Dictionary<string, float>>(item.Exchange_Rates.ToString());
                js.Add(d);
            }
            return js;
        }

        // GET api/v2
        public Object Get()
        {
            using (hooliEntities he = new hooliEntities())
            {
                var x = he.Cryptoes.Select(emp => new
                {
                    From = emp.From,
                    To = emp.To,
                    Avg_Rate = emp.Avg_Rate,
                    Exchange_Rates = emp.Exchange_Rates
                }).ToList();
                if (x.Count > 0)
                {
                    return Json(convertvalue(x));
                }
                else
                {
                    var error = new JsonError
                    {
                        Message = "There is no any pairs currently availble."
                    };
                    return Json(error);
                }
            }
        }

        // GET api/v2/{BTC or BTCUSD}
        public Object Get(string id)
        {
            if (id.Length == 6)
            {
                string from = id.Substring(0, 3);
                string to = id.Substring(3, 3);
                using (hooliEntities he = new hooliEntities())
                {
                    var x = he.Cryptoes.Where(c => c.From == from && c.To == to).Select(emp => new {
                        From = emp.From,
                        To = emp.To,
                        Avg_Rate = emp.Avg_Rate,
                        Exchange_Rates
                   = emp.Exchange_Rates
                    }).ToList();
                    if (x.Count > 0)
                    {
                        return Json(convertvalue(x));
                    }
                    else
                    {
                        var error = new JsonError
                        {
                            Message = "your input pair is not Available."
                        };
                        return Json(error);
                    }
                }
            }
            else if (id.Length == 3)
            {
                using (hooliEntities he = new hooliEntities())
                {
                    var x = he.Cryptoes.Where(c => c.From == id || c.To == id).Select(emp => new {
                        From = emp.From,
                        To = emp.To,
                        Avg_Rate = emp.Avg_Rate,
                        Exchange_Rates
                   = emp.Exchange_Rates
                    }).ToList();
                    if (x.Count > 0)
                    {
                        return Json(convertvalue(x));
                    }
                    else
                    {
                        var error = new JsonError
                        {
                            Message = "your input currency is not available."
                        };
                        return Json(error);
                    }
                }
            }
            else
            {
                var error = new JsonError
                {
                    Message = "your input pair is not like USDEUR."
                };

                return Json(error);
            }
        }

        // GET api/v2/history?s={number}
        [Route("~/api/v2/history")]
        public Object Get(int s)
        {
            DateTime dt = DateTime.Now.AddSeconds(-s);
            using (hooliEntities he = new hooliEntities())
            {
                var x = he.Cryptoes.Where(c => c.Time >= dt).Select(emp => new {
                    From = emp.From,
                    To = emp.To,
                    Avg_Rate = emp.Avg_Rate,
                    Exchange_Rates = emp.Exchange_Rates
                }).ToList();
                if (x.Count > 0)
                {
                    return Json(convertvalue(x));
                }
                else
                {
                    var error = new JsonError
                    {
                        Message = "There is not updated pairs in your given time."
                    };
                    return Json(error);
                }
            }
        }
    }
}