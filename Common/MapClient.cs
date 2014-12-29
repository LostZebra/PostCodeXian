// System
using System;
using System.Threading.Tasks;
// NET & Data
using Windows.Data.Json;
using System.Xml.Linq;
using System.Net.Http;
// For debugging
using System.Diagnostics;
// For device location
using Windows.Devices.Geolocation;
// Self defined
using PostCodeXian.DataModel;

namespace PostCodeXian.Common
{
    class MapClient
    {
        private static MapClient _mapClient;

        public static MapClient GetInstance()
        {
            return _mapClient ?? (_mapClient = new MapClient());
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
                return resultJsonObj["status"].GetString().Equals("OK") ? resultJsonObj["results"].GetArray() : null;
            }
        }

        public async Task<string> QueryPostCodeResult(string districtName, string streetName)
        {
            if (streetName == null)
            {
                return null;
            }
            // 首先调用第三方服务获得邮政编码
            Uri queryPostCodeUri = new Uri("http://webservice.webxml.com.cn/WebServices/ChinaZipSearchWebService.asmx/getZipCodeByAddress?theProvinceName=陕西&theCityName=西安&theAddress=" + streetName + "&userId=");
            using (HttpClient queryPostCodeClient = new HttpClient())
            {
                HttpResponseMessage queryPostCodeResponse = await queryPostCodeClient.GetAsync(queryPostCodeUri);
                queryPostCodeResponse.EnsureSuccessStatusCode();
                string resultStr = await queryPostCodeResponse.Content.ReadAsStringAsync();
                XDocument xmlDocument = XDocument.Parse(resultStr);
                string fullAddressWithPostCode = xmlDocument.Root.Value;
                
                return fullAddressWithPostCode.Contains("没有发现") ? DistrictDataSource.GetInstance().QueryPostCode(districtName, streetName) : fullAddressWithPostCode.Substring(fullAddressWithPostCode.Length - 6);
            }
        }

        public async Task<QueryUnitItem> GetCurrentStreet()
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
                
                // If data fetching returns error
                if (!resultJsonObj["status"].GetString().Equals("OK")) return null;
                
                JsonObject addressComponentJsonObj = resultJsonObj["result"].GetObject()["addressComponent"].GetObject();

                // Testing
                Debug.WriteLine(addressComponentJsonObj["city"].GetString());

                // If current location is not within the range of 西安市
                if (!addressComponentJsonObj["city"].GetString().Equals("西安市")) return null;

                string street = addressComponentJsonObj["street"].GetString();
                string district = addressComponentJsonObj["district"].GetString();

                return new QueryUnitItem(district, street);
            }
        }

        public async Task<QueryUnitItem> GetQueryUnitItem(SearchedResultItem selectedResultItem)
        {
            // 调用百度的api确定街道名称
            Uri queryStreetNameUri = new Uri("http://api.map.baidu.com/geocoder?location=" + selectedResultItem.Lat + "," + selectedResultItem.Lng + "&output=json&src=西安邮政编码查询");
            using (HttpClient queryStreetNameClient = new HttpClient())
            {
                HttpResponseMessage queryStreetNameResponse = await queryStreetNameClient.GetAsync(queryStreetNameUri);
                queryStreetNameResponse.EnsureSuccessStatusCode();
                string resultStr = await queryStreetNameResponse.Content.ReadAsStringAsync();
                JsonObject resultJsonObj = JsonObject.Parse(resultStr);

                // If data fetching returns error
                if (!resultJsonObj["status"].GetString().Equals("OK")) return null;

                JsonObject addressComponentJsonObj = resultJsonObj["result"].GetObject()["addressComponent"].GetObject();

                // If current location is not within the range of 西安市
                if (!addressComponentJsonObj["city"].GetString().Equals("西安市")) return null;

                string street = addressComponentJsonObj["street"].GetString();
                string district = addressComponentJsonObj["district"].GetString();

                return new QueryUnitItem(district, street);
            }
        }
    }
}
