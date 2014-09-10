// Self defined
using PostCodeXian.Common;
// System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Net & Data
using Windows.Data.Json;
using System.Net.Http;
// Regular expression
using System.Text.RegularExpressions;
// For debugging
using System.Diagnostics;

namespace PostCodeXian.Data
{
    public class SearchedResultItem
    {
        public String Address { get; private set; }

        public SearchedResultItem(string address)
        {
            this.Address = address;
        }
    }

    public sealed class SearchedResults
    {
        private static SearchedResults searchedResults;
        private List<SearchedResultItem> _searcheResultsList;

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
            if (searchedResults == null)
            {
                searchedResults = new SearchedResults();
            }
            return searchedResults;
        }

        public Task<SearchedResults> GetSearchedResults(string keyword)
        {
            return FetchAddressData(keyword);
        }

        private async Task<SearchedResults> FetchAddressData(string keyword)
        {
            MapClient gMapClient = MapClient.getInstance();
            var resultsList = await gMapClient.AutoCompleteResults(keyword);
            if (resultsList != null)
            {
                this._searcheResultsList.Clear();
                int count = 0; // Only ask for 5 items
                foreach(JsonValue resultItem in resultsList)
                {
                    if (count == 5)
                    {
                        break;
                    }
                    JsonObject resultItemObj = resultItem.GetObject();
                    this._searcheResultsList.Add(new SearchedResultItem(resultItemObj["name"].GetString()));
                    count++;
                }
            }
            return this;
        }
    }
}
