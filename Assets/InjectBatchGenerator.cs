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
        var shellScriptFileName = "VDStreamerInjector.ps1";
        var shellScriptPath = projectRootPath + "\\" + shellScriptFileName;
        var unityExePath = EditorApplication.applicationPath.Replace("/","\\");
        
        //Retrieve command-line arguments for this process
        string[] args = Environment.GetCommandLineArgs();
        string injectionValidationArg = "-injectedTrue";
        bool injectValidated = false;
        
        //Check if commandline args contains injector validation argument
        foreach(string s in args) { 
            if (s.Contains(injectionValidationArg))
            {
                injectValidated = true;
                Debug.Log("Project started through VD Streamer");
            }
        }
        if (!injectValidated)
        {
            
            if (File.Exists(shellScriptPath))
            {
                File.Delete(shellScriptPath);
            }
            using (FileStream fs = File.Create(shellScriptPath))
            {
                fs.Close();
            }
            var p = Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = @"(get-item 'REGISTRY::HKEY_LOCAL_MACHINE\SOFTWARE\Virtual Desktop, Inc.\Virtual Desktop Streamer').GetValue('Path')",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            
            
            var PID = Process.GetCurrentProcess().Id;
            var vStreamerPath = p.StandardOutput.ReadToEnd().Replace("/","\\").Replace(Environment.NewLine,"");
            p.WaitForExit();
            var findProcess = $"(Get-Process | Where-Object {{$_.Id -eq {PID.ToString()}}}).WaitForExit();";
            var wait = $"";
            var launchUnityInjected = $"&\"{vStreamerPath}VirtualDesktop.Streamer.exe\" \"{unityExePath}\" {injectionValidationArg} -useHub -hubIPC -cloudEnvironment production -projectPath \"\\\"\"{projectRootPath}\\\"\"\";";
            using (StreamWriter fs = File.CreateText(shellScriptPath))
            {
                fs.WriteLine(findProcess);
                // fs.WriteLine(wait);
                fs.WriteLine(launchUnityInjected);
                Console.Write(launchUnityInjected);
                fs.Close();
            }

            
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = projectRootPath,
                FileName = "powershell.exe",
                //-NoExit -NoProfile -ExecutionPolicy unrestricted
                Arguments = $".\\{shellScriptFileName.Replace(" ","^ ")} -NoExit",
                UseShellExecute = true,
                RedirectStandardOutput = false,
            };
            new Thread(() =>
            {
                Debug.Log("Injecting shell");
                Thread.CurrentThread.IsBackground = true;
                Process shellProcess = Process.Start(startInfo);
            }).Start();
            EditorApplication.Exit(0);
            // if (EditorApplication.isPlaying || EditorApplication.isPaused)
            // {
            //     EditorApplication.isPlaying = false;
            // }
            
        }
    }
}