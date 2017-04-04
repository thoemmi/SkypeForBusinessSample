# SkypeForBusinessSample

This repository demonstrates why the [Lync Client SDK](https://msdn.microsoft.com/de-de/library/office/jj933180.aspx)
is **not** suitable for writing bots for Lync/Skype for Business.

## Introduction

We tried to create a bot for Lync/Skype for Business. First we planned to use
the server-side interface UCMA ([Unified Communications Managed API](https://msdn.microsoft.com/de-de/library/office/dn465943.aspx)), but the
configuration is far from trivial: you have to call several PowerShell
scripts, create certificates to get a trusted connection from the Lync
Server to the API endpoint, and so on. While this would be feasible
in-house, it's not something we want to bother our customers with. (I can
only imagine the amount of calls our support would have to deal with).

However, we were recommended to use the [Lync Client SDK](https://msdn.microsoft.com/de-de/library/office/jj933180.aspx)
instead. Using the [UI Suppression Mode](https://msdn.microsoft.com/en-us/library/office/jj933224.aspx)
you can even run Lync headless, i.e. suppressing its own UI.

## Issues

However, it turned out that this approach does not fulfil our needs for
two reasons:

### Size limit of transferred images

If you use the UI Suppression Mode, you cannot use [`LyncClient.GetAutomation()`](https://msdn.microsoft.com/de-de/library/office/microsoft.lync.model.lyncclient.getautomation_di_3_uc_ocs14mreflyncwpf.aspx).
Instead you have to encode the image as base64 and send it as a message:

```csharp
public static void SendImage(this InstantMessageModality modality, string filepath) {
    byte[] bytes;
    if (Path.GetExtension(filepath).ToLowerInvariant() == "gif") {
        bytes = File.ReadAllBytes(filepath);
    } else {
        // convert image to GIF
        using (var image = Image.FromFile(filepath)) {
            using (var memStream = new MemoryStream()) {
                image.Save(memStream, ImageFormat.Gif);
                bytes = memStream.ToArray();
            }
        }
    }

    var data = "base64:" + Convert.ToBase64String(bytes);

    var formattedMessage = new Dictionary<InstantMessageContentType, string> {
        { InstantMessageContentType.Gif, data }
    };


    modality.BeginSendMessage(formattedMessage, modality.EndSendMessage, null);
}
```

However, messages are strings and as such limited to 64K characters. Because of
the base64 encoding, this results in a maximum image size of approx. 50 kB.

Unfortunately, we haven't found any other method in the SDK, which is capable of
sending larger images. One solution is scale down an image until its base64 
representation is less than 64 KB, but your image would get blurry, which is
not an option for us.

### The Windows account running the bot must be logged in

Our bot is not an desktop application, but should run on a server, unattended.
Its purpose is to reply to other user's requests. That's what bots are doing.

Therefore we want to run our bot as a Windows service. It all went well using
the Lync Client SDK as long as we developed the bot on our machines. However,
when running it on a server without a user being logged on, the bot went
offline too.

Further investigation showed this behaviour: The Lync Client SDK does not take
to the Lync Server directly, but relies on a running `lync.exe` client. And
that client in turn relies that the account, in which session the executable
is running, must have a desktop.

Let me give you an example: the Windows service is running as `DOMAIN\BotAccount`.
The service spawns a `lync.exe`, running as `DOMAIN\BotAccount` too. That Lync
client's presence is online only if `DOMAIN\BotAccount` has a desktop, i.e.
is logged on on that machine. As soon as the user `BotAccount` lgs out, the
Lync client changes its state to offline.

That's another reason why we cannot use the Lync Client SDK: we cannot demand
our customers to have the bot's account always being logged on. That contradicts
the purpose of a server application without any UI.

## Sample application

The sample application in this repository demonstrates the issues described
above.

### Image issue

Start it without any parameter just runs the bot in a console, using the
current user's account. From another account on another machine send
`small` to the bot, and it will reply with a small image (6 KB), which
will work. If you send `big`, the bot will try to send a large image (1.5 MB),
which will fail. Instead, the bot will behave weird, like constantly changing
its state from offline to uninitialized and back.

### Session issue

The application uses [TopShelf](http://topshelf-project.com/) to run as a service.
Following commands are necessary (with elevated privileges):

* `SkypeForBusinessSample.exe install --interactive` to install the Windows
  service, it will ask for the desired Windows account to run the service as.
* `SkypeForBusinessSample.exe start` to start the service
* `SkypeForBusinessSample.exe stop` to stop the service
* `SkypeForBusinessSample.exe uninstall` for uninstallation

(see TopShelf's [documentation](https://topshelf.readthedocs.io/en/latest/overview/commandline.html)
for all command line options)

So install the service with your own credentials and start it. Test from another
machine with a different account to talk to the bot (it will echo any message).
Then log off from the machine where the bot is running. The bot will go offline
immediately and not respond to any message anymore.

## Conclusion

Because of the limitation described above, the Lync Client SDK is not suitable
for writing bots. The Bot Framework SDK does not support Lync/Skype for Business
(yet), so it seems the only option at the moment is to use the UCMA.