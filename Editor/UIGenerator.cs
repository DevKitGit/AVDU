using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// Register a SettingsProvider using IMGUI for the drawing framework:
namespace Editor
{
    static class AvduSettingsIMGUIRegister
    {
        private static string NewPath { get; set; } = string.Empty;
        private static string SavedPath { get; set; }
        private static bool ReviveVd { get; set; }
        private static bool RunningCheck { get; set; }
        private static bool ConnectedCheck { get; set; }
        private static bool AvduDebug { get; set; }
        private static bool AvduEnabled { get; set; }

        
        [SettingsProvider]
        public static SettingsProvider AvduSettingsProvider()
        {
            
            var provider = new SettingsProvider("Project/AvduSettings", SettingsScope.Project)
            {
                label = "AVDU Launcher",
                guiHandler = GUIHandler,
            };
            SavedPath = EditorPrefs.GetString(AvduKeys.VdsPath,"");
            ReviveVd = EditorPrefs.GetBool(AvduKeys.ReviveVd, true);
            RunningCheck = EditorPrefs.GetBool(AvduKeys.DisablePlayModeRunning, true);
            ConnectedCheck = EditorPrefs.GetBool(AvduKeys.DisablePlayModeConnected, true);
            AvduDebug = EditorPrefs.GetBool(AvduKeys.DebugMode, false);
            AvduEnabled = EditorPrefs.GetBool(AvduKeys.AvduEnabled, true);
            return provider;
        }

        private static bool isPathValid(string path)
        {
            var valid = false;
            if(!string.IsNullOrEmpty(path))
            {
                try
                {
                    string fileName = System.IO.Path.GetFileName(path);
                    string fileDirectory = System.IO.Path.GetDirectoryName(path);
                }
                catch (ArgumentException)
                {
                    // Path functions will throw this 
                    // if path contains invalid chars
                    valid = true;
                }
            }
            return valid;
        }
    
        private static void GUIHandler(string obj)
        {
            var lWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 285;
            
            var toggleEnabled = EditorGUILayout.Toggle("Enable AVDU (requires restart)", AvduEnabled);

            if (toggleEnabled != AvduEnabled)
            {
                
                AvduEnabled = toggleEnabled;
                EditorPrefs.SetBool(AvduKeys.AvduEnabled, AvduEnabled);
                EditorApplication.delayCall += () =>
                {
                    var choice = false;
                    if (!toggleEnabled)
                    {
                        choice = EditorUtility.DisplayDialog(
                            "AVDU - Turned off",
                            "AVDU has been instructed to turn off, but this Unity Editor process was still launched using Virtual Desktop. Restart Unity Editor?",
                            "Yes",
                            "No");
                    }
                    else
                    {
                        choice = EditorUtility.DisplayDialog(
                            "AVDU - Turned on",
                            "AVDU has been instructed to turn on, but this Unity Editor was not launched using Virtual Desktop. Restart Unity Editor?",
                            "Yes",
                            "No");
                    }
                    if (!choice) return;
                    LaunchGenerator.Initialize();
                    EditorApplication.Exit(0);
                };

            }
            if (!AvduEnabled)
            {
                EditorGUILayout.HelpBox("This project will not launch using Virtual Desktop, Unity Editor must be restarted before this takes effect. When turning back on, restart the Unity Editor if AVDU does not do so automatically", MessageType.Error);
                return;
            }
            var toggleDebug = EditorGUILayout.Toggle("Enable Debug mode", AvduDebug);
            if (toggleDebug != AvduDebug)
            {
                AvduDebug = toggleDebug;
                EditorPrefs.SetBool(AvduKeys.DebugMode, toggleDebug);
            }
            EditorGUILayout.BeginHorizontal();
            var tempPath = NewPath;
            if (String.IsNullOrEmpty(tempPath.Trim()))
            {
                NewPath = EditorPrefs.GetString(AvduKeys.RegistryPath, "");
            }
            NewPath = EditorGUILayout.TextField("Virtual Desktop Streamer Path", NewPath); 
        
            if (GUILayout.Button("find",GUILayout.Width(40)))
            {
                NewPath = EditorUtility.OpenFolderPanel("Find the folder containing the 'VirtualDesktop.Streamer.exe' file", NewPath,"");
            }
            EditorGUILayout.EndHorizontal();
        
            if (isPathValid($"{NewPath}/VirtualDesktop.Streamer.exe") && !new FileInfo($"{NewPath}/VirtualDesktop.Streamer.exe").Exists)
            {
                EditorGUILayout.HelpBox("Please supply the correct folder path containing the 'VirtualDesktop.Streamer.exe' file", MessageType.Error);
                return;
            }
            if (!NewPath.Equals(tempPath))
            {
                SavedPath = NewPath;
                EditorPrefs.SetString(AvduKeys.VdsPath,SavedPath);
            }
            EditorGUILayout.LabelField("Disable Play Mode if Virtual Desktop:");
            EditorGUI.indentLevel++;
            var toggleRunningCheck = EditorGUILayout.Toggle("Is not running", RunningCheck);
            if (toggleRunningCheck != RunningCheck)
            {
                RunningCheck = toggleRunningCheck;
                EditorPrefs.SetBool(AvduKeys.DisablePlayModeRunning, toggleRunningCheck);
            }

            if (!RunningCheck)
            {
                GUI.enabled = false;
            }
            EditorGUI.indentLevel++;
            var toggleReviveVd = EditorGUILayout.Toggle("Also try to restart Virtual Desktop?", ReviveVd);
            if (toggleReviveVd != ReviveVd)
            {
                ReviveVd = toggleReviveVd;
                EditorPrefs.SetBool(AvduKeys.ReviveVd, toggleReviveVd);
            }
            EditorGUI.indentLevel--;

            if (!RunningCheck)
            {
                GUI.enabled = true;
            }
            var toggleConnectedCheck = EditorGUILayout.Toggle("Is not connected to a headset", ConnectedCheck);
            if (toggleConnectedCheck != ConnectedCheck)
            {
                ConnectedCheck = toggleConnectedCheck;
                EditorPrefs.SetBool(AvduKeys.DisablePlayModeConnected, toggleConnectedCheck);
            }
            EditorGUI.indentLevel--;
            EditorGUIUtility.labelWidth = lWidth;
        }
    }
}
