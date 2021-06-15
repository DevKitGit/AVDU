using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
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
        
        [SettingsProvider]
        public static SettingsProvider AvduSettingsProvider()
        {
            var provider = new SettingsProvider("Project/AvduSettings", SettingsScope.Project)
            {
                label = "AVDU Launcher",
                guiHandler = GUIHandler,
            };
            SavedPath = EditorPrefs.GetString(AvduKeys.VdsPath,"");
            ReviveVd = EditorPrefs.GetBool(AvduKeys.ReviveVd, false);
            
            AvduDebug = EditorPrefs.GetBool(AvduKeys.DebugMode, false);
            return provider;
        }

  
        private static void GUIHandler(string obj)
        {
            var lWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 285;
            EditorGUILayout.BeginHorizontal();
            var tempPath = NewPath;
            if (String.IsNullOrEmpty(NewPath.Trim()))
            {
                NewPath = EditorPrefs.GetString(AvduKeys.VdsPath, "");
            }
            NewPath = EditorGUILayout.TextField("Virtual Desktop Streamer Path", NewPath);
        
            if (GUILayout.Button("find",GUILayout.Width(40)))
            {
                NewPath = EditorUtility.OpenFolderPanel("Find the folder containing the 'VirtualDesktop.Streamer.exe' file", NewPath,"");
            }
            EditorGUILayout.EndHorizontal();
        
            if (!new FileInfo($"{NewPath}/VirtualDesktop.Streamer.exe").Exists)
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

            if(RunningCheck)
            {
                EditorGUI.indentLevel++;
                var toggleReviveVd = EditorGUILayout.Toggle("Also try to restart Virtual Desktop?", ReviveVd);
                if (toggleReviveVd != ReviveVd)
                {
                    ReviveVd = toggleReviveVd;
                    EditorPrefs.SetBool(AvduKeys.ReviveVd, toggleReviveVd);
                }
                EditorGUI.indentLevel--;
            }
            var toggleConnectedCheck = EditorGUILayout.Toggle("Is not connected to a headset", ConnectedCheck);
            if (toggleConnectedCheck != ConnectedCheck)
            {
                ConnectedCheck = toggleConnectedCheck;
                EditorPrefs.SetBool(AvduKeys.DisablePlayModeConnected, toggleConnectedCheck);
            }
            EditorGUI.indentLevel--;
            var toggleDebug = EditorGUILayout.Toggle("Enable Debug mode", AvduDebug);
            if (toggleDebug != AvduDebug)
            {
                AvduDebug = toggleDebug;
                EditorPrefs.SetBool(AvduKeys.DebugMode, toggleDebug);
            }
            EditorGUIUtility.labelWidth = lWidth;
        }
    }
}
