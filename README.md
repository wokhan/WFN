# WFN (Windows Firewall Notifier)

## Project Description
WFN started as a hobby around 2010 and is an "extension" to the embedded Windows firewall, offering real time connections monitoring, connections map, bandwidth usage monitoring...

Its main feature being the Notifier alert itself, which tells you about outgoing connections attempts and allows you to allow or block them, either permanently or temporarily. 

It has been made open source a few years ago.  

Please read the documentation about the features and limitation of WFN. 

Especially note that WFN is **not** a firewall itself!  

## Last update
**2018.03.03 - v2.0 beta 3**  
Head to the [releases page](https://github.com/wokhansoft/WFN/releases/tag/v2.0-beta3) if you are interested in trying the latest beta version, and **keep in mind, this is work in progress! Read the full description page before downloading!**  

## Requirements  
WFN requires Windows 7 or later with Microsoft .NET 4.5.2 or higher. 

Windows Server 2008 or later are not officially supported, but WFN should work fine on them.  

A .Net Core 3.1 version is on the way, removing the requirement for a .NET framework on the user's computer (as it will be embedded with the application itself).

## Features / Screenshots
**Connections listing**  
![](http://wokhan.online.fr/progs/wfn/connections.PNG)

**Real time connections mapping with routes**  
![](http://wokhan.online.fr/progs/wfn/map.PNG)

**Bandwidth monitoring**  
![](http://wokhan.online.fr/progs/wfn/bandwidth.PNG)

**Adapters informations**  
![](http://wokhan.online.fr/progs/wfn/adapters.PNG)

**Windows Firewall status management**  
![](http://wokhan.online.fr/progs/wfn/firewallstatus.PNG)

**Notification popup for unknown outgoing connections (optional)**  
![](http://wokhan.online.fr/progs/wfn/notifier.PNG)

## Additional information
- You can refer to the [roadmap](ROADMAP.md) to have a glance at what's coming next.
- If you want to contribute, please refer to our [contributing guide](CONTRIBUTING.md).
- You can also use the Sponsor button in GitHub if you want to support the project with a donation. 

## Thanks
Thanks to everyone who contributed and donated, and of course to people who will!
