using Microsoft.Win32.SafeHandles;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Processes;

public static partial class ProcessHelper
{
    public static void RunElevated(string process, string? args = null)
    {
        ProcessStartInfo proc = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = process,
            Verb = "runas",
            Arguments = args
        };

        Process.Start(proc);
    }

    public static (string Name, string Path, string CommandLine)? GetProcessOwnerWMI(uint owningPid)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT ProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process WHERE ProcessId = {owningPid}");
        using var r = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

        if (r is null)
        {
            return null;
        }

        return ((string)r["Name"], (string)r["ExecutablePath"], (string)r["CommandLine"]);
    }

    //public static IEnumerable<string>? GetAllServices(uint pid)
    //{
    //    IntPtr hServiceManager = NativeMethods.OpenSCManagerW(null, null, (uint)(NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT | NativeMethods.SCM_ACCESS.SC_MANAGER_ENUMERATE_SERVICE));
    //    if (hServiceManager == IntPtr.Zero)
    //    {
    //        LogHelper.Warning("Unable to open SCManager.");
    //        return Array.Empty<string>();
    //    }
    //    try
    //    {
    //        uint dwBufSize = 0;
    //        uint dwBufNeed = 0;
    //        uint ServicesReturned = 0;
    //        uint ResumeHandle = 0;

    //        var resp = NativeMethods.EnumServicesStatusExW(hServiceManager, (int)NativeMethods.SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)NativeMethods.SERVICE_TYPES.SERVICE_WIN32, (int)NativeMethods.SERVICE_STATE.SERVICE_ACTIVE, IntPtr.Zero, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
    //        if (resp != 0)
    //        {
    //            LogHelper.Warning("Unexpected result from call to EnumServicesStatusEx.");
    //            return Array.Empty<string>();
    //        }

    //        if (Marshal.GetLastWin32Error() != NativeMethods.ERROR_MORE_DATA)
    //        {
    //            LogHelper.Warning("Unable to retrieve data from SCManager.");
    //            return Array.Empty<string>();
    //        }

    //        List<string> result = new List<string>();

    //        bool IsThereMore = true;
    //        while (IsThereMore)
    //        {
    //            IsThereMore = false;
    //            dwBufSize = dwBufNeed;
    //            dwBufNeed = 0;
    //            IntPtr buffer = Marshal.AllocHGlobal((int)dwBufSize);
    //            try
    //            {
    //                resp = NativeMethods.EnumServicesStatusExW(hServiceManager, (int)NativeMethods.SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO, (int)NativeMethods.SERVICE_TYPES.SERVICE_WIN32, (int)NativeMethods.SERVICE_STATE.SERVICE_ACTIVE, buffer, dwBufSize, out dwBufNeed, out ServicesReturned, ref ResumeHandle, null);
    //                if (resp == 0)
    //                {
    //                    uint resp2 = (uint)Marshal.GetLastWin32Error();
    //                    if (resp2 == NativeMethods.ERROR_MORE_DATA)
    //                    {
    //                        IsThereMore = true;
    //                    }
    //                    else
    //                    {
    //                        LogHelper.Error("Unable to retrieve data from SCManager.", new Win32Exception((int)resp2));
    //                        return null;
    //                    }
    //                }
    //                for (uint i = 0; i < ServicesReturned; i++)
    //                {
    //                    IntPtr buffer2;
    //                    if (Environment.Is64BitProcess)
    //                    {
    //                        //8 byte packing on 64 bit OSes.
    //                        buffer2 = IntPtr.Add(buffer, (int)i * (NativeMethods.ENUM_SERVICE_STATUS_PROCESS.SizeOf + 4));
    //                    }
    //                    else
    //                    {
    //                        buffer2 = IntPtr.Add(buffer, (int)i * NativeMethods.ENUM_SERVICE_STATUS_PROCESS.SizeOf);
    //                    }
    //                    NativeMethods.ENUM_SERVICE_STATUS_PROCESS service = Marshal.PtrToStructure<NativeMethods.ENUM_SERVICE_STATUS_PROCESS>(buffer2);
    //                    if (pid == service.ServiceStatus.dwProcessId)
    //                    {
    //                        //We have found one of the services we're looking for!
    //                        result.Add(service.lpServiceName);
    //                    }
    //                }
    //            }
    //            finally
    //            {
    //                Marshal.FreeHGlobal(buffer);
    //            }
    //        }

    //        return result;
    //    }
    //    finally
    //    {
    //        NativeMethods.CloseServiceHandle(hServiceManager);
    //    }
    //}

    /// <summary>
    /// Retrieve information about all services by pid
    /// </summary>
    /// <returns></returns>
    public static Dictionary<uint, ServiceInfoResult> GetAllServicesByPidWMI()
    {
        // use WMI "Win32_Service" query to get service names by pid
        // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-service
        Dictionary<uint, ServiceInfoResult> dict = [];
        using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name, DisplayName, PathName FROM Win32_Service WHERE ProcessId != 0"))
        {
            using var results = searcher.Get();
            foreach (var r in results)
            {
                //Console.WriteLine($"{r["processId"]} {r["Name"]}");
                var pid = (uint)r["ProcessId"];
                if (pid > 0 && !dict.ContainsKey(pid))
                {
                    dict.Add(pid, new ServiceInfoResult(pid, (string)r["Name"], (string)r["DisplayName"], (string)r["PathName"]));
                }
            }
        }
        return dict;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    /// <returns></returns>
    //private static string GetServiceDesc(string service)
    //{
    //    string ret;
    //    try
    //    {
    //        using (var sc = new ServiceController(service))
    //        {
    //            ret = sc.DisplayName;
    //        }

    //        return ret;
    //    }
    //    //There's an undocumented feature/bug where instead of ArgumentException, an InvalidOperationException is thrown.
    //    catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
    //    {
    //        LogHelper.Debug("Couldn't get description for service: " + service);
    //        return String.Empty;
    //    }
    //}



    public unsafe static string GetLocalUserOwner(uint pid)
    {
        //Based on: https://bytes.com/topic/c-sharp/answers/225065-how-call-win32-native-api-gettokeninformation-using-c
        var hProcess = NativeMethods.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, pid);
        if (hProcess is null)
        {
            LogHelper.Warning($"Unable to retrieve process local user owner: process pid={pid} cannot be found!");
            return String.Empty;
        }

        SafeFileHandle hToken = new SafeFileHandle();
        TOKEN_USER hTokenInformation;
        try
        {
            if (!NativeMethods.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_QUERY, out hToken))
            {
                LogHelper.Warning("Unable to retrieve process local user owner: process pid={pid} cannot be opened!");
                return String.Empty;
            }

            //TODO: Wait... isn't a negation missing here?!
            if (NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, null, 0, out var dwBufSize))
            {
                LogHelper.Warning("Unexpected result from call to GetTokenInformation.");
                return String.Empty;
            }

            hTokenInformation = new TOKEN_USER();
            if (!NativeMethods.GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, &hTokenInformation, dwBufSize, out dwBufSize))
            {
                LogHelper.Warning("Unable to retrieve process local user owner: token cannot be opened!");
                return String.Empty;
            }

            PWSTR SID = new PWSTR();
            if (!NativeMethods.ConvertSidToStringSid(hTokenInformation.User.Sid, &SID))
            {
                LogHelper.Warning("Unable to retrieve process local user owner: SID cannot be converted!");
                return String.Empty;
            }

            return SID.ToString();
        }
        finally
        {
            hToken?.Close();
            hProcess?.Close();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="p_2"></param>
    /// <returns></returns>
    public static bool GetProcessFeedback(string cmd, string args, bool runas = false, bool dontwait = false)
    {
        try
        {
            ProcessStartInfo psiTaskTest = new ProcessStartInfo(cmd, args) { CreateNoWindow = true };

            if (runas)
            {
                psiTaskTest.Verb = "runas";
            }
            else
            {
                psiTaskTest.UseShellExecute = false;
            }

            Process procTaskTest = Process.Start(psiTaskTest);
            if (dontwait)
            {
                procTaskTest.WaitForExit(100);
                if (procTaskTest.HasExited)
                {
                    return procTaskTest.ExitCode == 0;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                procTaskTest.WaitForExit();
            }

            return procTaskTest.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///  Turns command line parameters into a dictionary to ease values retrieval
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    //public static Dictionary<string, string> ParseParameters(IList<string> args)
    //{
    //    Dictionary<string, string>? ret = null;
    //    String key = String.Empty;
    //    try
    //    {
    //        ret = new Dictionary<string, string>(args.Count / 2);
    //        for (int i = args.Count % 2; i < args.Count; i += 2)
    //        {
    //            key = args[i].TrimStart('-');
    //            ret.Add(key, args[i + 1]);
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        LogHelper.Error("Unable to parse the parameters: key = " + key + " argv = " + String.Join(" ", args), e);
    //    }

    //    return ret ?? new Dictionary<string, string>();
    //}

    /// <summary>
    /// Get the command-line of a running process id.<br>Use parseCommandLine to parse it into list of arguments</br>
    /// </summary>
    /// <param name="processId"></param>
    /// <returns>command-line or null</returns>
    public static string? GetCommandLineFromProcessWMI(uint processId)
    {
        try
        {
            using ManagementObjectSearcher clSearcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            return String.Join(String.Empty, clSearcher.Get().Cast<ManagementObject>().Select(mObj => (string)mObj["CommandLine"]));
        }
        catch (Exception e)
        {
            LogHelper.Error($"Unable to get command-line from processId: {processId} - is process running?", e);
        }
        return null;
    }
    /// <summary>
    /// Parses a complete command-line with arguments provided as a string (support commands spaces and quotes).
    /// <para>Special keys in dictionary
    /// <br>key=@command  contains the command itself</br>
    /// <br>key=@arg[x] for args wihtout argname</br>
    /// </para>
    /// </summary>
    /// 
    /// <param name="cmdLine">command-line to parse e.g. "\"c:\\program files\\svchost.exe\" -k -s svcName -t \"some text\""</param>
    /// <returns>Dictionary with key-value pairs.<para>
    /// key=@command contains the command itself</para>
    /// <para>key=@arg[x] for args without key</para>
    /// <para>[-argname|/aname]</para>
    /// </returns>
    //public static Dictionary<string, string?> ParseCommandLineArgs(string cmdLine)
    //{
    //    // https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
    //    // Fiddle link (regex): https://dotnetfiddle.net/PU7kXD

    //    string regEx = @"\G(""((""""|[^""])+)""|(\S+)) *";
    //    MatchCollection matches = Regex.Matches(cmdLine, regEx);
    //    List<string> args = matches.Cast<Match>().Select(m => Regex.Replace(m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value, @"""""", @"""")).ToList();
    //    return ParseCommandLineArgsToDict(args);
    //}

    /// <summary>
    /// Creates a dictionary from a command-line arguments list.
    /// <para>Special keys in dictionary
    /// <br>key=@command  contains the command itself from the first element in the list</br>
    /// <br>key=@arg[x] for args wihtout argname</br>
    /// </para>
    /// </summary>
    /// 
    //public static Dictionary<string, string?> ParseCommandLineArgsToDict(List<String> args)
    //{
    //    // Fiddle link to test it: https://dotnetfiddle.net/PU7kXD
    //    Dictionary<string, string?> dict = new Dictionary<string, string?>(args.Count);
    //    for (int i = 0; i < args.Count; i++)
    //    {
    //        string key;
    //        string? val;
    //        if (args[i].StartsWith("-") || args[i].StartsWith("/"))
    //        {
    //            key = args[i];
    //            if ((i + 1) < args.Count && !args[i + 1].StartsWith("-") && !args[i + 1].StartsWith("/"))
    //            {
    //                val = args[i + 1];
    //                i++;
    //            }
    //            else
    //            {
    //                val = null;
    //            }
    //        }
    //        else
    //        {
    //            // key=@command@ or argX 
    //            key = (i == 0) ? "@command" : "@arg" + i;
    //            val = args[i];
    //        }
    //        dict.Add(key, val);
    //    }

    //    return dict;
    //}

    /// <summary>
    /// Finds the process by name and sets the main window to the foreground.
    /// Note: Process name is the cli executable excluding ".exe" e.g. "WFN" instead of "WFN.exe". 
    /// </summary>
    /// <param name="processName">Known process from enum</param>
    public static void StartOrRestoreToForeground(ProcessNames processName)
    {
        var bProcess = Process.GetProcessesByName(processName.ProcessName).FirstOrDefault();
        // check if the process is running
        if (bProcess is not null)
        {
            // check if the window is hidden / minimized
            if (bProcess.MainWindowHandle == IntPtr.Zero)
            {
                // the window is hidden so try to restore it before setting focus.
                //TODO: this cannot work, obviously. The handle isn't the right one.
                //NativeMethods.ShowWindow((HWND)bProcess.Handle, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
            }

            // set user the focus to the window
            _ = NativeMethods.SetForegroundWindow((HWND)bProcess.MainWindowHandle);
        }
        else
        {
            _ = Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, processName.FileName));
        }
    }


    /// <summary>
    /// Opens Windows explorer and selects the file targeted by "flepath"
    /// </summary>
    /// <param name="filepath">Full path to the file to select</param>
    public static void BrowseToFile(string? filepath)
    {
        if (filepath is null)
        {
            return;
        }

        StartShellExecutable(ProcessNames.Explorer.FileName, $"/select,{filepath}", true);
    }

    /// <summary>
    /// Opens a folder in Windows explorer.
    /// </summary>
    /// <param name="folderPath">Path to the folder</param>
    public static void OpenFolder(string? folderPath)
    {
        if (folderPath is null)
        {
            return;
        }

        StartShellExecutable(ProcessNames.Explorer.FileName, folderPath, true);
    }


    /// <summary>
    /// Starts a default shell executable with arguments and optional message box in case of failure.
    /// </summary>
    /// <param name="executable">Path to the executable to launche</param>
    /// <param name="args">Arguments to pass to the executable</param>
    /// <param name="showMessageBox">Shows a message box if an error occurs. Not really user friendly but straightforward</param>
    public static void StartShellExecutable(string executable, string? args = "", bool showMessageBox = false)
    {
        try
        {
            LogHelper.Debug($"Starting shell executable: {executable}, args: {args}");
            Process.Start(new ProcessStartInfo(executable, args ?? "") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LogHelper.Error($"{ex.Message}: {executable} {args}", ex);
            if (showMessageBox)
            {
                MessageBox.Show($"Cannot start shell program: {executable}, Message: {ex.Message}");
            }
        }
    }
}

