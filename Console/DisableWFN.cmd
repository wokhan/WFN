@echo off
echo This script allows to disable WFN when the exe does not work properly, so that it can be safely removed.
echo Please close this window if you launched this script by mistake, or press any key to continue.
echo (You will of course be able to reinstall WFN anytime).
pause
echo.
echo Allowing all outgoing connections (reverting to Windows default behavior)...
netsh.exe advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound
echo Done.
echo.
echo Removing Windows Firewall Notifier scheduled task...
schtasks.exe /Delete /TN WindowsFirewallNotifierTask /F
echo Done.
echo.
echo Disabling outgoing blocked connections log...
auditpol.exe /set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:disable
echo Done.
echo.
echo Windows Firewall Notifier has been disabled.
echo You can now delete the application files, or relaunch the executable to restart the installation process.
echo.
pause