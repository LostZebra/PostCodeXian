using PostCodeXian.Common;
using PostCodeXian.Data;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
// For debugging

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace PostCodeXian
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public sealed partial class DistrictItem
    {
        private readonly NavigationHelper _navigationHelper;
        private readonly ObservableDictionary _defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView(@"Resources");

        private const string DistrictDataName = "DistrictData";
        
        // District presented in current page
        private District _currentDistrict;

        public DistrictItem()
        {
            this.InitializeComponent();

            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this._navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this._defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data.
            _currentDistrict = (District)e.NavigationParameter;
            this.DefaultViewModel[DistrictDataName] = _currentDistrict;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string postCode = (sender as Button).Content.ToString();
            List<PostCodeItem> postCodeItemList = DistrictDataSource.GetInstance().PostCodeLibrary[_currentDistrict];
            int selectedIndex = postCodeItemList.FindIndex(item => item.PostCode == postCode);
            PostCodeItem postCodeItem = postCodeItemList[selectedIndex];
            if (!Frame.Navigate(typeof(PcItem), postCodeItem))
            {
                throw new Exception(this._resourceLoader.GetString(@"NavigationFailedExceptionMessage"));
            }
        }
    }
}