using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace PostCodeXian.Data
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
        public static DistrictDataSource dataSource;
        public List<District> DistrictList { get; private set; }
        public Dictionary<District, List<PostCodeItem>> PostCodeLibrary { get; private set; }

        public DistrictDataSource()
        {
            this.DistrictList = new List<District>();
            this.PostCodeLibrary = new Dictionary<District, List<PostCodeItem>>();
        }

        public static DistrictDataSource GetInstance()
        {
            if (dataSource == null)
            {
                dataSource = new DistrictDataSource();
            }
            return dataSource;
        }

        public async Task GetDistrictData()
        {
            if (this.DistrictList.Count != 0)
            {
                return;
            }
            Uri districtDataUri = new Uri("ms-appx:///DataModel/DistrictData.json");
            StorageFile districtDataFile = await StorageFile.GetFileFromApplicationUriAsync(districtDataUri);
            string districtJsonText = await FileIO.ReadTextAsync(districtDataFile);
            JsonObject districtJsonObject = JsonObject.Parse(districtJsonText);
            // New DistrictData object
            foreach (var districtName in districtJsonObject.Keys)
            {
                JsonObject postCodeJsonObj = districtJsonObject[districtName].GetObject();
                JsonObject postCodeJsonArray = postCodeJsonObj["PostCodes"].GetObject();
                // Add all post codes corresponding to certain district
                List<string> postCodeList = new List<string>(postCodeJsonArray.Keys);  
                District district = new District(districtName, postCodeList);
                this.DistrictList.Add(district); 
                // List for all PostCodeItems
                List<PostCodeItem> postCodeItemList = new List<PostCodeItem>();
                foreach (var postCode in postCodeJsonArray.Keys)
                {           
                    JsonObject streetJsonArray = postCodeJsonArray[postCode].GetObject();
                    // Adding all streets corresponding to certain post code
                    List<string> streetList = new List<string>();  
                    foreach (var streetIndex in streetJsonArray.Keys)
                    {
                        streetList.Add(streetJsonArray[streetIndex].GetString());
                    }
                    postCodeItemList.Add(new PostCodeItem(postCode, streetList));
                }
                this.PostCodeLibrary.Add(district, postCodeItemList);
            }          
        }
    }
}
