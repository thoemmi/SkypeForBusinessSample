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

            _participiantInstantMessageModality = ((InstantMessageModality)conversation.Participants[1].Modalities[ModalityTypes.InstantMessage]);
            _participiantInstantMessageModality.InstantMessageReceived += OnInstantMessageReceived;

            _participiantInstantMessageModality.SendMessageAsync("welcome");
        }

        public void Dispose() {
            _participiantInstantMessageModality.InstantMessageReceived -= OnInstantMessageReceived;
        }

        private void OnInstantMessageReceived(object sender, MessageSentEventArgs e) {
            HandleText(e.Text);
        }

        private async Task HandleText(string text) {
            _logger.Info("Received {0}", text);

            var reply = "Echo: " + text;

            var modality = (InstantMessageModality)_conversation.SelfParticipant.Modalities[ModalityTypes.InstantMessage];
            await modality.SendMessageAsync(reply);
        }

        public void Close() {
            _conversation.End();
            Dispose();
        }
    }
}