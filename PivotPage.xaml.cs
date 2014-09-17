using PostCodeXian.Common;
using PostCodeXian.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Documents;
// For debugging
using System.Diagnostics;
// Net & Data
using Windows.Data.Json;
using System.Net;
using System.Net.Http;
using Windows.Storage;
// Xml parser
using System.Xml.Linq;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace PostCodeXian
{
    delegate void ChangeProgress(int i);
    public sealed partial class PivotPage : Page
    {
        private static Dictionary<string, object> state = new Dictionary<string, object>();
        private const string DistrictDataSetName = "DistrictDataSet";
        private const string ProgressBarWidthName = "ProgressBarWidth";
        private const string SearchedResultsName = "SearchedResultsGroup";
        private const Visibility visible = Visibility.Visible;
        private const Visibility collapsed = Visibility.Collapsed;
        private const int firstPivotItem = 1;
        private const int secondPivotItem = 2;

        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        private bool isDownloading = false;
        private bool isUpdateChecked = false;

        // Black and white brush
        private SolidColorBrush whiteBrush = new SolidColorBrush(Colors.White);
        private SolidColorBrush blackBrush = new SolidColorBrush(Colors.Black);

        private bool isPinningFinished = false;

        public PivotPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            this.searchedResultsListView.ItemClick += this.searchedResultsListView_ItemClick;
            this.searchedResultsListView.ItemClick += this.searchBox_LostFocus;
            this.searchBox.KeyDown += this.enterKeyDown_handler;

            // Modify download title layout
            double width = Window.Current.Bounds.Width;
            this.defaultViewModel[ProgressBarWidthName] = width - 50;

            // Behaviours when selected pivot item changes
            this.pivot.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e)
            {
                if ((sender as Pivot).SelectedIndex == 1)
                {
                    if (!isPinningFinished)
                    {
                        SecondPivot_Loaded();
                    }
                }
            };

            Application.Current.Suspending += Current_Suspending; 
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
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
        /// session. The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values["Launched"] == null)
            {
                ApplicationData.Current.LocalSettings.Values["Launched"] = true;
                isUpdateChecked = true;
                isDownloading = true;
                showUpdateStatus("正在准备数据");
                ChangeProgress updateProgress = new ChangeProgress(changeProgress);
                await CommonTaskClient.CheckFile();
                await CommonTaskClient.Download(updateProgress);
            }
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            DistrictDataSource dataSource = DistrictDataSource.GetInstance();
            await dataSource.GetDistrictData();
            this.defaultViewModel[DistrictDataSetName] = dataSource;
            
            // Async check update
            isUpdateChecked = state.ContainsKey("IsUpdateChecked") == false ? isUpdateChecked : (bool)state["IsUpdateChecked"];
            if (!isUpdateChecked)
            {
                isUpdateChecked = true;
                bool isCheckUpdateSucceed = true;

                showUpdateStatus("正在检查更新...");              
                try
                {
                    bool isUpdateAvailable = await CommonTaskClient.IsUpdateAvailable();
                    await Task.Delay(500);
                    if (isUpdateAvailable)
                    {
                        showUpdateStatus("邮编数据库有新版本");  
                        MessageDialog updateAvalible = new MessageDialog("有可用的邮编数据库更新，要下载吗？", "提示");
                        updateAvalible.Commands.Add(new UICommand("开始下载", new UICommandInvokedHandler(updateDialogCommanHander), 0));
                        updateAvalible.Commands.Add(new UICommand("以后再说", null, 1));
                        updateAvalible.DefaultCommandIndex = 0;
                        updateAvalible.CancelCommandIndex = 1;
                        await updateAvalible.ShowAsync();
                    }
                    else
                    {
                        showUpdateStatus("无可用更新");
                    }
                }
                catch (HttpRequestException)
                {
                    isCheckUpdateSucceed = false;
                }
                catch (WebException)
                {
                    isCheckUpdateSucceed = false;
                }
                if (!isCheckUpdateSucceed)
                {
                    showUpdateStatus("获取更新失败");
                }
                await Task.Delay(500);
                this.defaultViewModel["UpdateStatus"] = "";
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
            state["IsUpdateChecked"] = isUpdateChecked;
        }

        /// <summary>
        /// Invoked when an item within a section is clicked.
        /// </summary>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            District district = (District)e.ClickedItem;
            if (!Frame.Navigate(typeof(DistrictItem), district))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        /// <summary>
        /// Loads the content for the second pivot item when it is scrolled into view.
        /// </summary>
        private async void SecondPivot_Loaded()
        {
            if (ApplicationData.Current.LocalSettings.Values["AccessGeo"] == null)
            {
                MessageDialog accessingGeoLocator = new MessageDialog("请求访问地理信息数据", "提示");
                accessingGeoLocator.Commands.Add(new UICommand("允许", new UICommandInvokedHandler(geoDialogCommandHandler), 0));
                accessingGeoLocator.Commands.Add(new UICommand("不允许", new UICommandInvokedHandler(geoDialogCommandHandler), 0));
                accessingGeoLocator.DefaultCommandIndex = 0;
                accessingGeoLocator.CancelCommandIndex = 1;
                await accessingGeoLocator.ShowAsync();
                
            }
            else if (ApplicationData.Current.LocalSettings.Values["AccessGeo"].Equals(false))
            {
                MessageDialog refuseAccessGeoLocator = new MessageDialog("访问地理位置信息被拒绝", "提示");
                refuseAccessGeoLocator.Commands.Add(new UICommand("放弃", null, 0));
                refuseAccessGeoLocator.Commands.Add(new UICommand("给予权限", new UICommandInvokedHandler(geoDialogCommandHandler), 1));
                refuseAccessGeoLocator.DefaultCommandIndex = 0;
                refuseAccessGeoLocator.CancelCommandIndex = 1;
                await refuseAccessGeoLocator.ShowAsync();
            }
            else
            {
                string street = await gettingStreet();
                if (street != null)
                {
                    await gettingPostCode(firstPivotItem, street);
                }
            }
        }

        // Handle MessageDialog events
        private async void geoDialogCommandHandler(IUICommand commandResult)
        {
            if (commandResult.Label.Equals("允许") || commandResult.Label.Equals("给予权限"))
            {
                ApplicationData.Current.LocalSettings.Values["AccessGeo"] = true;
                string street = await gettingStreet();
                if (street != null)
                {
                    await gettingPostCode(firstPivotItem, street);
                }
            }
            else if (commandResult.Label.Equals("不允许"))
            {
                ApplicationData.Current.LocalSettings.Values["AccessGeo"] = false;
                gettingStreetFinished(false);      
            }
        }

        private async void updateDialogCommanHander(IUICommand commandResult)
        {
            if (commandResult.Label.Equals("开始下载"))
            {
                isDownloading = true;
                showUpdateStatus("开始下载...");

                ChangeProgress updateProgress = new ChangeProgress(changeProgress);
                await CommonTaskClient.Download(updateProgress);

                showUpdateStatus("下载完成,重启应用完成更改"); 
                await Task.Delay(1000);
                this.defaultViewModel["UpdateStatus"] = "";
            }
        }

        private void gettingStreetFinished(bool getAccessed = false, bool isSuccess = false)
        {
            if (getAccessed && isSuccess)
            {
                this.pinningStatus.Text = "开始获取邮政编码...";
            }
            else if (!isSuccess)
            {
                this.pinningStatus.Text = String.Empty;
                this.pinningLocation.Visibility = collapsed;
                this.resultStatus.Text = "获取地理位置信息失败";
                this.retryPin.Visibility = visible;
                this.retryPin.Content = "重试";
            }
            else
            {
                this.pinningStatus.Text = String.Empty;
                this.pinningLocation.Visibility = collapsed;
                this.resultStatus.Text = "拒绝访问地理位置信息";
            }
            this.isPinningFinished = true;
        }

        private void beforeGettingStreet()
        {
            if (this.retryPin.Visibility == visible)
            {
                this.retryPin.Visibility = collapsed;
                this.retryPin.Content = String.Empty;
            }
            this.resultStatus.Text = String.Empty;
            this.pinningLocation.Visibility = Visibility.Visible;
            this.pinningStatus.Text = "定位中....";
        }

        private async Task<string> gettingStreet()
        {
            string street = null;
            beforeGettingStreet();
            bool webRequestSucceed = true;
            try
            {
                MapClient locateStreet = MapClient.getInstance();
                street = await locateStreet.GetCurrentStreet();
            }
            catch (HttpRequestException)
            {
                webRequestSucceed = false;
            }
            catch (WebException)
            {
                webRequestSucceed = false;
            }
            gettingStreetFinished(true, webRequestSucceed);
            return street;
        }

        private async Task gettingPostCode(int pivotItemIndex, string streetAddress)
        {
            string result = null;
            bool webRequestFailed = false;
            try
            {
                if (pivotItemIndex == 1)
                {
                    if (streetAddress == null)
                    {
                        result = "没有找到匹配的邮政编码";
                    }
                    else
                    {
                        string postCode = await MapClient.getInstance().QueryPostCodeResult(streetAddress);
                        if (postCode != null)
                        {
                            result = "您查询的邮政编码是:\n" + postCode;
                        }
                        else
                        {
                            result = "没有找到匹配的邮政编码";
                        }
                    }
                    this.resultStatus.Text = result;
                    this.pinningLocation.Visibility = collapsed;
                    this.pinningStatus.Text = "获取邮编完成";
                    this.retryPin.Visibility = visible;
                    this.retryPin.Content = "重新定位";
                }
                else if (pivotItemIndex == 2)
                {
                    string postCode = await MapClient.getInstance().QueryPostCodeResult(streetAddress);
                    if (postCode != null)
                    {
                        this.postCodeDisplay.Text = "您查询的邮政编码是:\n" + postCode;
                    }
                    else
                    {
                        this.postCodeDisplay.Text = "没有找到匹配的邮政编码\n请输入所在地的道路名称";
                    }
                    this.fetchingPostCode.IsActive = false;
                }
            }
            catch (HttpRequestException)
            {
                webRequestFailed = true;
            }
            catch (WebException)
            {
                webRequestFailed = true;
            }
            if(webRequestFailed)
            {
                this.resultStatus.Text = "获取邮编失败";
                this.pinningLocation.Visibility = collapsed;
                this.pinningStatus.Text = "获取邮编完成";
                this.retryPin.Visibility = visible;
                this.retryPin.Content = "重新定位";
            }
        }

        public void changeProgress(int i)
        {
            if (this.downloadProgressBar.Visibility == collapsed && isDownloading)
            {
                this.downloadProgressBar.Visibility = visible;
                this.downloadProgress.Visibility = visible;
            }
            if (i == 100)
            {
                this.downloadProgressBar.Visibility = collapsed;
                this.downloadProgress.Visibility = collapsed;
                isDownloading = false;  // Download completed
            }
            this.downloadProgressBar.Value = i;
        }

        private void showUpdateStatus(string status)
        {
            VisualStateManager.GoToState(this, "TextBlockOpacityIncrease", true);  // Testing
            this.defaultViewModel["UpdateStatus"] = status;
            VisualStateManager.GoToState(this, "TextBlockOpacityReverse", true);  // Testing
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
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            Debug.WriteLine("Suspend");
            ApplicationData.Current.RoamingSettings.Values["IsUpdateChecked"] = isUpdateChecked;
        }

        private void pivot_Loaded(object sender, RoutedEventArgs e)
        {
            this.pivot.Title = "西安市邮政编码查询";
            this.localPostCode.Header = "当前位置";
            this.postCodeLibrary.Header = "邮编库";
            this.searchPostCode.Header = "搜索";
        }
         
        // Behaviours when text in searchBox changes
        private async void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchBoxText = (sender as TextBox).Text;
            bool webRequestFailed = false;
            if (searchBoxText.Length != 0 && this.searchBox.FocusState != FocusState.Unfocused)
            {
                this.DefaultViewModel[SearchedResultsName] = null; // Reset
                SearchedResults searchedResults = SearchedResults.GetInstance();
                try
                {
                    this.DefaultViewModel[SearchedResultsName] = await searchedResults.GetSearchedResults(searchBoxText);
                }
                catch (HttpRequestException)
                {
                    webRequestFailed = true;
                }
                catch (WebException)
                {
                    webRequestFailed = true;
                }
                // Show file not found dialog
                if (webRequestFailed)
                {
                    MessageDialog fileNotFoundDialog = new MessageDialog("网络连接错误!", "错误");
                    fileNotFoundDialog.Commands.Add(new UICommand("我知道了", null, 0));
                    var commandSelected = await fileNotFoundDialog.ShowAsync();
                }
                
            }
            else if (this.searchBox.FocusState != FocusState.Unfocused)
            {
                this.postCodeDisplay.Text = String.Empty;
            }
        }

        // Behaviours of searchBox events
        private void searchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.searchedResultsListView.Items.Count == 0)
            {
                this.searchBox.Text = String.Empty;
                this.searchBox.Foreground = this.blackBrush;
            }
        }

        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.searchedResultsListView.Items.Count == 0)
            {
                this.searchBox.Text = "搜索邮编";
                this.searchBox.Background = this.blackBrush;
                this.searchBox.Foreground = this.whiteBrush;
            }
            else
            {
                // Stop default appearance changes on searchBox
                this.searchBox.Background = this.whiteBrush;
                this.searchBox.Foreground = this.blackBrush;
            }
        }

        private void enterKeyDown_handler(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.Focus(FocusState.Programmatic);
            }
        }

        private async void searchedResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.fetchingPostCode.IsActive = true;           
            // Fetching post code 
            string streetAddress = (e.ClickedItem as SearchedResultItem).Address;
            this.DefaultViewModel[SearchedResultsName] = null;  // Disable search result list
            await gettingPostCode(secondPivotItem, streetAddress);
        }

        // Relocate user's current location
        private async void retryPin_Click(object sender, RoutedEventArgs e)
        {
            string street = await gettingStreet();
            if (street != null)
            {
                await gettingPostCode(firstPivotItem, street);
            }   
        }

        // AppBarButton clicked
        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            CommonTaskClient.SendFeedBack();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(AboutPage), null))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }
    }
}
