using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;

using NLog;

namespace SkypeForBusinessSample {
    public class LyncService {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private LyncClient _client;
        private bool _initializedBySample;
        private readonly Dictionary<string, Session> _activeSessions = new Dictionary<string, Session>();

        public void Start() {
            StartAsync().Wait();
        }

        private async Task StartAsync() {
            // enable UI suppression mode
            LyncHelper.SetUISuppressionMode(true);

            // kill all running Lync and S4B processes
            LyncHelper.KillAllInstances();

            try {
                //var lyncProcess = Process.Start("lync.exe");
                var lyncProcess = Process.Start("C:\\Program Files (x86)\\Microsoft Office\\Office15\\lync.exe");
                if (!LyncClient.GetClient().InSuppressedMode) {
                    lyncProcess.WaitForInputIdle(5000/*msec*/);
                }
                _logger.Debug("Lync.exe is running with UI suppression mode");
            }
            catch (Exception ex) {
                _logger.Debug(ex, @"lync.exe is not found: " + ex.Message);
            }

            _client = LyncClient.GetClient();
            _client.CredentialRequested += ClientOnCredentialRequested;

            // initialize the Lync client
            if (_client.State == ClientState.Uninitialized) {
                await _client.InitializeAsync();
                _initializedBySample = true;
            }

            RegisterEvents();

            await _client.SignInAsync();

            _logger.Debug($"Started, state is {_client.State}");

            _logger.Info($"Listening as {_client.Self.Contact.Uri.Replace("sip:", string.Empty)}");
        }

        private void ClientOnCredentialRequested(object sender, CredentialRequestedEventArgs args) {
            _logger.Debug($"Credential requested {args.Type}");

            //If the server type is Lync server and sign in credentials
            //are needed.
            if (args.Type == CredentialRequestedType.SignIn) {
                args.Submit(null, null, true);
            }
        }


        public void Stop() {
            if (_client == null) {
                return;
            }

            StopAsync().Wait();
        }

        private async Task StopAsync() {
            // Turn off event registration for state change
            UnregisterEvents();

            foreach (var session in _activeSessions.Values) {
                session.Close();
            }

            // disable UI suppression mode
            LyncHelper.SetUISuppressionMode(false);

            // sign out
            if (_client.State == ClientState.SignedIn) {
                _logger.Debug("Signing out");
                await _client.SignOutAsync();
            }

            // if this application initialized Lync, shut it down
            if (_client.State == ClientState.SignedOut) {
                if (_client.InSuppressedMode && _initializedBySample) {
                    _logger.Debug("shutting down");
                    await _client.ShutdownAsync();
                }
            }

            _logger.Debug("Stopped");
        }

        private void RegisterEvents() {
            _client.StateChanged += ClientOnStateChanged;
            _client.ConversationManager.ConversationAdded += OnConversationAdded;
            _client.ConversationManager.ConversationRemoved += OnConversationRemoved;
        }

        private void UnregisterEvents() {
            if (_client == null) {
                return;
            }

            _client.StateChanged -= ClientOnStateChanged;
            _client.ConversationManager.ConversationAdded -= OnConversationAdded;
            _client.ConversationManager.ConversationRemoved -= OnConversationRemoved;
        }


        private static void ClientOnStateChanged(object sender, ClientStateChangedEventArgs args) {
            var status = args.StatusCode < 0 ? new Win32Exception(args.StatusCode).Message : args.StatusCode.ToString();
            _logger.Debug($"State of Lync changed from {args.OldState} to {args.NewState}, status code {status}");
        }

        private void OnConversationAdded(object sender, ConversationManagerEventArgs args) {
            _logger.Debug("New conversation...");

            // only chat sessions are allowed
            if (!args.Conversation.Modalities.ContainsKey(ModalityTypes.InstantMessage)) {
                _logger.Info("Conversation does not contains a InstantMessage modality");
                args.Conversation.End();
                return;
            }

            // Create a Session instance for the new conversation
            var chatId = args.Conversation.Participants[1].Contact.Uri.Replace("sip:", "");
            Session session;
            if (!_activeSessions.TryGetValue(chatId, out session)) {
                session = new Session(chatId, args.Conversation);
                _activeSessions.Add(chatId, session);
            } else {
                _logger.Warn("There's already a session for this new conversation???");
            }
        }

        private void OnConversationRemoved(object sender, ConversationManagerEventArgs args) {
            _logger.Debug("On conversation removed");

            // try to remove the Session associated with the removed conversation
            var chatId = args.Conversation.Participants[1].Contact.Uri.Replace("sip:", "");
            Session session;
            if (_activeSessions.TryGetValue(chatId, out session)) {
                session.Dispose();
                _activeSessions.Remove(chatId);
            } else {
                _logger.Debug("Conversation ended which we didn't know about");
            }
        }
    }
}