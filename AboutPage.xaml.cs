using PostCodeXian.Common;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
// For debugging
using System.Diagnostics;
// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace PostCodeXian
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        private readonly NavigationHelper _navigationHelper;
        private readonly ObservableDictionary _defaultViewModel = new ObservableDictionary();

        public string AppTitle { get; private set; }
        public string DetailInfo { get; private set; }

        public AboutPage()
        {
            this.InitializeComponent();

            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += this.NavigationHelper_LoadState;
            _navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return _navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return _defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // About message
            string[] aboutInfo = CommonTaskClient.AboutInfo();
            this.DefaultViewModel["AppTitle"] = aboutInfo[0];
            this.DefaultViewModel["DetailInfo"] = aboutInfo[1]; 
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
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
            _navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
           _navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void FeedBackButton_Click(object sender, RoutedEventArgs e)
        {
            CommonTaskClient.SendFeedBack();
        }

        private void weixinIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            /*
            this.weiboProgressRing.IsActive = true;
            
            string appId = WXSDKData.AppId;
            WXBaseMessage wxMessage = null;
            int scene = SendMessageToWX.Req.WXSceneTimeline;

            WXTextMessage msg = new WXTextMessage();
            msg.Title = "文本";
            msg.ThumbData = null;
            msg.Text = "这是一段文本内容";
            wxMessage = msg;
            try
            {
                SendMessageToWX.Req req = new SendMessageToWX.Req(wxMessage, scene);
                IWXAPI api = WXAPIFactory.CreateWXAPI(appId);
                api.SendReq(req);
            }
            catch (WXException ex)
            {
                Debug.WriteLine(ex.Message);
            }

            this.weiboProgressRing.IsActive = false;
            */
        }
    }
}