using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Editor
{
    [InitializeOnLoad]
    public static class LaunchGenerator
    {
        static LaunchGenerator()
        {
            if (!EditorPrefs.HasKey(AvduKeys.FirstTimeSetup))
            {
                _log("First time setup of EditorPref variables");
                checkVdPath();
                EditorPrefs.SetBool(AvduKeys.AvduEnabled,true);
                EditorPrefs.SetBool(AvduKeys.DisablePlayModeConnected,true);
                EditorPrefs.SetBool(AvduKeys.DisablePlayModeRunning,true);
                EditorPrefs.SetBool(AvduKeys.ReviveVd,true);
                EditorPrefs.SetBool(AvduKeys.DebugMode,false);
                EditorPrefs.SetBool(AvduKeys.FirstTimeSetup,false);
            }

            if (EditorPrefs.GetBool(AvduKeys.AvduEnabled,true)) return;
            var editorIsOpen = EditorWindow.HasOpenInstances<EditorWindow>();
            var projectRootPath = @Path.GetDirectoryName(Application.dataPath);
            _log($"Found project root at: {projectRootPath}");
            var unityExePath = EditorApplication.applicationPath.Replace("/", "\\");
            //Retrieve command-line arguments for this process
            var args = Environment.GetCommandLineArgs();
        
            const string validationArg = "-injectedTrue";
            var injectValidated = false;
            foreach (var t in args)
            {
                if (!t.Contains(validationArg)) continue;
                injectValidated = true;
                _log("Project was launched using Virtual Desktop injection");
            }
            if (editorIsOpen && injectValidated)
            {
                CheckVdRunning();
                CheckVdConnected();
            }
            if (injectValidated) return;

            //Check if commandline args contains injector validation argument
            const string preQuote = "\\\"\\\\\"\"\"\\\"";
            const string postQuote = "\\\\\"\\\"\"\"\\\"";
            var finalLauncherString = validationArg;
            for (int i = 1; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i])) continue;
                if (args[i][0] == '-') finalLauncherString += (" " + args[i]);
                else if (!args[i].Contains(" ")) finalLauncherString += (" " + args[i]);
                else finalLauncherString += (" " + preQuote + args[i] + postQuote);
            }
            _log($"Generated escaped Unity injection string: {finalLauncherString}");
            if (editorIsOpen)
            {
                _log($"Unity Editor was running at the time of attempted injection: {Math.Round(EditorApplication.timeSinceStartup)} seconds since startup");
                EditorUtility.DisplayDialog(
                    "This editor instance wasn't launched by Virtual Desktop Streamer",
                    "Close the project, (re)start Virtual Desktop streamer, and then launch the project again.",
                    "OK",
                    "");
            }

            checkVdPath();
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = EditorPrefs.GetString(AvduKeys.FinalPath,""),
                FileName = "powershell.exe",
                Arguments =
                    $"$(Get-Process | Where-Object {{$_.Id -eq {Process.GetCurrentProcess().Id.ToString()}}}).WaitForExit();if(-Not (Get-Process VirtualDesktop.Streamer -ErrorAction SilentlyContinue)) {{\".\\VirtualDesktop.Streamer.exe\"}};Start-Sleep -s 1;\".\\VirtualDesktop.Streamer.exe\" \\\"{unityExePath}\\\" {finalLauncherString};",
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
            else if (EditorPrefs.GetBool(AvduKeys.DebugMode,false))
            {
                Debug.Log($"[Inject Batch Generator] {input}");
            }
        }

        private static void CheckVdRunning()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
            _log($"Play Mode was started. Checking if VD Streamer process is running");
            var reviverProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments =$"if((Get-Process VirtualDesktop.Streamer -ErrorAction SilentlyContinue)) {{Write-Output \"True\"}} else {{Write-Output \"False\"}}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            var vdRunning = false;
            if (reviverProcess != null)
            {
                reviverProcess.WaitForExit(1000);
                vdRunning = Boolean.Parse(reviverProcess.StandardOutput.ReadToEnd());
            }
            _log($"Is VD Streamer process running: {vdRunning}");
            if (vdRunning)
            {
                if (CheckVdConnected()) return;
                if (EditorPrefs.GetBool(AvduKeys.DisablePlayModeConnected,false))
                {
                    EditorApplication.ExitPlaymode();
                    _log("Virtual Desktop Streamer is running, but is not currently connected to a headset. Exiting Play Mode, as configured in the project settings");
                }
                else
                {
                    _log("Virtual Desktop Streamer is running, but is not currently connected to a headset. Play Mode will not be exited, as configured in the project settings");
                }
                return;
            }
            if (EditorPrefs.GetBool(AvduKeys.DisablePlayModeRunning))
            {
                _log("Virtual Desktop Streamer is not running. Exiting Play Mode, as configured in the project settings");
                EditorApplication.ExitPlaymode();
            }

            if (!EditorPrefs.GetBool(AvduKeys.ReviveVd, false))
            {
                _log("Virtual Desktop Streamer is not running. AVDU will not attempt to start it, as configured in the project settings");
                return;
            }
            _log("Virtual Desktop Streamer is not running.",true);
            var sPath = EditorPrefs.GetString(AvduKeys.VdsPath, "");
            if (!new FileInfo($"{sPath}VirtualDesktop.Streamer.exe").Exists)
            {
                _log($"Incorrect VD Streamer executable folder path found: {sPath}",true);
                return;
            }

            Process.Start(new ProcessStartInfo()
            {
                WorkingDirectory = sPath,
                FileName = "powershell.exe",
                Arguments = $"\".\\VirtualDesktop.Streamer.exe\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            _log("Virtual Desktop Streamer process is not longer running. AVDU will attempt to start it. Don't start Play Mode until your VR headset reconnects",true);
        }
        
        private static bool CheckVdConnected()
        {
            _log($"Checking if VD Server process is running");
            var reviverProcess = Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments =$"if((Get-Process VirtualDesktop.Server -ErrorAction SilentlyContinue)) {{Write-Output \"True\"}} else {{Write-Output \"False\"}}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            var vdStreaming = false;
            if (reviverProcess != null)
            {
                reviverProcess.WaitForExit(1000);
                vdStreaming = bool.Parse(reviverProcess.StandardOutput.ReadToEnd());
            }
            _log($"is VD Server process running: {vdStreaming}");
            return vdStreaming;
        }

        private static void checkVdPath()
        {
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

            var userdefinedStreamerPath = $"{EditorPrefs.GetString(AvduKeys.VdsPath, "").Replace("/","\\")}";
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
                    _log($"user-specified path is empty. Picking Virtual desktop streamer folder path fetched from registry: {finalStreamerPath}");
                    EditorPrefs.SetString(AvduKeys.VdsPath, finalStreamerPath);
                }
                else
                {
                    finalStreamerPath = userdefinedStreamerPath;
                    _log($"User-specified Picking user-specified Virtual Desktop Streamer path: {finalStreamerPath}");
                }
            }
            else
            {
                finalStreamerPath = registryVdStreamerPath;
                _log($"Picking Virtual desktop streamer folder path fetched from registry: {finalStreamerPath}");

            }
            if (!new FileInfo($"{finalStreamerPath}VirtualDesktop.Streamer.exe").Exists)
            {
                _log($"Invalid VD streamer path found\"{finalStreamerPath}VirtualDesktop.Streamer.exe\"\nTwo fixes: Make sure the correct path is specified in project settings, or reinstall Virtual Desktop",true);
                return;
            }
            EditorPrefs.SetString(AvduKeys.FinalPath,finalStreamerPath);
            _log($"Virtual Desktop Streamer path is valid and the exe exists");
        }
    }
}