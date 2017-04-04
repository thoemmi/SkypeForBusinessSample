using System.Diagnostics;

using Microsoft.Win32;

using NLog;

namespace SkypeForBusinessSample {
    public static class LyncHelper {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static void KillAllInstances() {
            foreach (var item in new[] { "UcMapi", "lync" }) {
                foreach (var proc in Process.GetProcessesByName(item)) {
                    proc.Kill();
                    _logger.Debug("killed the existing lync instance:" + proc);
                }
            }
        }

        public static void SetUISuppressionMode(bool enabled) {
            // Lync 2013 Client
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            var lyncClient = baseKey.OpenSubKey(@"Software\Microsoft\Office\15.0\Lync", true);
            if (lyncClient != null) {
                lyncClient.SetValue("UISuppressionMode", enabled ? 1 : 0, RegistryValueKind.DWord);
                _logger.Debug($"UISuppressionMode {(enabled ? "enabled" : "disabled")} for Lync 2013 client");
            } else {
                _logger.Debug("It looks like Lync 2013 client has not installed at your system");
            }

            // Skype For Business
            var skypeforBusiness = baseKey.OpenSubKey(@"Software\Microsoft\Office\16.0\Lync", true);
            if (skypeforBusiness != null) {
                skypeforBusiness.SetValue("UISuppressionMode", enabled ? 1 : 0, RegistryValueKind.DWord);
                _logger.Debug($"UISuppressionMode {(enabled ? "enabled" : "disabled")} for Skype for business");
            } else {
                _logger.Debug("It looks like Skype for business has not installed at your system");
            }
        }
    }
}