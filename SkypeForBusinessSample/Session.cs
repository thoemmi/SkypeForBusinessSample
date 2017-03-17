using System;
using System.Threading.Tasks;

using Microsoft.Lync.Model.Conversation;

using NLog;

namespace SkypeForBusinessSample {
    public class Session : IDisposable {
        private readonly Logger _logger;
        private readonly string _chatId;
        private readonly Conversation _conversation;
        private readonly InstantMessageModality _participiantInstantMessageModality;

        public Session(string chatId, Conversation conversation) {
            _logger = LogManager.GetLogger(chatId, typeof(Session));

            _chatId = chatId;
            _conversation = conversation;

            _participiantInstantMessageModality = (InstantMessageModality)conversation.Participants[1].Modalities[ModalityTypes.InstantMessage];
            _participiantInstantMessageModality.InstantMessageReceived += OnInstantMessageReceived;

            _participiantInstantMessageModality.SendMessageAsync("welcome");

            _conversation.InitialContextReceived += OnInitialContextReceived;
            _conversation.ContextDataReceived += OnContextDataReceived;
        }

        private void OnInitialContextReceived(object sender, InitialContextEventArgs e) {
            _logger.Debug($"OnInitialContextReceived: {e.ApplicationData}");
        }

        private void OnContextDataReceived(object sender, ContextEventArgs e) {
            _logger.Debug($"OnContextDataReceived: {e.ContextDataType}");
        }

        public void Dispose() {
            _conversation.InitialContextReceived -= OnInitialContextReceived;
            _conversation.ContextDataReceived -= OnContextDataReceived;
            _participiantInstantMessageModality.InstantMessageReceived -= OnInstantMessageReceived;
        }

        private void OnInstantMessageReceived(object sender, MessageSentEventArgs e) {
            var text = e.Text?.Trim('\r', '\n');
            if (string.IsNullOrEmpty(text)) {
                _logger.Warn("text is empty");
            } else {
                // no await; otherwise, the thread which raised the event will block forever
#pragma warning disable 4014
                HandleText(text);
#pragma warning restore 4014
            }
        }

        private async Task HandleText(string text) {
            _logger.Info("Received {0}", text);

            var reply = "Echo: " + text;

            var modality = (InstantMessageModality)_conversation.SelfParticipant.Modalities[ModalityTypes.InstantMessage];
            await modality.SendMessageAsync(reply);

            switch (text.ToLowerInvariant()) {
                case "small":
                    await modality.SendImage(@"small_image.jpg");
                    break;
                case "big":
                    await modality.SendImage(@"big_image.jpg");
                    break;
            }
        }

        public void Close() {
            _conversation.End();
            Dispose();
        }
    }
}