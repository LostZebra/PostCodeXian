using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.Data.Json;
using Windows.Storage;

namespace PostCodeXian.Common
{
    static class CommonTaskClient
    {
        public static async void SendFeedBack()
        {
            EmailMessage em = new EmailMessage {Subject = "西安邮政编码查询_意见反馈"};
            em.To.Add(new EmailRecipient("xiaoyong19910227@gmail.com", "开发者"));
            await EmailManager.ShowComposeNewEmailAsync(em);
        }

        public static string[] AboutInfo()
        {
            string version = (ApplicationData.Current.LocalSettings.Values["DataFileVersion"] == null) ? "1.0" : ApplicationData.Current.LocalSettings.Values["DataFileVersion"].ToString();
            return new[] { "西安邮政编码查询", "版本：1.0.0.0\n开发者：肖勇\n邮编数据库版本：" + version};
        }

        public static async Task Download(UpdateProgress change)
        {
            ulong currentSize = 0;
            // Replace existed file
            const string fileName = "DistrictData.json";
            var newLibraryFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            Uri downloadUri = new Uri("http://postcodexian.cloudapp.net//MainService.svc/DownloadNewLibrary");
            using (HttpClient downloadClient = new HttpClient())
            {
                HttpResponseMessage responseMessage = await downloadClient.GetAsync(downloadUri);
                responseMessage.EnsureSuccessStatusCode();

                string tempStr = String.Empty;
                byte[] buffer = new byte[100];  // Buffer
                using (var downloaded = await responseMessage.Content.ReadAsStreamAsync())
                {
                    int totalBytes = (int)downloaded.Length;
                    var readBytes = downloaded.Read(buffer, 0, buffer.Length);
                    while(readBytes > 0)
                    {
                        await Task.Delay(200);  // Simulated time delay
                        tempStr += Encoding.UTF8.GetString(buffer, 0, readBytes);
                        currentSize += (ulong)readBytes;
                        change(((int)currentSize * 100 / totalBytes));
                        readBytes = downloaded.Read(buffer, 0, readBytes);
                    }
                    await FileIO.WriteTextAsync(newLibraryFile, tempStr);
                    downloaded.Flush();
                }
            }     
        }

        public static async Task UpdateFileVersion()
        {
            Uri clientUri = new Uri("http://postcodexian.cloudapp.net/MainService.svc/FileDescription");
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage responseMessage = await client.GetAsync(clientUri);
                responseMessage.EnsureSuccessStatusCode();
                JsonObject fileDesJson = JsonObject.Parse(await responseMessage.Content.ReadAsStringAsync());
                // Get file description successful
                if (fileDesJson["Status"].GetString().Equals("OK"))
                {
                    // Update file version
                    ApplicationData.Current.LocalSettings.Values["DataFileVersion"] = fileDesJson["FileVersion"].GetString();
                }
            }    
        }

        public static async Task<bool> IsUpdateAvailable()
        {
            Uri clientUri = new Uri("http://postcodexian.cloudapp.net/MainService.svc/FileDescription");
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage responseMessage = await client.GetAsync(clientUri);
                responseMessage.EnsureSuccessStatusCode();
                JsonObject fileDesJson = JsonObject.Parse(await responseMessage.Content.ReadAsStringAsync());
                // Get file description successful
                if (fileDesJson["Status"].GetString().Equals("OK"))
                {
                    if (HasNewerVersion(fileDesJson["FileVersion"].GetString()))
                    {
                        return true;
                    }
                }
                return false;
            }    
        }

        private static bool HasNewerVersion(string newVersion)
        {
            string currentVersion = (string)ApplicationData.Current.LocalSettings.Values["DataFileVersion"];
            if (currentVersion == null)
            {
                return true;
            }
            string[] currentVersionDigits = currentVersion.Split('.');
            string[] futureVersionDigits = newVersion.Split('.');

            return futureVersionDigits.Where((t, i) => currentVersionDigits[i].CompareTo(t) < 0).Any();
        }
    }
}
