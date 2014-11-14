using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Json;
using PostCodeXian.Common;

namespace PostCodeXian.DataModel
{
    public class SearchedResultItem
    {
        public string Address { get; private set; }

        public string Lat { get; private set; }

        public string Lng { get; private set; }

        public SearchedResultItem(string address, string lat, string lng)
        {
            this.Lat = lat;
            this.Lng = lng;
            this.Address = address;
        }
    }

    public class QueryUnitItem
    {
        public string DistrictName { get; private set; }

        public string StreetName { get; private set; }

        public QueryUnitItem(string districtName, string streetName)
        {
            this.DistrictName = districtName;
            this.StreetName = streetName;
        }
    }

    public sealed class SearchedResults
    {
        private static SearchedResults _searchedResults;
        private readonly List<SearchedResultItem> _searcheResultsList;

        public SearchedResults()
        {
            this._searcheResultsList = new List<SearchedResultItem>();
        }

        public List<SearchedResultItem> SearchedResultsList
        {
            get { return this._searcheResultsList; }
        }

        public static SearchedResults GetInstance()
        {
            if (_searchedResults == null)
            {
                _searchedResults = new SearchedResults();
            }
            return _searchedResults;
        }

        public Task<SearchedResults> GetSearchedResults(string keyword)
        {
            return FetchAddressData(keyword);
        }

        private async Task<SearchedResults> FetchAddressData(string keyword)
        {
            this._searcheResultsList.Clear();
            MapClient gMapClient = MapClient.GetInstance();
            var resultsList = await gMapClient.AutoCompleteResults(keyword);
            if (resultsList == null || resultsList.Count == 0) return this;
            int count = 0; // Only ask for 5 items
            foreach(var resultItem in resultsList)
            {
                if (count == 5)
                {
                    break;
                }
                JsonObject resultItemObj = resultItem.GetObject();
                
                this._searcheResultsList.Add(new SearchedResultItem(resultItemObj["name"].GetString(), 
                                             resultItemObj["location"].GetObject()["lat"].GetNumber().ToString(),
                                             resultItemObj["location"].GetObject()["lng"].GetNumber().ToString()));
                count++;
            }
            return this;
        }
    }
}
