using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        bool injectValidated = false;

        //Check if commandline args contains injector validation argument
        foreach (string s in args)
        {
            if (s.Contains(injectionValidationArg))
            {
                injectValidated = true;
                Debug.Log("[Inject Batch Generator] Project started through VD Streamer");
            }
        }
        if (!injectValidated)
        {
            Debug.Log("[Inject Batch Generator] Time of injection: "+Time.time);
            var p = Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = @"(get-item 'REGISTRY::HKEY_LOCAL_MACHINE\SOFTWARE\Virtual Desktop, Inc.\Virtual Desktop Streamer').GetValue('Path')",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            p.WaitForExit();
            var PID = Process.GetCurrentProcess().Id;
            var vStreamerPath = p.StandardOutput.ReadToEnd().Replace("/", "\\").Replace(Environment.NewLine, "");
            Debug.Log("[Inject Batch Generator] "+vStreamerPath);
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = vStreamerPath,
                FileName = "powershell.exe",
                Arguments = $"&(Get-Process | Where-Object {{$_.Id -eq {PID.ToString()}}}).WaitForExit();\".\\VirtualDesktop.Streamer.exe\" \\\"{unityExePath}\\\" {injectionValidationArg} -useHub -hubIPC -cloudEnvironment production -projectPath \\\"\\\\\"\"\"\\\"{projectRootPath}\\\\\"\\\"\"\"\\\";",
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