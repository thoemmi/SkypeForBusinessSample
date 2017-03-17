using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Lync.Model.Conversation;

namespace SkypeForBusinessSample {
    public static class InstantMessageModalityExtensions {
        public static Task SendMessageAsync(this InstantMessageModality modality, string text) {
            if (!modality.CanInvoke(ModalityAction.SendInstantMessage)) {
                throw new InvalidOperationException("Operation doesn't accept SendInstantMessage");
            }

            return Task.Factory.FromAsync((callback, state) => modality.BeginSendMessage(text, callback, state), modality.EndSendMessage, null);
        }

        public static Task SendMessageAsync(this InstantMessageModality modality, string data, InstantMessageContentType type) {
            var formattedMessage = new Dictionary<InstantMessageContentType, string> {
                { type, data }
            };

            if (!modality.CanInvoke(ModalityAction.SendInstantMessage)) {
                throw new InvalidOperationException("Operation doesn't accept SendInstantMessage");
            }

            return Task.Factory.FromAsync(modality.BeginSendMessage, modality.EndSendMessage, formattedMessage, modality);
        }

        public static Task SendImage(this InstantMessageModality modality, string filepath) {
            byte[] bytes;
            if (Path.GetExtension(filepath).ToLowerInvariant() == "gif") {
                bytes = File.ReadAllBytes(filepath);
            } else {
                using (var image = Image.FromFile(filepath)) {
                    using (var memStream = new MemoryStream()) {
                        image.Save(memStream, ImageFormat.Gif);
                        bytes = memStream.ToArray();
                    }
                }
            }

            var data = "base64:" + Convert.ToBase64String(bytes);
            return SendMessageAsync(modality, data, InstantMessageContentType.Gif);
        }
    }
}