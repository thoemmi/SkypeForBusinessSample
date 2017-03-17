using System.Threading.Tasks;

using Microsoft.Lync.Model;

namespace SkypeForBusinessSample {
    public static class LyncClientExtensions {
        public static Task InitializeAsync(this LyncClient client) {
            return Task.Factory.FromAsync(client.BeginInitialize, client.EndInitialize, null);
        }

        public static Task SignInAsync(this LyncClient client) {
            return Task.Factory.FromAsync((callback, state) => client.BeginSignIn("", "", "", callback, state), client.EndSignIn, null);
        }

        public static Task SignOutAsync(this LyncClient client) {
            return Task.Factory.FromAsync(client.BeginSignOut, client.EndSignOut, null);
        }

        public static Task ShutdownAsync(this LyncClient client) {
            return Task.Factory.FromAsync(client.BeginShutdown, client.EndShutdown, null);
        }
    }
}