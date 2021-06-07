using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static class InjectBatchGenerator
{

    // static InjectBatchGenerator()

    static InjectBatchGenerator()
    {
        var projectRootPath = @Path.GetDirectoryName(Application.dataPath);
        Debug.Log($"[Inject Batch Generator] {projectRootPath}");
        var unityExePath = EditorApplication.applicationPath.Replace("/", "\\");
        //Retrieve command-line arguments for this process
        string[] args = Environment.GetCommandLineArgs();
        string injectionValidationArg = "-injectedTrue";
        string[] injectionArgs = {"-accept-apiupdate", injectionValidationArg, "-useHub", "-hubIPC", "-projectPath"};
        bool injectValidated = false;
        //Check if commandline args contains injector validation argument
        for(int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains(injectionValidationArg))
            {
                injectValidated = true;

                Debug.Log("[Inject Batch Generator] Project started through VD Streamer");
            }
            if (injectionArgs.Contains(args[i]) && !string.IsNullOrEmpty(args[i]))
            {
                if (args[i].Contains("-projectPath"))
                {
                    args[i + 1] = "";
                }
                args[i] = "";
                
            }
        }

        string injectionString = string.Join(" ", args);
        Debug.Log($"[Inject Batch Generator] {injectionString}");
        if (!injectValidated)
        {
            if (Time.time > 30)
            {
                EditorUtility.DisplayDialog(
                    "This Unity Editor isn't injected by Virtual Desktop Streamer",
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
            });
            p.WaitForExit();
            var PID = Process.GetCurrentProcess().Id;
            var vStreamerPath = p.StandardOutput.ReadToEnd().Replace("/", "\\").Replace(Environment.NewLine, "");
            Debug.Log("[Inject Batch Generator] " + vStreamerPath);
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = vStreamerPath,
                FileName = "powershell.exe",
                Arguments =
                    $"&(Get-Process | Where-Object {{$_.Id -eq {PID.ToString()}}}).WaitForExit();\".\\VirtualDesktop.Streamer.exe\" \\\"{unityExePath}\\\" -accept-apiupdate {injectionValidationArg} -projectPath \\\"\\\\\"\"\"\\\"{projectRootPath}\\\\\"\\\"\"\"\\\";",
                UseShellExecute = false,
                RedirectStandardOutput = true,
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