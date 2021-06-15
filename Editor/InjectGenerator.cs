using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static class InjectBatchGenerator
{
    static InjectBatchGenerator()
    {
        var editorIsLoading = EditorWindow.HasOpenInstances<EditorWindow>();
        if (!editorIsLoading) CheckVd();
        
        var projectRootPath = @Path.GetDirectoryName(Application.dataPath);
        _log($"Found project root at: {projectRootPath}");
        var unityExePath = EditorApplication.applicationPath.Replace("/", "\\");
        //Retrieve command-line arguments for this process
        var args = Environment.GetCommandLineArgs();
        
        const string injectionValidationArg = "-injectedTrue";
        var injectValidated = false;
        foreach (var t in args)
        {
            if (!t.Contains(injectionValidationArg)) continue;
            injectValidated = true;
            _log("Project was launched using Virtual Desktop injection");
        }

        if (injectValidated) return;
        //Check if commandline args contains injector validation argument
        const string preQuote = "\\\"\\\\\"\"\"\\\"";
        const string postQuote = "\\\\\"\\\"\"\"\\\"";
        var injectionString = injectionValidationArg;
        for (int i = 1; i < args.Length; i++)
        {
            if (string.IsNullOrEmpty(args[i])) continue;
            if (args[i][0] == '-') injectionString += (" " + args[i]);
            else if (!args[i].Contains(" ")) injectionString += (" " + args[i]);
            else injectionString += (" " + preQuote + args[i] + postQuote);
        }
        _log($"Generated escaped Unity injection string: {injectionString}");
        if (!editorIsLoading)
        {
            _log($"Unity Editor was running at the time of attempted injection: {Math.Round(EditorApplication.timeSinceStartup)} seconds since startup");
            EditorUtility.DisplayDialog(
                "This editor instance wasn't launched by Virtual Desktop Streamer",
                "Close the project, (re)start Virtual Desktop streamer, and then launch the project again.",
                "OK",
                "");
        }

        var p = Process.Start(new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments =
                @"(get-item 'REGISTRY::HKEY_LOCAL_MACHINE\SOFTWARE\Virtual Desktop, Inc.\Virtual Desktop Streamer').GetValue('Path')",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
        var registryVdStreamerPath = "";
        if (p != null)
        {
            p.WaitForExit();
            registryVdStreamerPath = p.StandardOutput.ReadToEnd().Replace("/", "\\").Replace(Environment.NewLine, "");
        }

        var pid = Process.GetCurrentProcess().Id;
        var userdefinedStreamerPath = $"{EditorPrefs.GetString("AVDU VDS path", "").Replace("/","\\")}";
        if (!userdefinedStreamerPath.EndsWith("\\"))
        {
            userdefinedStreamerPath += "\\";
        }
        string finalStreamerPath;
        _log($"User-specified virtual desktop streamer folder path: {userdefinedStreamerPath}");
        if (String.IsNullOrEmpty(registryVdStreamerPath.Trim()))
        {
            _log("Virtual desktop streamer folder path wasn't found in the registry");
            finalStreamerPath = userdefinedStreamerPath;
        }
        else if (!registryVdStreamerPath.Trim().Equals(userdefinedStreamerPath.Trim()))
        {
            if (string.IsNullOrEmpty(userdefinedStreamerPath.Trim()))
            {
                finalStreamerPath = registryVdStreamerPath;
                EditorPrefs.SetString("AVDU VDS path", finalStreamerPath);
            }
            else
            {
                finalStreamerPath = userdefinedStreamerPath;
                _log("Picking user-specified Virtual Desktop Streamer path");
            }
        }
        else
        {
            _log($"Picking Virtual desktop streamer folder path fetched from registry: {registryVdStreamerPath}");
            finalStreamerPath = registryVdStreamerPath;
        }

        if (!new FileInfo($"{finalStreamerPath}VirtualDesktop.Streamer.exe").Exists)
        {
            _log($"Invalid VD streamer path found\"{finalStreamerPath}VirtualDesktop.Streamer.exe\"\nTwo fixes: Make sure the correct path is specified in project settings, else reinstall Virtual Desktop",true);
            return;
        }
        EditorPrefs.SetString("AVDU streamerPath",finalStreamerPath);
        _log($"Virtual Desktop Streamer path is valid and the exe exists");
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = finalStreamerPath,
            FileName = "powershell.exe",
            Arguments =
                $"$(Get-Process | Where-Object {{$_.Id -eq {pid.ToString()}}}).WaitForExit();if(-Not (Get-Process VirtualDesktop.Streamer -ErrorAction SilentlyContinue)) {{\".\\VirtualDesktop.Streamer.exe\"}};Start-Sleep -s 1;\".\\VirtualDesktop.Streamer.exe\" \\\"{unityExePath}\\\" {injectionString};",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            Process.Start(startInfo);
        }).Start();
        _log("Detached child process successfully launched, attempting to close Unity Editor");
        EditorApplication.Exit(0);
    }

    private static void _log(string input,bool isError = false)
    {
        if (isError)
        {
            Debug.LogError($"[Inject Batch Generator] {input}");
        }
        else if (EditorPrefs.GetBool("AVDU Debug",false))
        {
            Debug.Log($"[Inject Batch Generator] {input}");
        }
    }

    private static void CheckVd()
    {
        var revive = EditorPrefs.GetBool("AVDU reviveVD");
        if (!revive || !EditorApplication.isPlayingOrWillChangePlaymode) return;
        var sPath = EditorPrefs.GetString("AVDU streamerPath", "");
        _log($"Checking if VD Streamer in folder {sPath} is running, restarting if not");
        if (!new FileInfo($"{sPath}VirtualDesktop.Streamer.exe").Exists)
        {
            _log($"incorrect VD Streamer executable folder path found: {sPath}",true);
            return;
        }
        
        var reviverProcess = Process.Start(new ProcessStartInfo()
        {
            WorkingDirectory = sPath,
            FileName = "powershell.exe",
            Arguments =$"if((Get-Process VirtualDesktop.Streamer -ErrorAction SilentlyContinue)) {{Write-Output \"True\"}} else {{Write-Output \"False\"}}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
        var vdRunning = true;
        if (reviverProcess != null)
        {
            vdRunning = Boolean.Parse(reviverProcess.StandardOutput.ReadToEnd());
        }
        _log($"Is VD Streamer process located in folder {sPath} running: {vdRunning}");
        if (vdRunning) return;
        EditorApplication.ExitPlaymode();
        Process.Start(new ProcessStartInfo()
        {
            WorkingDirectory = sPath,
            FileName = "powershell.exe",
            Arguments = $"\".\\VirtualDesktop.Streamer.exe\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
        _log("Virtual Desktop Streamer process is no longer running, attempting to restart it. Don't start play-mode until your VR headset reconnects",true);
    }
}