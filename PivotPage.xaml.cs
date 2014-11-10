using PostCodeXian.Common;
using PostCodeXian.Data;
using System;
using System.Threading.Tasks;
using System.IO;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
// StatusBar
using Windows.UI.ViewManagement;
// For debugging
using System.Diagnostics;
// Net & Data
using System.Net;
using System.Net.Http;
using Windows.Storage;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace PostCodeXian
{
    delegate void UpdateProgress(int i);
    public sealed partial class PivotPage
    {
        private const string DistrictDataSetName = "DistrictDataSet";
        private const string ProgressBarWidthName = "ProgressBarWidth";
        private const string SearchedResultsName = "SearchedResultsGroup";
        private const Visibility Visible = Visibility.Visible;
        private const Visibility Collapsed = Visibility.Collapsed;
        private const int FirstPivotItem = 1;
        private const int SecondPivotItem = 2;

        private readonly NavigationHelper _navigationHelper;
        private readonly ObservableDictionary _defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView(@"Resources");
        private StatusBar _statusBar = StatusBar.GetForCurrentView();

        private bool _isDownloading;
        private bool _isUpdateChecked;
        private bool _isPinningFinished;

        // Black and white brush
        private readonly SolidColorBrush _whiteBrush = new SolidColorBrush(Colors.White);
        private readonly SolidColorBrush _blackBrush = new SolidColorBrush(Colors.Black);

        public PivotPage()
        {
            this.InitializeComponent();

            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += NavigationHelper_LoadState;
            _navigationHelper.SaveState += NavigationHelper_SaveState;

            this.SearchedResultsListView.ItemClick += SearchedResultsListView_ItemClick;
            this.SearchedResultsListView.ItemClick += SearchBox_LostFocus;
            this.SearchBox.KeyDown += enterKeyDown_handler;

            // Modify download title layout
            double width = Window.Current.Bounds.Width;
            _defaultViewModel[ProgressBarWidthName] = width - 50;

            // Behaviours when selected Pivot item changes
            this.Pivot.SelectionChanged += delegate(object sender, SelectionChangedEventArgs e)
            {
                var pivot = sender as Pivot;
                if (pivot != null && pivot.SelectedIndex == 1)
                {
                    if (!_isPinningFinished)
                    {
                        SecondPivot_Loaded();
                    }
                }
            };
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
                _isUpdateChecked = true;
                _isDownloading = true;
                ShowUpdateStatus("正在准备数据");
                UpdateProgress updateProgress = ChangeProgress;
                try
                {
                    await CommonTaskClient.CheckFile();
                    await CommonTaskClient.Download(updateProgress);
                }
                catch (HttpRequestException)
                {
                    ShowUpdateStatus("网络连接错误");
                }
            }
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            DistrictDataSource dataSource = DistrictDataSource.GetInstance();
            try
            {
                await dataSource.GetDistrictData();
                _defaultViewModel[DistrictDataSetName] = dataSource;
            }
            catch (FileNotFoundException)
            {
                ShowUpdateStatus("本地邮编数据库加载失败");
            }
            
            // Async check update
            _isUpdateChecked = (e.PageState == null || !e.PageState.ContainsKey("IsUpdateChecked")) ? _isUpdateChecked : (bool)e.PageState["IsUpdateChecked"];
            if (!_isUpdateChecked)
            {
                _isUpdateChecked = true;
                bool isCheckUpdateSucceed = true;

                ShowUpdateStatus("正在检查更新...");
                _statusBar.ProgressIndicator.ShowAsync();

                try
                {
                    bool isUpdateAvailable = await CommonTaskClient.IsUpdateAvailable();
                    await Task.Delay(500);
                    if (isUpdateAvailable)
                    {
                        ShowUpdateStatus("邮编数据库有新版本");  
                        MessageDialog updateAvalible = new MessageDialog("有可用的邮编数据库更新，要下载吗？", "提示");
                        updateAvalible.Commands.Add(new UICommand("开始下载", UpdateDialogCommanHander, 0));
                        updateAvalible.Commands.Add(new UICommand("以后再说", null, 1));
                        updateAvalible.DefaultCommandIndex = 0;
                        updateAvalible.CancelCommandIndex = 1;
                        await updateAvalible.ShowAsync();
                    }
                    else
                    {
                        ShowUpdateStatus("无可用更新");
                        _statusBar.ProgressIndicator.HideAsync();
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
                    ShowUpdateStatus("获取更新失败");
                    _statusBar.ProgressIndicator.HideAsync();
                }
                await Task.Delay(500);
                _defaultViewModel["UpdateStatus"] = "";
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
            e.PageState["IsUpdateChecked"] = _isUpdateChecked;
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
                throw new Exception(_resourceLoader.GetString(@"NavigationFailedExceptionMessage"));
            }
        }

        /// <summary>
        /// Loads the content for the second Pivot item when it is scrolled into view.
        /// </summary>
        private async void SecondPivot_Loaded()
        {
            if (ApplicationData.Current.LocalSettings.Values["AccessGeo"] == null)
            {
                MessageDialog accessingGeoLocator = new MessageDialog("请求访问地理信息数据", "提示");
                accessingGeoLocator.Commands.Add(new UICommand("允许", geoDialogCommandHandler, 0));
                accessingGeoLocator.Commands.Add(new UICommand("不允许", geoDialogCommandHandler, 0));
                accessingGeoLocator.DefaultCommandIndex = 0;
                accessingGeoLocator.CancelCommandIndex = 1;
                await accessingGeoLocator.ShowAsync();
                
            }
            else if (ApplicationData.Current.LocalSettings.Values["AccessGeo"].Equals(false))
            {
                MessageDialog refuseAccessGeoLocator = new MessageDialog("访问地理位置信息被拒绝", "提示");
                refuseAccessGeoLocator.Commands.Add(new UICommand("放弃", null, 0));
                refuseAccessGeoLocator.Commands.Add(new UICommand("给予权限", geoDialogCommandHandler, 1));
                refuseAccessGeoLocator.DefaultCommandIndex = 0;
                refuseAccessGeoLocator.CancelCommandIndex = 1;
                await refuseAccessGeoLocator.ShowAsync();
            }
            else
            {
                string street = await GettingStreet();
                if (street != null)
                {
                    await GettingPostCode(FirstPivotItem, street);
                }
            }
        }

        // Handle MessageDialog events
        private async void geoDialogCommandHandler(IUICommand commandResult)
        {
            if (commandResult.Label.Equals("允许") || commandResult.Label.Equals("给予权限"))
            {
                ApplicationData.Current.LocalSettings.Values["AccessGeo"] = true;
                string street = await GettingStreet();
                if (street != null)
                {
                    await GettingPostCode(FirstPivotItem, street);
                }
            }
            else if (commandResult.Label.Equals("不允许"))
            {
                ApplicationData.Current.LocalSettings.Values["AccessGeo"] = false;
                GettingStreetFinished();      
            }
        }

        private async void UpdateDialogCommanHander(IUICommand commandResult)
        {
            if (commandResult.Label.Equals("开始下载"))
            {
                ShowUpdateStatus("开始下载...");
                _isDownloading = true;

                UpdateProgress updateProgress = ChangeProgress;
                await CommonTaskClient.Download(updateProgress);

                ShowUpdateStatus("下载完成,重启应用完成更改"); 
                await Task.Delay(1000);  // Simulated task delay
                _defaultViewModel["UpdateStatus"] = "";
            }
        }

        private void GettingStreetFinished(bool getAccessed = false, bool isSuccess = false)
        {
            if (getAccessed && isSuccess)
            {
                this.PinningStatus.Text = "开始获取邮政编码...";
            }
            else if (!isSuccess)
            {
                this.PinningStatus.Text = String.Empty;
                this.PinningLocation.Visibility = Collapsed;
                this.ResultStatus.Text = "获取地理位置信息失败";
                this.RetryPin.Visibility = Visible;
                this.RetryPin.Content = "重试";
            }
            else
            {
                this.PinningStatus.Text = String.Empty;
                this.PinningLocation.Visibility = Collapsed;
                this.ResultStatus.Text = "拒绝访问地理位置信息";
            }
            _isPinningFinished = true;
        }

        private void BeforeGettingStreet()
        {
            if (this.RetryPin.Visibility == Visible)
            {
                this.RetryPin.Visibility = Collapsed;
                this.RetryPin.Content = String.Empty;
            }
            this.ResultStatus.Text = String.Empty;
            this.PinningLocation.Visibility = Visibility.Visible;
            this.PinningStatus.Text = "定位中....";
        }

        private async Task<string> GettingStreet()
        {
            string street = null;
            BeforeGettingStreet();
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
            GettingStreetFinished(true, webRequestSucceed);
            return street;
        }

        private async Task GettingPostCode(int pivotItemIndex, string streetAddress)
        {
            bool webRequestFailed = false;
            try
            {
                switch (pivotItemIndex)
                {
                    case 1:
                    {
                        string result;
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
                        this.ResultStatus.Text = result;
                        this.PinningLocation.Visibility = Collapsed;
                        this.PinningStatus.Text = "获取邮编完成";
                        this.RetryPin.Visibility = Visible;
                        this.RetryPin.Content = "重新定位";
                        break;
                    }
                    case 2:
                    {
                        string postCode = await MapClient.getInstance().QueryPostCodeResult(streetAddress);
                        if (postCode != null)
                        {
                            this.PostCodeDisplay.Text = "您查询的邮政编码是:\n" + postCode;
                        }
                        else
                        {
                            this.PostCodeDisplay.Text = "没有找到匹配的邮政编码\n请输入所在地的道路名称";
                        }
                        this.FetchingPostCode.IsActive = false;
                        break;
                    }
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
                this.ResultStatus.Text = "获取邮编失败";
                this.PinningLocation.Visibility = Collapsed;
                this.PinningStatus.Text = "获取邮编完成";
                this.RetryPin.Visibility = Visible;
                this.RetryPin.Content = "重新定位";
            }
        }

        public void ChangeProgress(int i)
        {
            if (this.DownloadProgressBar.Visibility == Collapsed && _isDownloading)
            {
                this.DownloadProgressBar.Visibility = Visible;
                this.DownloadProgress.Visibility = Visible;
            }
            if (i == 100)
            {
                this.DownloadProgressBar.Visibility = Collapsed;
                this.DownloadProgress.Visibility = Collapsed;
                _isDownloading = false;  // Download completed
            }
            this.DownloadProgressBar.Value = i;
        }

        private void ShowUpdateStatus(string status)
        {
            VisualStateManager.GoToState(this, "TextBlockOpacityIncrease", true);  // Testing
            _defaultViewModel["UpdateStatus"] = status;
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
            _navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Pivot_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(sender);
        }

        // Behaviours when text in SearchBox changes
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchBoxText = (sender as TextBox).Text;
            bool webRequestFailed = false;
            if (searchBoxText.Length != 0 && this.SearchBox.FocusState != FocusState.Unfocused)
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
                    await fileNotFoundDialog.ShowAsync();
                }
                
            }
            else if (this.SearchBox.FocusState != FocusState.Unfocused)
            {
                this.PostCodeDisplay.Text = String.Empty;
            }
        }

        // Behaviours of SearchBox events
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.SearchedResultsListView.Items == null || this.SearchedResultsListView.Items.Count != 0) return;
            this.SearchBox.Text = String.Empty;
            this.SearchBox.Foreground = _blackBrush;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.SearchedResultsListView.Items != null && this.SearchedResultsListView.Items.Count == 0)
            {
                this.SearchBox.Text = "搜索邮编";
                this.SearchBox.Background = _blackBrush;
                this.SearchBox.Foreground = _whiteBrush;
            }
            else
            {
                // Stop default appearance changes on SearchBox
                this.SearchBox.Background = _whiteBrush;
                this.SearchBox.Foreground = _blackBrush;
            }
        }

        private void enterKeyDown_handler(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.Focus(FocusState.Programmatic);
            }
        }

        private async void SearchedResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.FetchingPostCode.IsActive = true;           
            // Fetching post code 
            var clickedItem = e.ClickedItem as SearchedResultItem;
            if (clickedItem != null)
            {
                string streetAddress = clickedItem.Address;
                this.DefaultViewModel[SearchedResultsName] = null;  // Disable search result list
                await GettingPostCode(SecondPivotItem, streetAddress);
            }
        }

        // Relocate user's current location
        private async void RetryPin_Click(object sender, RoutedEventArgs e)
        {
            string street = await GettingStreet();
            if (street != null)
            {
                await GettingPostCode(FirstPivotItem, street);
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
                throw new Exception(_resourceLoader.GetString(@"NavigationFailedExceptionMessage"));
            }
        }
    }
}
