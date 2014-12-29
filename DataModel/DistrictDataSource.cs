using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
// For debugging
using System.Diagnostics;

namespace PostCodeXian.DataModel
{
    class District
    {
        public string DistrictName { get; private set; }
        public List<string> PostCodeList { get; private set; }

        public District(string districtName, List<string> postCodeList)
        {
            this.DistrictName = districtName;
            this.PostCodeList = postCodeList;
        }
    }
    
    class PostCodeItem
    {
        public string PostCode { get; private set; }
        public List<string> StreetList { get; private set; }

        public PostCodeItem(string postCode, List<string> streetList)
        {
            this.PostCode = postCode;
            this.StreetList = streetList;
        }
    }

    class DistrictDataSource
    {
        public static DistrictDataSource DataSource;
        public List<District> DistrictList { get; private set; }
        public Dictionary<District, List<PostCodeItem>> PostCodeLibrary { get; private set; }

        public DistrictDataSource()
        {
            this.DistrictList = new List<District>();
            this.PostCodeLibrary = new Dictionary<District, List<PostCodeItem>>();
        }

        public static DistrictDataSource GetInstance()
        {
            return DataSource ?? (DataSource = new DistrictDataSource());
        }

        public async Task GetDistrictData()
        {
            if (this.DistrictList.Count != 0)
            {
                return;
            }
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            // var file = await folder.GetFileAsync("DistrictDataBackup.json");
            var file = await folder.GetFileAsync("DistrictData.json");
            string districtJsonText = await FileIO.ReadTextAsync(file);
            JsonObject districtJsonObject = JsonObject.Parse(districtJsonText);
            // New DistrictData object
            foreach (var districtName in districtJsonObject.Keys)
            {
                JsonObject postcodeAddressJsonObj = districtJsonObject[districtName].GetObject();
                List<string> postcodeList = new List<string>(postcodeAddressJsonObj.Keys);
                District district = new District(districtName, postcodeList);
                this.DistrictList.Add(district); 
                // List for all Postcode items
                List<PostCodeItem> postcodeItemList = (from postcode in postcodeList let addressListJsonArray = postcodeAddressJsonObj[postcode].GetArray() let addressList = addressListJsonArray.Select(address => address.GetString()).ToList() select new PostCodeItem(postcode, addressList)).ToList();
                this.PostCodeLibrary.Add(district, postcodeItemList);
            }          
        }

        public string QueryPostCode(string districtName, string streetName)
        {
            var matchedPostcodeList = this.PostCodeLibrary.Where(districtItem => districtItem.Key.DistrictName == districtName)
                .Select(districtItem => districtItem.Value).First();
            var matchedAddressList = from postcodeItem in matchedPostcodeList
                                 from address in postcodeItem.StreetList
                                 where address.Contains(streetName)
                                 select new {Postcode = postcodeItem.PostCode, Address = address};
            return matchedAddressList.Select(matchedAddress => matchedAddress.Postcode).FirstOrDefault();
        }
    }
}
