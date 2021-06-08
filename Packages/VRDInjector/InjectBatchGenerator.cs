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
        var projectRootPath = @Path.GetDirectoryName(Application.dataPath);
        Debug.Log($"[Inject Batch Generator] {projectRootPath}");
        var unityExePath = EditorApplication.applicationPath.Replace("/", "\\");
        //Retrieve command-line arguments for this process
        string[] args = Environment.GetCommandLineArgs();
        string injectionValidationArg = "-injectedTrue";
        bool injectValidated = false;
        foreach (var t in args)
        {
            if (t.Contains(injectionValidationArg))
            {
                injectValidated = true;
                Debug.Log("[Inject Batch Generator] Virtual Desktop streamer injected into project");
            }
        }
        if (!injectValidated)
        {
            //Check if commandline args contains injector validation argument
            string preQuote = "\\\"\\\\\"\"\"\\\"";
            string postQuote = "\\\\\"\\\"\"\"\\\"";
            string injectionString = injectionValidationArg;
            for (int i = 1; i < args.Length; i++)
            {
                if (!string.IsNullOrEmpty(args[i]))
                {
                    if (args[i][0] == '-')
                    {
                        injectionString += (" " + args[i]);
                    }
                    else if (!args[i].Contains(" "))
                    {
                        injectionString += (" " + args[i]);
                    }
                    else
                    {
                        injectionString += (" " + preQuote + args[i] + postQuote);
                    }
                }
            }
            Debug.Log($"[Inject Batch Generator] {injectionString}");
            if (EditorApplication.timeSinceStartup > 30)
            {
                EditorUtility.DisplayDialog(
                    "This editor instance isn't injected by Virtual Desktop Streamer",
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
            p.WaitForExit();
            var pid = Process.GetCurrentProcess().Id;
            var vStreamerPath = p.StandardOutput.ReadToEnd().Replace("/", "\\").Replace(Environment.NewLine, "");
            Debug.Log("[Inject Batch Generator] " + vStreamerPath);
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = vStreamerPath,
                FileName = "powershell.exe",
                Arguments =
                    $"&(Get-Process | Where-Object {{$_.Id -eq {pid.ToString()}}}).WaitForExit();\".\\VirtualDesktop.Streamer.exe\" \\\"{unityExePath}\\\" {injectionString};",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            new Thread(() =>
            {
                Debug.Log("[Inject Batch Generator] Injecting shell command using detached child process");
                Thread.CurrentThread.IsBackground = true;
                Process.Start(startInfo);
            }).Start();
            EditorApplication.Exit(0);
        }
    }
}