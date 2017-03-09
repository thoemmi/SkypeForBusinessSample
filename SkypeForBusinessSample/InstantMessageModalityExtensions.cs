using System;
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
    }
}