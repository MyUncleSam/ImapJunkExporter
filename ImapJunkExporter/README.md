# What is ImapJunkExporter
This program exports the junk folder of a given mail server.

# Why
I am using mailcow with an encrypted mail storage. Using it encrypted is secure but removes the possibility of auto learning spam mails in rspamd. So I wrote this to:
- export the junk folder to a local folder
- run a console command to learn all exported mails as junk

# Configuration
You need to configure the mailboxes in the `appsettings.json`.

## Example
```
{
  "Cron": {
    "RunOnce": false,
    "Schedule": "0 0 * * *"
  },
  "Worker": {
    "ProtocolEmlBaseInformation": true
  },
  "Mailboxes": [
    {
      "ImapHost": "your.email.host.tld",
      "ImapPort": 993,
      "ImapUseSsl": true,
      "ImapUsername": "info@your.email",
      "ImapPassword": "base64_encoded_password",
      "TargetLocalFolder": "/path/to/where/extract/junk/mails",
      "TargetFilenamePrefix": "info_your_email_",
      "IgnoreSpamMessages": true
    },
    {
      "ImapHost": "your.email.host.tld",
      "ImapPort": 993,
      "ImapUseSsl": true,
      "ImapUsername": "support@your.email",
      "ImapPassword": "base64_encoded_password",
      "TargetLocalFolder": "/path/to/where/extract/junk/mails",
      "TargetFilenamePrefix": "support_your_email_",
      "IgnoreSpamMessages": true
    }
  ]
}
```

## Description
In the description below the path to configuration elements is written as a string. This string represents the hierarchical position of the element in the config (see above).

| Path | Default | Description |
|----- | ------- | ----------- |
| Cron -> Schedule | 0 0 * * * | Cron when to run an export, configuration help can be found here: https://crontab.guru/ |
| Cron -> RunOnce | false | true: run once and stops the program, false: runs every time specified in Cron -> Schedule |
| Worker -> ProtocolEmlBaseInformation | true | Writes one protocol entry for each exported eml file including the unique id and subject |
| Mailboxes | | List of mail accounts to export the junk mails from |
| Mailboxes[] -> ImapHost | | The server hostname to connect to (usually the external name) |
| Mailboxes[] -> ImapPort | | The imap port to connect to |
| Mailboxes[] -> ImapUsername | | The username to login, usually the E-Mail address |
| Mailboxes[] -> ImapPassword | | The password for this account, this needs to be Base64 encoded to also support special characters. Keep in mind this is NOT an encryption of your password. See below how to convert it to Base64 |
| Mailboxes[] -> TargetLocalFolder | | Folder to save the eml files to, already exported eml files get skipped |
| Mailboxes[] -> TargetFilenamePrefix | | If you export multiple accounts you should give each one its own prefix |
| Mailboxes[] -> IgnoreSpamMessages | | true: do not export messages which already got flagges by rspamd as spam, false: export all messages, even already detected spam messages |

## Base64
Using Base64 you can encoded a string to a Base64 format. It is used in the configuration for the password. This gives a better possibility to use passwords with special characters without breaking the json format.

### Where can I convert my password to Base64?
As it is your password I would suggest to use tools on you local computer to convert it. There are some tools in the internet but a lot of them are processing the input on their servers. As I would not suggest sending them your password I can recommend this page: https://gchq.github.io/CyberChef/#recipe=To_Base64('A-Za-z0-9%2B/%3D')&input=SW5zZXJ0IHlvdXIgdGV4dCBoZXJlLCB0aGUgb3V0cHV0IGluIEJhc2U2NCBpcyBzaG93biBiZWxvdy4
Simply replace the text on the upper right and you get the Base64 string below. CyberChef uses only javascript which runs inside your browser - so nothing is sent here.

### Why could it break the json format?
Json has a pre defined format to store information in. If the password contains special characters like double quotes it can easily break the file and is no longer readable by the program. As Base64 only uses only some basic characters (A-Za-z0-9+/=) is is not going to break this format.

### So Base64 is a secure way to store password?
NO DEFINITLY NOT! A base 64 encoded string is in no way secure as it can be decoded by anyone.

As an example, you can see here how a Base64 string is decoded back into a normal string without providing anything: https://gchq.github.io/CyberChef/#recipe=From_Base64('A-Za-z0-9%2B/%3D',true,false)&input=U1c1elpYSjBJSGx2ZFhJZ2RHVjRkQ0JvWlhKbExDQjBhR1VnYjNWMGNIVjBJR2x1SUVKaGMyVTJOQ0JwY3lCemFHOTNiaUJpWld4dmR5ND0

# Mailcow
If you want to use it in combination with mailcow I suggest:
- export the junk from all mail accounts you want to learn rspamd from into a single folder (by using `TargetFilenamePrefix`)
- use the commandline to learn the messages as spam - see the mailcow documentation https://docs.mailcow.email/manual-guides/Rspamd/u-e-rspamd-work-with-spamdata/#learn-spam-or-ham-from-existing-directory