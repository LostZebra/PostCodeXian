// System
using System;
using System.Threading.Tasks;
// NET & Data
using Windows.Data.Json;
using System.Xml.Linq;
using System.Net.Http;
// For debugging
// using System.Diagnostics;
// For device location
using Windows.Devices.Geolocation;

namespace PostCodeXian.Common
{
    class MapClient
    {
        public static MapClient mapClient;

        public static MapClient getInstance()
        {
            if (mapClient == null)
            {
                mapClient = new MapClient();
            }
            return mapClient;
        }

        public async Task<JsonArray> AutoCompleteResults(string keyword)
        {
            Uri searchStreetUri = new Uri("http://api.map.baidu.com/place/search?query=" + keyword + "&region=西安&output=json&src=西安邮政编码查询");
            using (HttpClient searchStreetClient = new HttpClient())
            {
                HttpResponseMessage searchStreetResponse = await searchStreetClient.GetAsync(searchStreetUri);
                searchStreetResponse.EnsureSuccessStatusCode();
                string resultStr = await searchStreetResponse.Content.ReadAsStringAsync();
                JsonObject resultJsonObj = JsonObject.Parse(resultStr);
                if(resultJsonObj["status"].GetString().Equals("OK"))
                {
                    return resultJsonObj["results"].GetArray();
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<string> QueryPostCodeResult(string streetName)
        {
            if (streetName.Length == 0)
            {
                return null;
            }
            Uri queryPostCodeUri = new Uri("http://webservice.webxml.com.cn/WebServices/ChinaZipSearchWebService.asmx/getZipCodeByAddress?theProvinceName=陕西&theCityName=西安&theAddress=" + streetName + "&userId=");
            using (HttpClient queryPostCodeClient = new HttpClient())
            {
                HttpResponseMessage queryPostCodeResponse = await queryPostCodeClient.GetAsync(queryPostCodeUri);
                queryPostCodeResponse.EnsureSuccessStatusCode();
                string resultStr = await queryPostCodeResponse.Content.ReadAsStringAsync();
                XDocument xmlDocument = XDocument.Parse(resultStr);
                string fullAddressWithPostCode = xmlDocument.Root.Value;
                if (fullAddressWithPostCode.Contains("没有发现"))
                {
                    return null;
                }
                string postCode = fullAddressWithPostCode.Substring(fullAddressWithPostCode.Length - 6);
                return postCode;
            }
        }

        public async Task<string> GetCurrentStreet()
        {
            Geolocator locateMe = new Geolocator();
            Geoposition myPostion = await locateMe.GetGeopositionAsync();
            string latitude = myPostion.Coordinate.Point.Position.Latitude.ToString();
            string longitude = Math.Abs(myPostion.Coordinate.Point.Position.Longitude).ToString();
            // 调用百度的api确定街道名称
            Uri queryStreetNameUri = new Uri("http://api.map.baidu.com/geocoder?location=" + latitude + "," + longitude + "&output=json&src=西安邮政编码查询");
            using (HttpClient queryStreetNameClient = new HttpClient())
            {
                HttpResponseMessage queryStreetNameResponse = await queryStreetNameClient.GetAsync(queryStreetNameUri);
                queryStreetNameResponse.EnsureSuccessStatusCode();
                string resultStr = await queryStreetNameResponse.Content.ReadAsStringAsync();
                JsonObject resultJsonObj = JsonObject.Parse(resultStr);
                if (resultJsonObj["status"].GetString().Equals("OK"))
                {
                    JsonObject valueObj = resultJsonObj["result"].GetObject();
                    string streetName = (valueObj["addressComponent"].GetObject() as JsonObject)["street"].GetString();
                    return streetName;
                }
            }
            return "公园北路";
        }
    }
}

