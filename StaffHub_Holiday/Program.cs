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
    public class HolidayRestAPI
    {
        public void updatedayNotes(string teamCountry, HolidayData HolidayDataObject, string TenantID, string TeamID, string ShiftrURL)
        {
            string URL = ShiftrURL + "tenants/" + TenantID + "/teams/" + TeamID + "/notes";
            //Console.WriteLine(URL);
           string s= "{\"id\":\"NOTE_210cca60-65c9-45a1-a45e-4d0d297b3554\"," +
                    "\"noteType\":\"Day\","+
                     "\"startTime\":\"2017-07-27T18:30:00.000Z\"," +
                     "\"text\":\"bla\"," +
            "\"endTime\":\"2017-07-28T18:30:00.000Z\"}";
            Console.WriteLine(s);
            /*
            string data = @"{'notes': [{
      'id': 'string',
      'teamId': 'string',
      'noteType': 'Day',
      'startTime': '2017-07-21T09:42:32.453Z',
      'endTime': '2017-07-21T09:42:32.453Z',
      'text': 'string'
    }
  ]
}";*/
            var client = new RestClient();
            client.EndPoint = URL;
            client.Method = HttpVerb.POST;
            client.ContentType = "application/json";
            client.PostData = s;
            client.AuthToken = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSIsImtpZCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSJ9.eyJhdWQiOiJhYTU4MDYxMi1jMzQyLTRhY2UtOTA1NS04ZWRlZTQzY2NiODkiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLXBwZS5uZXQvZjY4NmQ0MjYtOGQxNi00MmRiLTgxYjctYWI1NzhlMTEwY2NkLyIsImlhdCI6MTUwMTAwMTI2MywibmJmIjoxNTAxMDAxMjYzLCJleHAiOjE1MDEwMDUxNjMsImFpbyI6IkFTUUEyLzhHQUFBQVN3SXVaT1oxd0IrSUJ0M0J0Yzl3bWdVV1c3TWV2K042K2xQd2I1TXF1Tnc9IiwiYW1yIjpbInJzYSIsIm1mYSJdLCJhdF9oYXNoIjoiYnoxemttWFVtSGk3di1FRzVGaHZlQSIsImZhbWlseV9uYW1lIjoiSmFpbiIsImdpdmVuX25hbWUiOiJQZWV5dXNoIiwiaW5fY29ycCI6InRydWUiLCJpcGFkZHIiOiIxNjcuMjIwLjIzOC4xNDEiLCJuYW1lIjoiUGVleXVzaCBKYWluIiwibm9uY2UiOiJlZGZkOTI3Ny00NWI2LTQ5MzQtYmFiNy1mNmEyYWI2Yjc1NzkiLCJvaWQiOiJlYWY5ZTQyOS1jYmQ0LTRhNmEtYTUwNS04Nzc5NjFhMWNlYzAiLCJvbnByZW1fc2lkIjoiUy0xLTUtMjEtMjE0Njc3MzA4NS05MDMzNjMyODUtNzE5MzQ0NzA3LTIyNDEwOTUiLCJwbGF0ZiI6IjMiLCJwdWlkIjoiMTAwMzAwMDBBMjJGMkZBRCIsInN1YiI6IldFTWw4Nm51SjZQenRaVkxSSHFzWm1YbVVoTFRhNWVJVGNNcTRsMDdhY3ciLCJ0aWQiOiJmNjg2ZDQyNi04ZDE2LTQyZGItODFiNy1hYjU3OGUxMTBjY2QiLCJ1bmlxdWVfbmFtZSI6InBlamFpbkBtaWNyb3NvZnQuY29tIiwidXBuIjoicGVqYWluQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.f5xiDXKgvWe6RaCxcaB5627q_u9Ot8Yx6HR_xvDgkRNEplVzJCa8SECZrxfNsOCeRZb14fXS798_MIce-mG3vlCCfq1ZpH5uSJ0tEG9E_BCbZA4enfSgyZE71qTn44lEQW6Pt2HoyzAXRKx4-6JCs6_3Dckt92yksjOjL0pGxztdhxtD-m2k_MnaI3HlP6R14LBR3a2yeZZ3EuSYkhAzjVD7UYlVmmM2LEq016PuaIQ7Zyr1QoVea_os4AKUOp844rh6B3ktzubcZ8AZtJVpPq7jXu_yMtcAKOGs9E0B0h75IUSVxyQD_jgQKn5kLLXKK00LjYatkwyqaZd7_siAvg";
            var result = client.MakeRequest();
            //Console.WriteLine(result);
        }

    }
    class Program
    {   
        
        static void Main(string[] args)
        {
            string InputFileAddress = "input.txt";
            RestClient rest = new RestClient();
            var client = new RestClient();
            string TenantID = "f686d426-8d16-42db-81b7-ab578e110ccd";
            string TeamID = "TEAM_19b97c43-1398-4e9f-9678-01cd7d20dc71";
            string ShiftrURL = "http://localhost:45094/api/";
            client.EndPoint = @"http://localhost:45094/api/tenants/f686d426-8d16-42db-81b7-ab578e110ccd/teams/TEAM_19b97c43-1398-4e9f-9678-01cd7d20dc71/teamsettings"; ;
            client.Method = HttpVerb.GET;
            client.ContentType = "application/json";
            client.AuthToken= "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSIsImtpZCI6Imh4dmc0cjNUQjRRZEFjZ1ZHdUp4ZlMyNTgzWSJ9.eyJhdWQiOiJhYTU4MDYxMi1jMzQyLTRhY2UtOTA1NS04ZWRlZTQzY2NiODkiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLXBwZS5uZXQvZjY4NmQ0MjYtOGQxNi00MmRiLTgxYjctYWI1NzhlMTEwY2NkLyIsImlhdCI6MTUwMTAwMTI2MywibmJmIjoxNTAxMDAxMjYzLCJleHAiOjE1MDEwMDUxNjMsImFpbyI6IkFTUUEyLzhHQUFBQVN3SXVaT1oxd0IrSUJ0M0J0Yzl3bWdVV1c3TWV2K042K2xQd2I1TXF1Tnc9IiwiYW1yIjpbInJzYSIsIm1mYSJdLCJhdF9oYXNoIjoiYnoxemttWFVtSGk3di1FRzVGaHZlQSIsImZhbWlseV9uYW1lIjoiSmFpbiIsImdpdmVuX25hbWUiOiJQZWV5dXNoIiwiaW5fY29ycCI6InRydWUiLCJpcGFkZHIiOiIxNjcuMjIwLjIzOC4xNDEiLCJuYW1lIjoiUGVleXVzaCBKYWluIiwibm9uY2UiOiJlZGZkOTI3Ny00NWI2LTQ5MzQtYmFiNy1mNmEyYWI2Yjc1NzkiLCJvaWQiOiJlYWY5ZTQyOS1jYmQ0LTRhNmEtYTUwNS04Nzc5NjFhMWNlYzAiLCJvbnByZW1fc2lkIjoiUy0xLTUtMjEtMjE0Njc3MzA4NS05MDMzNjMyODUtNzE5MzQ0NzA3LTIyNDEwOTUiLCJwbGF0ZiI6IjMiLCJwdWlkIjoiMTAwMzAwMDBBMjJGMkZBRCIsInN1YiI6IldFTWw4Nm51SjZQenRaVkxSSHFzWm1YbVVoTFRhNWVJVGNNcTRsMDdhY3ciLCJ0aWQiOiJmNjg2ZDQyNi04ZDE2LTQyZGItODFiNy1hYjU3OGUxMTBjY2QiLCJ1bmlxdWVfbmFtZSI6InBlamFpbkBtaWNyb3NvZnQuY29tIiwidXBuIjoicGVqYWluQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.f5xiDXKgvWe6RaCxcaB5627q_u9Ot8Yx6HR_xvDgkRNEplVzJCa8SECZrxfNsOCeRZb14fXS798_MIce-mG3vlCCfq1ZpH5uSJ0tEG9E_BCbZA4enfSgyZE71qTn44lEQW6Pt2HoyzAXRKx4-6JCs6_3Dckt92yksjOjL0pGxztdhxtD-m2k_MnaI3HlP6R14LBR3a2yeZZ3EuSYkhAzjVD7UYlVmmM2LEq016PuaIQ7Zyr1QoVea_os4AKUOp844rh6B3ktzubcZ8AZtJVpPq7jXu_yMtcAKOGs9E0B0h75IUSVxyQD_jgQKn5kLLXKK00LjYatkwyqaZd7_siAvg";
            /* var result = client.MakeRequest();
             JObject json = JObject.Parse(result);
             JsonParser parse = new JsonParser();
             string teamCountry = parse.getTeamLocation(json);
             ReadHolidaydata ReadData = new ReadHolidaydata();
             HolidayData HolidayDataObject = ReadData.ReadDataFromFile(InputFileAddress);*/
            HolidayData HolidayDataObject=null;
            string teamCountry = null;
            HolidayRestAPI HolidayRestAPIObject = new HolidayRestAPI();
            HolidayRestAPIObject.updatedayNotes(teamCountry, HolidayDataObject,TenantID,TeamID,ShiftrURL);
            //Console.WriteLine(HolidayDataObject.CountrySpecificHolidays["India"]["2017"].First().HolidayName);
            //Console.WriteLine(teamCountry.ToString());
            Console.ReadLine();
        }
    }
}
