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
    private static string debugName = "[Inject Batch Generator]";
    static InjectBatchGenerator()
    {
        var debug = EditorPrefs.GetBool("AVDU Debug",false);
        var projectRootPath = @Path.GetDirectoryName(Application.dataPath);
        if (debug)
        {
            Debug.Log($"{debugName} Found project root at: {projectRootPath}");
        }
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
                if (debug)
                {
                    Debug.Log($"{debugName} Project was launched using Virtual Desktop injection");
                }
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

            if (debug)
            {
                Debug.Log($"{debugName} Generated escaped Unity injection string: {injectionString}");
            }
            if (EditorApplication.timeSinceStartup > 45)
            {
                if (debug)
                {
                    Debug.Log($"{debugName} Unity Editor was running at the time of attempted injection: {EditorApplication.timeSinceStartup}");
                }
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
            p.WaitForExit();
            var pid = Process.GetCurrentProcess().Id;

            var vStreamerPath = p.StandardOutput.ReadToEnd().Replace("/", "\\").Replace(Environment.NewLine, "");
            if (vStreamerPath.Contains("VirtualDesktop.Streamer.exe") && new FileInfo(vStreamerPath).Exists)
            {
                
                EditorPrefs.SetString("AVDU VDS path",vStreamerPath);
                if (debug)
                {
                    Debug.Log($"{debugName} VirtualDesktop.Streamer.exe path fetched from registry: {vStreamerPath}");
                    var startInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = vStreamerPath,
                        FileName = "powershell.exe",
                        Arguments =
                            $"&(Get-Process | Where-Object {{$_.Id -eq {pid.ToString()}}}).WaitForExit();start \"\" \".\\VirtualDesktop.Streamer.exe;\".\\VirtualDesktop.Streamer.exe\" \\\"{unityExePath}\\\" {injectionString};",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        Process.Start(startInfo);
                    }).Start();
                    if (debug)
                    {
                        Debug.Log($"{debugName} Detached child process successfully launched, attempting to close Unity Editor");
                    }
                    EditorApplication.Exit(0);
                }
            }
        }
    }
}