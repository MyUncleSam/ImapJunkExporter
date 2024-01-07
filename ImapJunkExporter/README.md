# What is ImapJunkExporter
This program exports the junk folder of a given mail server.

# Why
I am using mailcow with an encrypted mail storage. Using it encrypted is secure but removes the possibility of auto learning spam mails in rspamd. So I wrote this to:
- export the junk folder to a local folder
- run a console command to learn all exported mails as junk

# How it works

## Runmode
You can let this tool run once or by defining a cron job.

## Configuration
You can configure the tool as needed using the `appsettings.json`. For more details see the `Configuration` part in this readme.

## Workflow
1. Iterate through all provided mailboxes
2. Check if Junk subfolder for learned mails is existing, create it if not (and subscribe to it)
3. Check all mails in this folder
  1. If the mail was already detected by the spam system ignore it (continue with next mail)
  2. Export it to the given filesystem path, if the given file already exists the mail gets ignored (but still moved to the `Learned` subfolder)
  3. Move the mail to the `Learned` subfolder, this prevents a reprocessing

## Export filename
The exported filename is always `{UniqueId}.eml`. So if the UniqueId is `4711` the export name would be `4711.eml`.
If you export multiple mails to the same folder you should use the `TargetFilenamePrefix`. Using it result into `{TargetFilenamePrefix}{UniqueId}.eml`. So if you add the prefix `Test_` to the exmple above it would result into `Test_4711.eml`.

# Configuration
You need to configure the mailboxes in the `appsettings.json`.

## Example
```
{
  "Schedule": {
    "Schedule": "0 0/15 * ? * * *",
    "RunOnce": false
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
      "ImapLearnedFolderName": "Learned",
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
      "ImapLearnedFolderName": "Learned",
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
| Schedule -> Cron | 0 0/15 * ? * * * | Quartz cron expression when to run an export, configuration help can be found here: https://freeformatter.com/cron-expression-generator-quartz.html |
| Schedule -> RunOnce | false | true: run once and stops the program, false: runs every time specified in Cron -> Schedule |
| Worker -> ProtocolEmlBaseInformation | true | Writes one protocol entry for each exported eml file including the unique id and subject |
| Mailboxes | | List of mail accounts to export the junk mails from |
| Mailboxes[] -> ImapHost | | The server hostname to connect to (usually the external name) |
| Mailboxes[] -> ImapPort | | The imap port to connect to |
| Mailboxes[] -> ImapUsername | | The username to login, usually the E-Mail address |
| Mailboxes[] -> ImapPassword | | The password for this account, this needs to be Base64 encoded to also support special characters. Keep in mind this is NOT an encryption of your password. See below how to convert it to Base64 |
| Mailboxes[] -> ImapLearnedFolderName | | Name of the Junk subfolder, all exported mails are going to be moved there to avoid double processing. Do not use special characters. |
| Mailboxes[] -> TargetLocalFolder | | Folder to save the eml files to, already exported eml files get skipped |
| Mailboxes[] -> TargetFilenamePrefix | | If you export multiple accounts you should give each one its own prefix |
| Mailboxes[] -> IgnoreSpamMessages | | true: do not export messages which already got flagges by rspamd as spam, false: export all messages, even already detected spam messages |

## Environment variables
You can also specify configurations using environment variables. This can be useful to run the program with different settings. The program always reads the configuration in the following order. Last one setting a variable wins:
1. appsettings.json
2. environment variables.

So using environment variables will always win.

Examples:
| Environment key=value pair | Description |
| -------------------------- | ----------- |
| Schedule__Cron=*/10 * * ? * * * | changes the cron interval to every 10 seconds |
| Schedule__RunOnce=true | changes from cron to run only once |
| Mailboxes:0:ImapHost | changes the ImapHost for the first element in the mailboxes list |
(Instead of `__` you can also use `:` like `Schedule__Cron` can also be written `Schedule:Cron`.)

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

Example bash script:
This example does the following steps:
1. iterate through all files in `/opt/spam_to_learn` (change to the folder where your exported mails are in)
2. let rspamd learns those messages as spam
3. deletes the eml file which got learned

```
#!/bin/bash
cd /opt/mailcow-dockerized

for file in /opt/imap_junk_exporter/junk/*;
do
        docker exec -i $(docker compose ps -q rspamd-mailcow) rspamc learn_spam < "$file"
        rm "$file"
done

cd -
```

# Logging
If you want or need to change the logging you simply need to change the file `/app/build/NLog.config`. I highly suggest to not edit it inside the container but to overwrite it with a linked file (like `appsettings.json`).

# Docker

## Image
`ruepp/imapjunkexporter`

## Environment variables
Not needed by default but see configuration above. For the correct usage of timezones I suggest setting the `TZ` environment parameter.

## Volumes
| Volume | Type | Description |
| ------ | ---- | ----------- |
| /app/build/logging | Folder | log files |
| /app/build/appsettings.json | File | configuration file |
| /app/build/NLog.config | File | NLog configuration for logging |

## Example docker-compose.yaml
```
version: '3'
services:
  junkexporter:
    image: ruepp/imapjunkexporter
    container_name: junkexporter
    restart: unless-stopped
    volumes:
      - ./log:/app/build/logging
      - ./appsettings.json:/app/build/appsettings.json
      # - ./NLog.config:/app/build/NLog.config
      - ./junk:/export
    environment:
      TZ: Europe/Berlin
      # CRON__RUNONCE: false
```

## Example docker run
This example runs the container and removes it directly after it finished.
```
docker run --rm -v ./log:/app/build/logging -v ./appsettings.json:/app/build/appsettings.json -e TZ=Europe/Berlin -e Schedule__RunOnce=true ruepp/imapjunkexporter
```