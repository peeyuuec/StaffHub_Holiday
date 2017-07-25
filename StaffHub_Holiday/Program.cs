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
using Newtonsoft.Json;
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
                        country=country.Replace("[", string.Empty);
                        country=country.Replace("]", string.Empty);

                    }
                    else
                    {
                        HolidayDataObject.InsertHolidays(country, line);
                        //Console.WriteLine(country+" ||||"+line);
                        counter++;
                    }
                }
                
            }

            file.Close();
            return HolidayDataObject;
        }


    }
    public class DayNote
    {
        public string ID, NoteType,StartTime,EndTime,Text;
        
    }
    public class HolidayRestAPI
    {
        public void updatedayNotes(string teamCountry, HolidayData HolidayDataObject, string TenantID, string TeamID, string ShiftrURL,string AuthToken)
        {
            string URL = ShiftrURL + "tenants/" + TenantID + "/teams/" + TeamID + "/notes/Bulk";
            Dictionary<string, List<HolidayNameDate>> HolidaysListOfTeamCountry = HolidayDataObject.CountrySpecificHolidays[teamCountry];
            List<HolidayNameDate> ListHolidaysIn2017 = HolidaysListOfTeamCountry["2017"];
            int TotalHolidays = ListHolidaysIn2017.Count;
            var DayNoteArray = new DayNote[TotalHolidays];
            int count = 0;
            Random random1 = new Random();
            foreach (var HolidayVal in ListHolidaysIn2017)
            {
                DayNoteArray[count] = new DayNote();
                DayNoteArray[count].ID = "NOTE_210cca60-65c9-45a1-a45e-" + random1.Next(1000,9999).ToString()+ random1.Next(1000, 9999).ToString() + random1.Next(1000, 9999).ToString();
                DayNoteArray[count].NoteType = "Day";
                DayNoteArray[count].StartTime = HolidayVal.HolidayYear + "-" + HolidayVal.HolidayMonth.PadLeft(2,'0') + "-" + HolidayVal.HolidayDay.PadLeft(2, '0') + "T12:00:00.000Z";
                DayNoteArray[count].EndTime = HolidayVal.HolidayYear + "-" + HolidayVal.HolidayMonth.PadLeft(2, '0') + "-" + HolidayVal.HolidayDay.PadLeft(2, '0') + "T13:00:00.000Z";
                DayNoteArray[count].Text = "Holiday: "+HolidayVal.HolidayName;
                count++;
            }

            
            string json = JsonConvert.SerializeObject(DayNoteArray);
            json = "{\"notes\":" + json + "}";
            Console.WriteLine(json);
            
            var client = new RestClient();
            client.EndPoint = URL;
            client.Method = HttpVerb.POST;
            client.ContentType = "application/json";
            client.PostData = json;
            client.AuthToken = AuthToken;
            var result = client.MakeRequest();
            Console.WriteLine(result);
        }

    }
    class Program
    {   
        
        static void Main(string[] args)
        {
            string InputFileAddress = "input.txt";
            RestClient rest = new RestClient();
            var client = new RestClient();
            string AuthToken= "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSIsImtpZCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSJ9.eyJhdWQiOiJhYTU4MDYxMi1jMzQyLTRhY2UtOTA1NS04ZWRlZTQzY2NiODkiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLXBwZS5uZXQvZjY4NmQ0MjYtOGQxNi00MmRiLTgxYjctYWI1NzhlMTEwY2NkLyIsImlhdCI6MTUwMTAxNTQ0NywibmJmIjoxNTAxMDE1NDQ3LCJleHAiOjE1MDEwMTkzNDcsImFpbyI6IkFTUUEyLzhHQUFBQXZyVUo0bnJ4bXR5TWtKTkxyUHNIYW1PUEpJdGc4S1pMMGRKSW1hU05tQUk9IiwiYW1yIjpbInJzYSIsIm1mYSJdLCJhdF9oYXNoIjoidDNmd29BcldwNGFHZS0wM3BuaWl3dyIsImZhbWlseV9uYW1lIjoiSmFpbiIsImdpdmVuX25hbWUiOiJQZWV5dXNoIiwiaXBhZGRyIjoiMTA2LjIxOS4wLjEwNyIsIm5hbWUiOiJQZWV5dXNoIEphaW4iLCJub25jZSI6IjlkZmVkOGY2LWRjYjMtNGFkMS05Yzk5LWNlYWEzMWM5OTFjNiIsIm9pZCI6ImVhZjllNDI5LWNiZDQtNGE2YS1hNTA1LTg3Nzk2MWExY2VjMCIsIm9ucHJlbV9zaWQiOiJTLTEtNS0yMS0yMTQ2NzczMDg1LTkwMzM2MzI4NS03MTkzNDQ3MDctMjI0MTA5NSIsInBsYXRmIjoiMyIsInB1aWQiOiIxMDAzMDAwMEEyMkYyRkFEIiwic3ViIjoiV0VNbDg2bnVKNlB6dFpWTFJIcXNabVhtVWhMVGE1ZUlUY01xNGwwN2FjdyIsInRpZCI6ImY2ODZkNDI2LThkMTYtNDJkYi04MWI3LWFiNTc4ZTExMGNjZCIsInVuaXF1ZV9uYW1lIjoicGVqYWluQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJwZWphaW5AbWljcm9zb2Z0LmNvbSIsInZlciI6IjEuMCJ9.A5CCKCXPdgL8TDw8qEiU5kr5q-GaxhBWYVQ0Qxn40IjN9KfxUok_EiMRxQvQSL8gJw5lGgIVuWM2_7VEhq7JdwhtgTtxgs3jRh954978sGXQ2H9tJLoR7qKiPtprOCsPyjbvQXZZ4QRgL_EimKbBgHBRXQ0NMkmaHNF2Y6gBaGQEwN5xQoDZlxETXIPFizUf89Uw3XK5OSSNVhERuf2-g8r04RkY03HLXx3U9V8OwLjpYHqCOozUF9UHk4VVX2XGdcm-OybPH_HA4TdRuB2UdzkoIqbQRIEyTJExzhV5lFtTay82d7VLSYia9vHF9RwowHgFfFZN-8sgw3t8m_97aw";
            string TenantID = "f686d426-8d16-42db-81b7-ab578e110ccd";
            string TeamID = "TEAM_f85769cc-99fe-4568-92c6-3c52abb858d5";
            string ShiftrURL = "http://localhost:45094/api/";
            client.EndPoint = @"http://localhost:45094/api/tenants/f686d426-8d16-42db-81b7-ab578e110ccd/teams/TEAM_19b97c43-1398-4e9f-9678-01cd7d20dc71/teamsettings"; ;
            client.Method = HttpVerb.GET;
            client.ContentType = "application/json";
            client.AuthToken = AuthToken;
                 var result = client.MakeRequest();
             JObject json = JObject.Parse(result);
             JsonParser parse = new JsonParser();
             string teamCountry = parse.getTeamLocation(json);
             ReadHolidaydata ReadData = new ReadHolidaydata();
             HolidayData HolidayDataObject = ReadData.ReadDataFromFile(InputFileAddress);
            //HolidayData HolidayDataObject=null;
            //string teamCountry = "India";
            HolidayRestAPI HolidayRestAPIObject = new HolidayRestAPI();
            HolidayRestAPIObject.updatedayNotes(teamCountry, HolidayDataObject,TenantID,TeamID,ShiftrURL,AuthToken);
            //Console.WriteLine(HolidayDataObject.CountrySpecificHolidays["India"]["2017"].First().HolidayName);
            //Console.WriteLine(teamCountry.ToString());
            Console.ReadLine();
        }
    }
}
