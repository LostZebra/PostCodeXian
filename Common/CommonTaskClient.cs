using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
// Email
using Windows.ApplicationModel.Email;
// Storage
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;
// For debugging
using System.Diagnostics;
// NET & Data
using Windows.Data.Json;
using System.Xml;
using System.Xml.Linq;
using System.Net.Http;
using System.IO;

namespace PostCodeXian
{
    static class CommonTaskClient
    {
        private static double fileSize = 0.0;

        public static async void SendFeedBack()
        {
            EmailMessage em = new EmailMessage();
            em.Subject = "西安邮政编码查询_意见反馈";
            em.To.Add(new EmailRecipient("xiaoyong19910227@gmail.com", "开发者"));
            await EmailManager.ShowComposeNewEmailAsync(em);
        }

        public static string[] AboutInfo()
        {
            return new[] { "西安邮政编码查询", "版本：1.0.0.0\n开发者：肖勇\n邮编数据库版本：1.0"};
        }

        public static async Task Download(ChangeProgress change)
        {
            ulong currentSize = 0;
            // Replace existed file
            string fileName = "info.txt";
            var newLibraryFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            Uri downloadUri = new Uri("http://localhost:62874/MainService.svc/DownloadNewLibrary");
            using (HttpClient downloadClient = new HttpClient())
            {
                HttpResponseMessage responseMessage = await downloadClient.GetAsync(downloadUri);
                responseMessage.EnsureSuccessStatusCode();
                
                byte[] buffer = new byte[100];  // Buffer
                using (var downloaded = await responseMessage.Content.ReadAsStreamAsync())
                {
                    var readBytes = downloaded.Read(buffer, 0, buffer.Length);
                    while(readBytes > 0)
                    {
                        await Task.Delay(100);  // Simulated time delay
                        currentSize += (ulong)readBytes;
                        await FileIO.WriteBytesAsync(newLibraryFile, buffer);
                        change((int)(currentSize * 100 / fileSize));
                        readBytes = downloaded.Read(buffer, 0, buffer.Length);
                    }
                }
            }     
        }

        public static async Task<bool> IsUpdateAvailable()
        {
            Uri clientUri = new Uri("http://localhost:62874/MainService.svc/FileDescription");
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
                        fileSize = fileDesJson["FileSize"].GetNumber();
                        return true;
                    }
                }
                return false;
            }    
        }

        private static bool HasNewerVersion(string newVersion)
        {
            string currentVersion = AboutInfo()[1].Substring(26, 3);
            string[] currentVersionDigits = currentVersion.Split('.');
            string[] futureVersionDigits = newVersion.Split('.');

            for (int i = 0; i < futureVersionDigits.Length; i++)
            {
                if (currentVersionDigits[i].CompareTo(futureVersionDigits[i]) < 0)
                    return true;
            }
            return false;
        }
    }
}
