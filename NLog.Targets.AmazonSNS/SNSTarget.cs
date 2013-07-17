#region Microsoft Public License (Ms-PL)

// // Microsoft Public License (Ms-PL)
// // 
// // This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// // 
// // 1. Definitions
// // 
// // The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// // 
// // A "contribution" is the original software, or any additions or changes to the software.
// // 
// // A "contributor" is any person that distributes its contribution under this license.
// // 
// // "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// // 
// // 2. Grant of Rights
// // 
// // (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// // 
// // (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// // 
// // 3. Conditions and Limitations
// // 
// // (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// // 
// // (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// // 
// // (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// // 
// // (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// // 
// // (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using Amazon;
using Amazon.SimpleNotificationService;
using NLog;
using NLog.Targets; 
using NLog.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Amazon.SimpleNotificationService.Model;
using NLog.Common;

namespace NLog.Targets.AmazonSNS
{
    [Target("SNSTarget")]
    public class SNSTarget : TargetWithLayout
    {
        private static readonly int DEFAULT_MAX_MESSAGE_SIZE = 64;
        private static readonly string TRUNCATE_MESSAGE = " [truncated]";
        private static readonly Encoding TRANSFER_ENCODING = Encoding.UTF8;
        private int truncateSizeInBytes;

        private AmazonSimpleNotificationServiceClient client;

        public string AwsAccessKey { get; set; }
        public string AwsSecretKey { get; set; }
        [DefaultValue("us-east-1")]
        public string Endpoint { get; set; }
        [DefaultValue("{$message}")]
        public Layout Subject { get; set; }
        public string TopicArn { get; set; }
        public int? MaxMessageSize { get; set; }
        public int ConfiguredMaxMessageSizeInBytes { get; private set; }

        public SNSTarget()
        {
            Subject = "{$message}";
            Endpoint = "us-east-1";
        }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            var size = MaxMessageSize ?? 64;
            if (size <= 0 || size >= 256)
            {
                ConfiguredMaxMessageSizeInBytes = 256 * 1024;
            }
            else if (size <= 64)
            {
                ConfiguredMaxMessageSizeInBytes = 64 * 1024;
            }
            else
            {
                ConfiguredMaxMessageSizeInBytes = size * 1024;
            }
            InternalLogger.Info(string.Format("Max message size is set to {0} KB.", ConfiguredMaxMessageSizeInBytes / 1024));
            truncateSizeInBytes = ConfiguredMaxMessageSizeInBytes - TRANSFER_ENCODING.GetByteCount(TRUNCATE_MESSAGE);

            try
            {
                if (string.IsNullOrEmpty(AwsAccessKey) && string.IsNullOrEmpty(AwsSecretKey))
                {
                    InternalLogger.Info("AWS Access Keys are not specified. Use Application Setting or EC2 Instance profile for keys.");
                    client = new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(Endpoint));
                }
                else
                {
                    client = new AmazonSimpleNotificationServiceClient(AwsAccessKey, AwsSecretKey, RegionEndpoint.GetBySystemName(Endpoint));
                }
            }
            catch (Exception e)
            {
                InternalLogger.Fatal("Amazon SNS client failed to be configured. This logger wont'be send any message. Error is\n{0}\n{1}", e.Message, e.StackTrace);
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var logMessage = Layout.Render(logEvent);
            var subject = Subject.Render(logEvent);

            var count = Encoding.UTF8.GetByteCount(logMessage);
            if (count > ConfiguredMaxMessageSizeInBytes)
            {
                if (InternalLogger.IsWarnEnabled)
                    InternalLogger.Warn("logging message will be truncted. original message is\n{0}",
                        logMessage);
                logMessage = logMessage.LeftB(TRANSFER_ENCODING, truncateSizeInBytes)
                     + TRUNCATE_MESSAGE;
            }
            try
            {
                client.Publish(new PublishRequest()
                {
                    Message = logMessage,
                    Subject = subject,
                    TopicArn = TopicArn
                });
            }
            catch (AmazonSimpleNotificationServiceException e)
            {
                InternalLogger.Fatal("RequstId: {0},ErrorType: {1}, Status: {2}\nFailed to send log with\n{3}\n{4}",
                    e.RequestId, e.ErrorType, e.StatusCode,
                    e.Message, e.StackTrace);
            }
            
        }
    }

    public static class StringExtention
    {
        public static string LeftB(this string s, Encoding encoding, int maxByteCount)
        {
            var bytes = encoding.GetBytes(s);
            if (bytes.Length <= maxByteCount) return s;

            var result = s.Substring(0,
                encoding.GetString(bytes, 0, maxByteCount).Length);

            while (encoding.GetByteCount(result) > maxByteCount)
            {
                result = result.Substring(0, result.Length - 1);
            }
            return result;
        }
    }

}
