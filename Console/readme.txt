=== Windows Firewall Notifier ===
Author: Khan
Website: http://wokhan.online.fr
Current version: 1.8.0 (released 2013.05.05)

=== Description ===
Windows Firewall Notifier (WFN) extends the default Windows embedded firewall behavior, allowing to handle outgoing connections, and displaying (almost) real-time information about the current connections.

=== Little disclaimer (because I have to) ===
This application is provided "as is". Use at your own risk.
Since I do not want you to panic: the only thing that could happen is that you lose your Internet connection (for instance if "something" prevents Windows Firewall Notifier to be launched properly while all outgoing connection have been blocked as required). In that case, you will be able to restore the previous settings by launching the Windows Firewall Notifier again (it will then "uninstall"). You can also go to the control panel / administration tools / advanced firewall settings to enable back all outgoing connections.
As said: nothing that could really hurt ^_^ 

=== Behavior ===
This application only uses existing Windows features, enabling some of them when first launched:
- Enables the Windows embedded firewall
- Sets the firewall to block both inbound and outbound connections for which no rule exists
- Enables the default Windows firewall inbound connection notification
- Enables the Windows firewall outbound connections logging (disabled by default)
- Creates a scheduled task linked to the Windows firewall event log entries, allowing Windows Firewall Notifier to be launched when needed only

=== Installation ===
The application can be unzipped in the folder of your choice (usually c:\Programs\WindowsFirewallNotifier), knowing that it must not be moved once activated (unless you actually want to remove it). 

When launching version 1.6 for the first time, I advise to remove any svchost.exe firewall rule you may have created using the previous versions since the services management logic has been way improved.

=== How-to ===
Once enabled (after a first launch), the application will show a notification balloon when an application attempts an outgoing connexion while not being allowed to do so. A click on the notification will display a dialog box, allowing the user to:
- temporarily allow the application
- create a rule for the application, so that it will always be able to connect
- block the application once only, after what other notifications may (will) appear
- always block the application (no notification will therefore be displayed afterwards)

By manually relaunching the application executable, a new dialog will appear, allowing to display and modify the exceptions list ("always blocked" applications), to check the connections lastly blocked by Windows (allowing to create rules directly from that point), and, as before, to deactivate WFN.
Starting from v1.2.0, the same window will allow you to check all Windows firewall rules (and to remove the unneeded ones).

NB: If you get a services list starting with "*" when using service detection, it means that WFN was not able to properly detect the service the connection originates from, probably because the corresponding process is not running anymore.
 
=== Versions ===
1.0.1 (2011.08.??)
- first public version.

1.1.0 (2011.09.15): 
- added the exceptions management screen. 
- added the "blocked connections" log.
- modified the dialog box for outgoing connections.

1.2.0 (2011.09.17):
- WFN now uses standard Windows API (it was using some tricky ways to interface with Windows firewall in the previous versions, it is now way better and faster).
- added the "rules" screen to partially handle Windows firewall existing rules.
- other modifications (new minor functionalities).

1.3.0 (2011.09.18):
- major design modifications for the blocked connection dialog (no more notification balloon, as requested by many user).
- multiple blocked connections are now handled properly.
- the dialog will stay visible as long as the user does not make a choice (or leave it open).
- code optimization.

1.3.2 (2011.09.29):
- fixed various bugs (especially with 64bit systems).
- parallel WFN instances management have been deeply improved (= applications trying to connect simultaneously to many different targets when blocked are now handled properly (ex: Dropbox)).
- code optimization.
- added an error log.

1.4.0 (2011.10.09 - beta version only):
- many users asked for it, so services (svchost) management has been improved and only the concerned service will be either allowed or blocked.
- the scheduled task and the event parsing have been improved, resulting in better performance.

1.5.0b (2011.10.29):
- previously added services detection was not working properly and has been deeply modified.
- added an option to skip service detection.
- major code optimization (partly changed the application logic to reduce CPU usage).
- deep design modifications and ergonomics improvements.
- added some options as asked by some users.

1.5.5b (2011.11.04):
- improved (again) the service detection (using Windows API).
- added a new connections panel to track active connections (almost a realtime netstat).
- added the WSH (Windows Service Hardening) hidden firewall rules to the rules list.
- automatically adds rules for Windows Update (remote ports 80 / 443), BITS (same) and CryptSvc (port 80) when activating the notifications, since they are not properly detected while being supposedly required.
- and some other things I probably forgot...

1.6.0 (2011.11.06):
- added IPV6 support for the active connections list.
- fixed some minor bug.

1.6.1 (2011.11.07):
- fixed a bug with services name retrieval.

1.6.2 (2011.11.09):
- fixed the previous fix as it was still not working properly (I know, shame on me...).
NB: I advise to remove any global svchost.exe rule created with the previous version to allow a proper service redetection.

1.7.0 (2012.04.28):
- improved the application design (and now uses the free Blueberry Basic icon set by icojam - http://www.icojam.com).
- changed the logic to allow any user to display the notifications, even in a multi user environment, with three different executable (Notifier.exe, RuleManager.exe and Console.exe).
NB: when using the "enable for all" option, Windows UAC will display a notification when creating a rule (even for admin accounts). This is way more secure than the way it was previously working.
The WFN Console still requires an admin account as well.

1.8.0 (2013.05.05 - Bug fixes & Windows 8 compatibility update):
- modified the task creation for the current user so that it works properly on Windows 8.
- modified the task creation for accentuated user accounts names.
- fixed rules name/description resolution.
- minor improvements.
- added a simple update button in the WFN console (only shown when an update is available).
- fixed the logical application path resolution (could lead to improper rules creation in some specific cases).