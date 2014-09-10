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

namespace PostCodeXian
{
    static class CommonTaskClient
    {
        public static async void SendFeedBack()
        {
            EmailMessage em = new EmailMessage();
            em.Subject = "西安邮政编码查询_意见反馈";
            em.To.Add(new EmailRecipient("xiaoyong19910227@gmail.com", "开发者"));
            await EmailManager.ShowComposeNewEmailAsync(em);
        }

        public static string[] AboutInfo()
        {
            return new[] { "西安邮政编码查询", "版本：1.0.0\n开发者：肖勇"};
        }

        public static async Task Download(ChangeProgress change)
        {
            // Test code
            // Here fill the logic of download
            ulong currentSize = 0;
            StorageFile fromFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Common/ReadMe.txt"));
            var fromFileProperties = await fromFile.GetBasicPropertiesAsync();
            var fromFileSize = fromFileProperties.Size;

            // byte[] buffer = new byte[1024];
            string fileName = "ReadMe.txt";
            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);
            var newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using (IInputStream inputStream = await fromFile.OpenSequentialReadAsync())
            {
                IBuffer buffer = new Windows.Storage.Streams.Buffer(100);
                for (buffer = await inputStream.ReadAsync(buffer, 100, InputStreamOptions.ReadAhead); buffer.Length > 0;
                    buffer = await inputStream.ReadAsync(buffer, 100, InputStreamOptions.ReadAhead))
                {
                    await Task.Delay(100);
                    currentSize += (ulong)buffer.Length;
                    await FileIO.WriteBufferAsync(newFile, buffer);
                    change((int)(currentSize * 100 / fromFileSize));
                }
            }
        }
    }
}
