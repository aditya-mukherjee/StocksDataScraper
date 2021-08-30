using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace My.Function
{
    public class Result
    {
        public string Link { get; set; }
        public string ImageLink { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
    }

    public class Feed
    {
        public List<Result> results { get; set; }
    }

    public class RootObject
    {
        public Feed feed { get; set; }
    }

    public class Tweet
    {
        public string Body { get; set; }
        public string Tags { get; set; }
    }

    public class TweetStream
    {
        public List<Tweet> tweetsList { get; set; }
    }

    public class TweetJson
    {
        public TweetStream stream { get; set; }
    }


    public static class HttpExample1
    {
        static HttpClient httpClient = new HttpClient();

        [FunctionName("HttpExample1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["streamType"];
            name = name ?? "rss";

            string tags = req.Query["tags"];
            tags = tags ?? "false";
            Boolean tagsParam = Convert.ToBoolean(tags);

            string summaries = req.Query["summaries"];
            summaries = summaries ?? "false";
            Boolean summariesParam = Convert.ToBoolean(summaries);

            string json;

            switch (name)
            {
                case "rss":
                    json = GetFeed(tagsParam, summariesParam, context);
                    break;
                case "tweets":
                    json = await GetTweets(tagsParam, summariesParam, context);
                    break;
                default:
                    json = "{\"output\":\"invalid streamType parameter\"}";
                    break;
            }        

            // log.LogInformation("Full JSON: " + json + "\r\n");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(json);
            // return new OkObjectResult(responseMessage);
        }

        public static string GetFeed(Boolean tags, Boolean summaries, ExecutionContext context)
        {
            WebRequest request = HttpWebRequest.Create("https://www.business-standard.com/rss/home_page_top_stories.rss");  
            WebResponse response = request.GetResponse();  
            StreamReader reader = new StreamReader(response.GetResponseStream());  
            string urlText = reader.ReadToEnd(); // it takes the response from your url. now you can use as your need  
            //log.LogInformation(urlText.ToString());

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(urlText.ToString());

            XmlNodeList items = xDoc.GetElementsByTagName("item");

            // string res="";

            RootObject ro = new RootObject();
            Feed cs = new Feed();
            cs.results = new List<Result>();

            for(int i=0;i<items.Count;i++)
            {
                string InnerXml = items[i].InnerXml;

                int fDes = InnerXml.IndexOf("<description><![CDATA[");    
                int sDes = InnerXml.IndexOf("]]></description>");  

                string Description = InnerXml.Substring(fDes + 22, sDes - fDes - 22);

                int fLink = InnerXml.IndexOf("<link>");    
                int sLink = InnerXml.IndexOf("</link>");  

                string Link = InnerXml.Substring(fLink + 6, sLink - fLink - 6);

                int fImageLink = InnerXml.IndexOf("<imageThumb><![CDATA[");    
                int sImageLink = InnerXml.IndexOf("]]></imageThumb>");  

                string ImageLink = InnerXml.Substring(fImageLink + 21, sImageLink - fImageLink - 21);

                // log.LogInformation("Description: " + Description + "\r\n");
                // log.LogInformation("Link: " + Link + "\r\n");
                // log.LogInformation("ImageLink: " + ImageLink + "\r\n");
                // res = res + "Description: " + Description + "\r\n" +
                //             "Link: " + Link + "\r\n\r\n" +
                //             "ImageLink: "+ImageLink + "\r\n\r\n";

                Result rs = new Result();
                rs.Link = JsonConvert.ToString(Link);
                rs.ImageLink = JsonConvert.ToString(ImageLink);
                rs.Description = JsonConvert.ToString(Description);

                if(tags)
                    rs.Tags=GetTags(context, JsonConvert.ToString(Description));
                else
                    rs.Tags="";

                cs.results.Add(rs);
            }

            ro.feed = cs;

            string json = JsonConvert.SerializeObject(ro);

            return json;
        }

        public static async Task<string> GetTweets(Boolean tags, Boolean summaries, ExecutionContext context)
        {
            /*
            httpClient.BaseAddress = new Uri("https://api.twitter.com/2/tweets/search/recent");
            string urlParams = "?query=%23stocks&tweet.fields=created_at&expansions=author_id&user.fields=created_at&max_results=10";

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "AAAAAAAAAAAAAAAAAAAAAEj5QwEAAAAA3DqQ96NOlS16j1LKFc%2BTify7kzc%3DNiYqA2HFNMLb8Naqs9PHbSgPQZH5g2eS887FkVHTKY0vJwuOgl");

            HttpResponseMessage resp = httpClient.GetAsync(urlParams).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            
            if (resp.IsSuccessStatusCode)
            {
                // Parse the response body.
                // return resp.Content.ReadAsStringAsync().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                // log.LogInformation("Object: " + dataObjects + "\r\n");
            }
            else
            {
                // return "";
                // log.LogInformation("{0} ({1})", (int)resp.StatusCode, resp.ReasonPhrase);
            }
            */

            string url = "https://api.twitter.com/2/tweets/search/recent?query=%23stocks&tweet.fields=created_at&expansions=author_id&user.fields=created_at&max_results=10";
            string token = "AAAAAAAAAAAAAAAAAAAAAEj5QwEAAAAA3DqQ96NOlS16j1LKFc%2BTify7kzc%3DNiYqA2HFNMLb8Naqs9PHbSgPQZH5g2eS887FkVHTKY0vJwuOgl";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.SendAsync(request, 0);

            string stringResponse = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(stringResponse);
            dynamic dataArray = json["data"];

            string resp="";

            // foreach( TweetData data in tweetsList )
            // {
            //     resp+=data.text +"\r\n";
            // }

            List<Tweet> tweetList = new List<Tweet>();

            foreach (var item in dataArray.Children())
            {
                Tweet tweetObject = new Tweet();
                tweetObject.Body = item["text"];
                if(tags)
                    tweetObject.Tags = GetTags(context, tweetObject.Body);
                else
                    tweetObject.Tags = "";
                tweetList.Add(tweetObject);

                resp+=item["text"] +"\r\n";
            }

            TweetJson tweetJson = new TweetJson();
            TweetStream stream = new TweetStream();
            stream.tweetsList = tweetList;
            tweetJson.stream = stream;

            return JsonConvert.SerializeObject(tweetJson);
        }

        public static string[,] StocksList = {
        {"ACC Ltd.","CEMENT & CEMENT PRODUCTS","ACC"},
{"AU Small Finance Bank Ltd.","FINANCIAL SERVICES","AUBANK"},
{"Aarti Industries Ltd.","CHEMICALS","AARTIIND"},
{"Abbott India Ltd.","PHARMA","ABBOTINDIA"},
{"Adani Enterprises Ltd.","METALS","ADANIENT"},
{"Adani Green Energy Ltd.","POWER","ADANIGREEN"},
{"Adani Ports and Special Economic Zone Ltd.","SERVICES","ADANIPORTS"},
{"Adani Total Gas Ltd.","OIL & GAS","ATGL"},
{"Adani Transmission Ltd.","POWER","ADANITRANS"},
{"Aditya Birla Capital Ltd.","FINANCIAL SERVICES","ABCAPITAL"},
{"Aditya Birla Fashion and Retail Ltd.","CONSUMER SERVICES","ABFRL"},
{"Ajanta Pharmaceuticals Ltd.","PHARMA","AJANTPHARM"},
{"Alembic Pharmaceuticals Ltd.","PHARMA","APLLTD"},
{"Alkem Laboratories Ltd.","PHARMA","ALKEM"},
{"Amara Raja Batteries Ltd.","AUTOMOBILE","AMARAJABAT"},
{"Ambuja Cements Ltd.","CEMENT & CEMENT PRODUCTS","AMBUJACEM"},
{"Apollo Hospitals Enterprise Ltd.","HEALTHCARE SERVICES","APOLLOHOSP"},
{"Apollo Tyres Ltd.","AUTOMOBILE","APOLLOTYRE"},
{"Ashok Leyland Ltd.","AUTOMOBILE","ASHOKLEY"},
{"Asian Paints Ltd.","CONSUMER GOODS","ASIANPAINT"},
{"Aurobindo Pharma Ltd.","PHARMA","AUROPHARMA"},
{"Avenue Supermarts Ltd.","CONSUMER SERVICES","DMART"},
{"Axis Bank Ltd.","FINANCIAL SERVICES","AXISBANK"},
{"Bajaj Auto Ltd.","AUTOMOBILE","BAJAJ-AUTO"},
{"Bajaj Finance Ltd.","FINANCIAL SERVICES","BAJFINANCE"},
{"Bajaj Finserv Ltd.","FINANCIAL SERVICES","BAJAJFINSV"},
{"Bajaj Holdings & Investment Ltd.","FINANCIAL SERVICES","BAJAJHLDNG"},
{"Balkrishna Industries Ltd.","AUTOMOBILE","BALKRISIND"},
{"Bandhan Bank Ltd.","FINANCIAL SERVICES","BANDHANBNK"},
{"Bank of Baroda","FINANCIAL SERVICES","BANKBARODA"},
{"Bank of India","FINANCIAL SERVICES","BANKINDIA"},
{"Bata India Ltd.","CONSUMER GOODS","BATAINDIA"},
{"Berger Paints India Ltd.","CONSUMER GOODS","BERGEPAINT"},
{"Bharat Electronics Ltd.","INDUSTRIAL MANUFACTURING","BEL"},
{"Bharat Forge Ltd.","INDUSTRIAL MANUFACTURING","BHARATFORG"},
{"Bharat Heavy Electricals Ltd.","INDUSTRIAL MANUFACTURING","BHEL"},
{"Bharat Petroleum Corporation Ltd.","OIL & GAS","BPCL"},
{"Bharti Airtel Ltd.","TELECOM","BHARTIARTL"},
{"Biocon Ltd.","PHARMA","BIOCON"},
{"Bombay Burmah Trading Corporation Ltd.","CONSUMER GOODS","BBTC"},
{"Bosch Ltd.","AUTOMOBILE","BOSCHLTD"},
{"Britannia Industries Ltd.","CONSUMER GOODS","BRITANNIA"},
{"CESC Ltd.","POWER","CESC"},
{"Cadila Healthcare Ltd.","PHARMA","CADILAHC"},
{"Canara Bank","FINANCIAL SERVICES","CANBK"},
{"Castrol India Ltd.","OIL & GAS","CASTROLIND"},
{"Cholamandalam Investment and Finance Company Ltd.","FINANCIAL SERVICES","CHOLAFIN"},
{"Cipla Ltd.","PHARMA","CIPLA"},
{"City Union Bank Ltd.","FINANCIAL SERVICES","CUB"},
{"Coal India Ltd.","METALS","COALINDIA"},
{"Coforge Ltd.","IT","COFORGE"},
{"Colgate Palmolive (India) Ltd.","CONSUMER GOODS","COLPAL"},
{"Container Corporation of India Ltd.","SERVICES","CONCOR"},
{"Coromandel International Ltd.","FERTILISERS & PESTICIDES","COROMANDEL"},
{"Crompton Greaves Consumer Electricals Ltd.","CONSUMER GOODS","CROMPTON"},
{"Cummins India Ltd.","INDUSTRIAL MANUFACTURING","CUMMINSIND"},
{"DLF Ltd.","CONSTRUCTION","DLF"},
{"Dabur India Ltd.","CONSUMER GOODS","DABUR"},
{"Dalmia Bharat Ltd.","CEMENT & CEMENT PRODUCTS","DALBHARAT"},
{"Deepak Nitrite Ltd.","CHEMICALS","DEEPAKNTR"},
{"Dhani Services Ltd.","FINANCIAL SERVICES","DHANI"},
{"Divi's Laboratories Ltd.","PHARMA","DIVISLAB"},
{"Dixon Technologies (India) Ltd.","CONSUMER GOODS","DIXON"},
{"Dr. Lal Path Labs Ltd.","HEALTHCARE SERVICES","LALPATHLAB"},
{"Dr. Reddy's Laboratories Ltd.","PHARMA","DRREDDY"},
{"Eicher Motors Ltd.","AUTOMOBILE","EICHERMOT"},
{"Emami Ltd.","CONSUMER GOODS","EMAMILTD"},
{"Endurance Technologies Ltd.","AUTOMOBILE","ENDURANCE"},
{"Escorts Ltd.","AUTOMOBILE","ESCORTS"},
{"Exide Industries Ltd.","AUTOMOBILE","EXIDEIND"},
{"Federal Bank Ltd.","FINANCIAL SERVICES","FEDERALBNK"},
{"Fortis Healthcare Ltd.","HEALTHCARE SERVICES","FORTIS"},
{"GAIL (India) Ltd.","OIL & GAS","GAIL"},
{"GMR Infrastructure Ltd.","CONSTRUCTION","GMRINFRA"},
{"Gland Pharma Ltd.","PHARMA","GLAND"},
{"Glenmark Pharmaceuticals Ltd.","PHARMA","GLENMARK"},
{"Godrej Agrovet Ltd.","CONSUMER GOODS","GODREJAGRO"},
{"Godrej Consumer Products Ltd.","CONSUMER GOODS","GODREJCP"},
{"Godrej Industries Ltd.","CONSUMER GOODS","GODREJIND"},
{"Godrej Properties Ltd.","CONSTRUCTION","GODREJPROP"},
{"Grasim Industries Ltd.","CEMENT & CEMENT PRODUCTS","GRASIM"},
{"Gujarat Gas Ltd.","OIL & GAS","GUJGASLTD"},
{"Gujarat State Petronet Ltd.","OIL & GAS","GSPL"},
{"HCL Technologies Ltd.","IT","HCLTECH"},
{"HDFC Asset Management Company Ltd.","FINANCIAL SERVICES","HDFCAMC"},
{"HDFC Bank Ltd.","FINANCIAL SERVICES","HDFCBANK"},
{"HDFC Life Insurance Company Ltd.","FINANCIAL SERVICES","HDFCLIFE"},
{"Havells India Ltd.","CONSUMER GOODS","HAVELLS"},
{"Hero MotoCorp Ltd.","AUTOMOBILE","HEROMOTOCO"},
{"Hindalco Industries Ltd.","METALS","HINDALCO"},
{"Hindustan Aeronautics Ltd.","INDUSTRIAL MANUFACTURING","HAL"},
{"Hindustan Petroleum Corporation Ltd.","OIL & GAS","HINDPETRO"},
{"Hindustan Unilever Ltd.","CONSUMER GOODS","HINDUNILVR"},
{"Hindustan Zinc Ltd.","METALS","HINDZINC"},
{"Housing Development Finance Corporation Ltd.","FINANCIAL SERVICES","HDFC"},
{"ICICI Bank Ltd.","FINANCIAL SERVICES","ICICIBANK"},
{"ICICI Lombard General Insurance Company Ltd.","FINANCIAL SERVICES","ICICIGI"},
{"ICICI Prudential Life Insurance Company Ltd.","FINANCIAL SERVICES","ICICIPRULI"},
{"ICICI Securities Ltd.","FINANCIAL SERVICES","ISEC"},
{"IDFC First Bank Ltd.","FINANCIAL SERVICES","IDFCFIRSTB"},
{"ITC Ltd.","CONSUMER GOODS","ITC"},
{"Indiabulls Housing Finance Ltd.","FINANCIAL SERVICES","IBULHSGFIN"},
{"Indiamart Intermesh Ltd.","CONSUMER SERVICES","INDIAMART"},
{"Indian Hotels Co. Ltd.","CONSUMER SERVICES","INDHOTEL"},
{"Indian Oil Corporation Ltd.","OIL & GAS","IOC"},
{"Indian Railway Catering And Tourism Corporation Ltd.","SERVICES","IRCTC"},
{"Indraprastha Gas Ltd.","OIL & GAS","IGL"},
{"Indus Towers Ltd.","TELECOM","INDUSTOWER"},
{"IndusInd Bank Ltd.","FINANCIAL SERVICES","INDUSINDBK"},
{"Info Edge (India) Ltd.","CONSUMER SERVICES","NAUKRI"},
{"Infosys Ltd.","IT","INFY"},
{"InterGlobe Aviation Ltd.","SERVICES","INDIGO"},
{"Ipca Laboratories Ltd.","PHARMA","IPCALAB"},
{"JSW Energy Ltd.","POWER","JSWENERGY"},
{"JSW Steel Ltd.","METALS","JSWSTEEL"},
{"Jindal Steel & Power Ltd.","METALS","JINDALSTEL"},
{"Jubilant Foodworks Ltd.","CONSUMER SERVICES","JUBLFOOD"},
{"Kotak Mahindra Bank Ltd.","FINANCIAL SERVICES","KOTAKBANK"},
{"L&T Finance Holdings Ltd.","FINANCIAL SERVICES","L&TFH"},
{"L&T Technology Services Ltd.","IT","LTTS"},
{"LIC Housing Finance Ltd.","FINANCIAL SERVICES","LICHSGFIN"},
{"Larsen & Toubro Infotech Ltd.","IT","LTI"},
{"Larsen & Toubro Ltd.","CONSTRUCTION","LT"},
{"Laurus Labs Ltd.","PHARMA","LAURUSLABS"},
{"Lupin Ltd.","PHARMA","LUPIN"},
{"MRF Ltd.","AUTOMOBILE","MRF"},
{"Mahanagar Gas Ltd.","OIL & GAS","MGL"},
{"Mahindra & Mahindra Financial Services Ltd.","FINANCIAL SERVICES","M&MFIN"},
{"Mahindra & Mahindra Ltd.","AUTOMOBILE","M&M"},
{"Manappuram Finance Ltd.","FINANCIAL SERVICES","MANAPPURAM"},
{"Marico Ltd.","CONSUMER GOODS","MARICO"},
{"Maruti Suzuki India Ltd.","AUTOMOBILE","MARUTI"},
{"Max Financial Services Ltd.","FINANCIAL SERVICES","MFSL"},
{"MindTree Ltd.","IT","MINDTREE"},
{"MphasiS Ltd.","IT","MPHASIS"},
{"Muthoot Finance Ltd.","FINANCIAL SERVICES","MUTHOOTFIN"},
{"NATCO Pharma Ltd.","PHARMA","NATCOPHARM"},
{"NMDC Ltd.","METALS","NMDC"},
{"NTPC Ltd.","POWER","NTPC"},
{"Navin Fluorine International Ltd.","CHEMICALS","NAVINFLUOR"},
{"Nestle India Ltd.","CONSUMER GOODS","NESTLEIND"},
{"Nippon Life India Asset Management Ltd.","FINANCIAL SERVICES","NAM-INDIA"},
{"Oberoi Realty Ltd.","CONSTRUCTION","OBEROIRLTY"},
{"Oil & Natural Gas Corporation Ltd.","OIL & GAS","ONGC"},
{"Oil India Ltd.","OIL & GAS","OIL"},
{"PI Industries Ltd.","FERTILISERS & PESTICIDES","PIIND"},
{"Page Industries Ltd.","TEXTILES","PAGEIND"},
{"Petronet LNG Ltd.","OIL & GAS","PETRONET"},
{"Pfizer Ltd.","PHARMA","PFIZER"},
{"Pidilite Industries Ltd.","CHEMICALS","PIDILITIND"},
{"Piramal Enterprises Ltd.","FINANCIAL SERVICES","PEL"},
{"Polycab India Ltd.","INDUSTRIAL MANUFACTURING","POLYCAB"},
{"Power Finance Corporation Ltd.","FINANCIAL SERVICES","PFC"},
{"Power Grid Corporation of India Ltd.","POWER","POWERGRID"},
{"Prestige Estates Projects Ltd.","CONSTRUCTION","PRESTIGE"},
{"Procter & Gamble Hygiene & Health Care Ltd.","CONSUMER GOODS","PGHH"},
{"Punjab National Bank","FINANCIAL SERVICES","PNB"},
{"RBL Bank Ltd.","FINANCIAL SERVICES","RBLBANK"},
{"REC Ltd.","FINANCIAL SERVICES","RECLTD"},
{"Reliance Industries Ltd.","OIL & GAS","RELIANCE"},
{"SBI Cards and Payment Services Ltd.","FINANCIAL SERVICES","SBICARD"},
{"SBI Life Insurance Company Ltd.","FINANCIAL SERVICES","SBILIFE"},
{"SRF Ltd.","CHEMICALS","SRF"},
{"Sanofi India Ltd.","PHARMA","SANOFI"},
{"Shree Cement Ltd.","CEMENT & CEMENT PRODUCTS","SHREECEM"},
{"Shriram Transport Finance Co. Ltd.","FINANCIAL SERVICES","SRTRANSFIN"},
{"Siemens Ltd.","INDUSTRIAL MANUFACTURING","SIEMENS"},
{"State Bank of India","FINANCIAL SERVICES","SBIN"},
{"Steel Authority of India Ltd.","METALS","SAIL"},
{"Sun Pharmaceutical Industries Ltd.","PHARMA","SUNPHARMA"},
{"Sun TV Network Ltd.","MEDIA ENTERTAINMENT & PUBLICATION","SUNTV"},
{"Syngene International Ltd.","HEALTHCARE SERVICES","SYNGENE"},
{"TVS Motor Company Ltd.","AUTOMOBILE","TVSMOTOR"},
{"Tata Chemicals Ltd.","CHEMICALS","TATACHEM"},
{"Tata Consultancy Services Ltd.","IT","TCS"},
{"Tata Consumer Products Ltd.","CONSUMER GOODS","TATACONSUM"},
{"Tata Elxsi Ltd.","IT","TATAELXSI"},
{"Tata Motors Ltd.","AUTOMOBILE","TATAMOTORS"},
{"Tata Power Co. Ltd.","POWER","TATAPOWER"},
{"Tata Steel Ltd.","METALS","TATASTEEL"},
{"Tech Mahindra Ltd.","IT","TECHM"},
{"The Ramco Cements Ltd.","CEMENT & CEMENT PRODUCTS","RAMCOCEM"},
{"Titan Company Ltd.","CONSUMER GOODS","TITAN"},
{"Torrent Pharmaceuticals Ltd.","PHARMA","TORNTPHARM"},
{"Torrent Power Ltd.","POWER","TORNTPOWER"},
{"Trent Ltd.","CONSUMER SERVICES","TRENT"},
{"UPL Ltd.","FERTILISERS & PESTICIDES","UPL"},
{"UltraTech Cement Ltd.","CEMENT & CEMENT PRODUCTS","ULTRACEMCO"},
{"Union Bank of India","FINANCIAL SERVICES","UNIONBANK"},
{"United Breweries Ltd.","CONSUMER GOODS","UBL"},
{"United Spirits Ltd.","CONSUMER GOODS","MCDOWELL-N"},
{"V-Guard Industries Ltd.","CONSUMER GOODS","VGUARD"},
{"Varun Beverages Ltd.","CONSUMER GOODS","VBL"},
{"Vedanta Ltd.","METALS","VEDL"},
{"Vodafone Idea Ltd.","TELECOM","IDEA"},
{"Voltas Ltd.","CONSUMER GOODS","VOLTAS"},
{"Whirlpool of India Ltd.","CONSUMER GOODS","WHIRLPOOL"},
{"Wipro Ltd.","IT","WIPRO"},
{"Yes Bank Ltd.","FINANCIAL SERVICES","YESBANK"},
{"Zee Entertainment Enterprises Ltd.","MEDIA ENTERTAINMENT & PUBLICATION","ZEEL"}
        };

        public static string GetTags(ExecutionContext context, string text)
        {
            // var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            List<string> tagsList = new List<string>();

            // using(var sReader = new StreamReader(System.IO.Path.Combine(context.FunctionDirectory, "..\\..\\..\\..\\ind_nifty200list.csv")))
            // {
            //     while (!sReader.EndOfStream)
            //     {
            //         var line = sReader.ReadLine();
            //         var values = line.Split(',');

            //         if(text.Contains(values[0]) || text.Contains(values[2]))
            //         {
            //             if(text.Contains(values[0]))
            //                 tagsList.Add(values[0]);

            //             if(text.Contains(values[2]))
            //                 tagsList.Add(values[2]);

            //             tagsList.Add(values[1].ToLower());

            //             break;
            //         }
            //     }
            // }       

            for(int i=0;i<StocksList.GetLength(0);i++)
            {
                if(text.Contains(" "+StocksList[i,0]) || text.Contains(" "+StocksList[i,2]))
                {
                    if(text.Contains(" "+StocksList[i,0]))
                        tagsList.Add(StocksList[i,0]);

                    if(text.Contains(" "+StocksList[i,2]))
                        tagsList.Add(StocksList[i,2]);

                    tagsList.Add(StocksList[i,1].ToLower());
                }
            }    

            return string.Join(",",tagsList.ToArray());
        }
    }
}
