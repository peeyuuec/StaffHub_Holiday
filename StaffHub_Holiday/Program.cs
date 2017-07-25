using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

public enum HttpVerb
{
    GET,
    POST,
    PUT,
    DELETE
}

namespace StaffHub_Holiday
{

    public class RestClient
    {
        public string EndPoint { get; set; }
        public HttpVerb Method { get; set; }
        public string ContentType { get; set; }
        public string PostData { get; set; }
        public string AuthToken { get; set; }

        public RestClient()
        {
            EndPoint = "";
            Method = HttpVerb.GET;
            ContentType = "text/xml";
            PostData = "";
        }
        public RestClient(string endpoint)
        {
            EndPoint = endpoint;
            Method = HttpVerb.GET;
            ContentType = "text/xml";
            PostData = "";
        }
        public RestClient(string endpoint, HttpVerb method)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = "text/xml";
            PostData = "";
        }

        public RestClient(string endpoint, HttpVerb method, string postData)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = "text/xml";
            PostData = postData;
        }


        public string MakeRequest()
        {
            return MakeRequest("");
        }

        public string MakeRequest(string parameters)
        {
            var request = (HttpWebRequest)WebRequest.Create(EndPoint + parameters);

            request.Method = Method.ToString();
            request.ContentLength = 0;
            request.ContentType = ContentType;
            request.Headers.Add("Authorization", AuthToken);
            if (!string.IsNullOrEmpty(PostData) && Method == HttpVerb.POST)
            {
                var encoding = new UTF8Encoding();
                var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(PostData);
                request.ContentLength = bytes.Length;

                using (var writeStream = request.GetRequestStream())
                {
                    writeStream.Write(bytes, 0, bytes.Length);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var responseValue = string.Empty;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
                    throw new ApplicationException(message);
                }

                // grab the response
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseValue = reader.ReadToEnd();
                        }
                }

                return responseValue;
            }
        }

    } // class
    public class JsonParser
    {
        public Dictionary<string, string> locationMapping = new Dictionary<string, string>
        {
            { "Calcutta", "India" },
            { "US", "United States" }
        };
        public string decodeGeographic(string geoLocation)
        {
            string error = "errorGeolocationNotFound";
            foreach (KeyValuePair<string, string> entry in locationMapping)
            {
                // do something with entry.Value or entry.Key
                if (geoLocation.Contains(entry.Key)) return entry.Value;
            }
            return error;

        }
        public string getTeamLocation(JObject json)
        {
            string error = "errorKeyNotFound";
            var teamsettings = json["teamSettings"];
            foreach(var property in teamsettings)
            {
                if(property["key"].ToString()== "TimeZoneOlsonCode")
                {
                    return decodeGeographic(property["value"].ToString());
                    
                }

            }
            return error;

        }

    }
    public struct HolidayNameDate
    {
        public string HolidayName, HolidayYear,HolidayMonth,HolidayDay;

        public HolidayNameDate(string HolidayName, string HolidayYear,string HolidayMonth, string HolidayDay)
        {
            this.HolidayName = HolidayName;
            this.HolidayYear = HolidayYear;
            this.HolidayMonth = HolidayMonth;
            this.HolidayDay = HolidayDay;
        }
    }
    public class HolidayData
    {
        public Dictionary<string, List<HolidayNameDate>> Holidays;
        public Dictionary<string, Dictionary<string, List<HolidayNameDate>>> CountrySpecificHolidays;
        public HolidayData()
        {
            //Holidays = new Dictionary<string, List<HolidayNameDate>>();
            CountrySpecificHolidays = new Dictionary<string, Dictionary<string, List<HolidayNameDate>>>();

        }

        public void InsertHolidays(string country,string line)
        {
            var Splits = line.Split(',');
            var DateSplits = Splits[1].Split('/');
            HolidayNameDate HolidayStructure ;
            HolidayStructure.HolidayName = Splits[0];
            HolidayStructure.HolidayYear = DateSplits[0];
            HolidayStructure.HolidayMonth = DateSplits[1];
            HolidayStructure.HolidayDay = DateSplits[2];

            if (CountrySpecificHolidays.ContainsKey(country))
            {
                Holidays = CountrySpecificHolidays[country];
                if (Holidays.ContainsKey(DateSplits[0]))
                {
                    Holidays[DateSplits[0]].Add(HolidayStructure);
                }
                else
                {
                    List<HolidayNameDate> newList = new List<HolidayNameDate>();
                    newList.Add(HolidayStructure);
                    Holidays[DateSplits[0]] = newList;
                }
               
                CountrySpecificHolidays[country] = Holidays;

            }
            else
            {
                Holidays= new Dictionary<string, List<HolidayNameDate>>();
                List<HolidayNameDate> newList = new List<HolidayNameDate>();
                newList.Add(HolidayStructure);
                Holidays[DateSplits[0]] = newList;
                CountrySpecificHolidays[country] = Holidays;
            }
            
        } 
        

    }
    public class ReadHolidaydata
    {
        public HolidayData ReadDataFromFile(string FileName)
        {
            HolidayData HolidayDataObject = new HolidayData();
            //HolidayDataObject.InsertHolidays("india", "independance day of india,2017/8/15");
            int counter = 0;
            string line;

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(FileName);
            string country = "";
            while ((line = file.ReadLine()) != null)
            {
                if (line.Length >0) {
                    if (line[0] == '[')
                    {
                        var splits = line.Split(' ');
                        country = splits[0];
                        country.Replace("[", string.Empty);
                        country.Replace("]", string.Empty);

                    }
                    else
                    {
                        HolidayDataObject.InsertHolidays(country, line);
                    }
                }
                Console.WriteLine(country+" ||||"+line);
                counter++;
            }

            file.Close();
            return HolidayDataObject;
        }


    }
    class Program
    {   
        
        static void Main(string[] args)
        {
            string InputFileAddress = "input.txt";
            RestClient rest = new RestClient();
            var client = new RestClient();
            client.EndPoint = @"http://localhost:45094/api/tenants/f686d426-8d16-42db-81b7-ab578e110ccd/teams/TEAM_19b97c43-1398-4e9f-9678-01cd7d20dc71/teamsettings"; ;
            client.Method = HttpVerb.GET;
            client.ContentType = "application/json";
            client.AuthToken= "	Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSIsImtpZCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSJ9.eyJhdWQiOiJhYTU4MDYxMi1jMzQyLTRhY2UtOTA1NS04ZWRlZTQzY2NiODkiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLXBwZS5uZXQvZjY4NmQ0MjYtOGQxNi00MmRiLTgxYjctYWI1NzhlMTEwY2NkLyIsImlhdCI6MTUwMDk5MzcxMCwibmJmIjoxNTAwOTkzNzEwLCJleHAiOjE1MDA5OTc2MTAsImFpbyI6IlkyTmdZUGh3NnJEV1JvMGJqcHd2YytiV0p1NFRsanI4eWxpcWljUGl5Sk9PWXMwbm9hRUEiLCJhbXIiOlsicnNhIiwibWZhIl0sImF0X2hhc2giOiJwZEhaLWtLLUNDQWRLT3plQkRVYUZBIiwiZmFtaWx5X25hbWUiOiJKYWluIiwiZ2l2ZW5fbmFtZSI6IlBlZXl1c2giLCJpbl9jb3JwIjoidHJ1ZSIsImlwYWRkciI6IjE2Ny4yMjAuMjM4LjE0MSIsIm5hbWUiOiJQZWV5dXNoIEphaW4iLCJub25jZSI6IjBiOTcxYTU0LTc5ZDAtNGY1Ni04ODc3LTEwZDUyNjM1NDMxNyIsIm9pZCI6ImVhZjllNDI5LWNiZDQtNGE2YS1hNTA1LTg3Nzk2MWExY2VjMCIsIm9ucHJlbV9zaWQiOiJTLTEtNS0yMS0yMTQ2NzczMDg1LTkwMzM2MzI4NS03MTkzNDQ3MDctMjI0MTA5NSIsInBsYXRmIjoiMyIsInB1aWQiOiIxMDAzMDAwMEEyMkYyRkFEIiwic3ViIjoiV0VNbDg2bnVKNlB6dFpWTFJIcXNabVhtVWhMVGE1ZUlUY01xNGwwN2FjdyIsInRpZCI6ImY2ODZkNDI2LThkMTYtNDJkYi04MWI3LWFiNTc4ZTExMGNjZCIsInVuaXF1ZV9uYW1lIjoicGVqYWluQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJwZWphaW5AbWljcm9zb2Z0LmNvbSIsInZlciI6IjEuMCJ9.GBF_LDmO2eGIlZXrPf_xbNtSAzNGZvArSRwBXj8ZND_Zb6kEx5NlBA7AJvpHbJvkL_6FVyvLq_JYXbgLYqUYycVY8WCzqXoPR_yGdFmLmIdS7BbyvgN52GXuziXx48TcJgss1o464FLZJMXObXo6kKlDExDKS2BoW1C8GHZBVlWovoZs1QxfP_2UDKWBgG9TBf4vs-Zi1GTvCAN9KhjemmIc35mZ7p3SfrhF4buGzLV-i49Ek4ye82Tx3oSLb5vCKeJaAzW6fNLKwBOpcXOPhfrs2izonWNy7VVGLzrbNzt2P97oexJoERu02ogBMyYnlydZ8YWsHrcAPIzAn5XOuw";
            var result = client.MakeRequest();
            JObject json = JObject.Parse(result);
            JsonParser parse = new JsonParser();
            string teamCountry = parse.getTeamLocation(json);
            ReadHolidaydata ReadData = new ReadHolidaydata();
            HolidayData HolidayDataObject = ReadData.ReadDataFromFile(InputFileAddress);
            //HolidayDataObject.InsertHolidays("india","independance day,2017/8/15");
            Console.WriteLine(HolidayDataObject.CountrySpecificHolidays["india"]["2017"].First().HolidayName);
            Console.WriteLine(teamCountry.ToString());
            Console.ReadLine();
        }
    }
}
