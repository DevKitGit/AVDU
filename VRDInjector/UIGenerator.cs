using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Register a SettingsProvider using IMGUI for the drawing framework:
static class MyCustomSettingsIMGUIRegister
{
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider()
    {
        var provider = new SettingsProvider("Project/MyCustomIMGUISettings", SettingsScope.Project)
        {
            label = "Virtual Desktop Injector",
            guiHandler = GUIHandler,
            keywords = new HashSet<string>(new[] { "Number", "Some String" })
        };
        SavedPath = EditorPrefs.GetString("AVDU VDS path","");
        return provider;
    }

    public static string SavedPath { get; set; } = String.Empty;

    private static bool _autoLaunch = true;
    private static bool _debug = true;
    public static string Path { get; set; } = String.Empty;

    private static void GUIHandler(string obj)
    {
        // EditorGUI.BeginChangeCheck();
        var lWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 200;
        
        EditorGUILayout.BeginHorizontal();
        var tempPath = Path;
        Path = EditorGUILayout.TextField("Virtual Desktop Streamer Path", Path);
        if (GUILayout.Button("find",GUILayout.Width(40)))
        {
            Path = EditorUtility.OpenFilePanel("path", Path,"exe");
        }
        EditorGUILayout.EndHorizontal();
        if (!Path.Contains("VirtualDesktop.Streamer.exe") || !new FileInfo(Path).Exists)
        {
            EditorGUILayout.HelpBox("Please supply a correct path for wefoij", MessageType.Warning);
            return;
        }

        if (Path != tempPath)
        {
            SavedPath = Path;
            EditorPrefs.SetString("AVDU VDS path",SavedPath);
        }
       

        var toggleAutoLaunch = EditorGUILayout.Toggle("Launch project in injected mode", _autoLaunch);
        if (toggleAutoLaunch != _autoLaunch)
        {
            _autoLaunch = toggleAutoLaunch;
            EditorPrefs.SetBool("AVDU LaunchAutoInjected", toggleAutoLaunch);
        }
        var injectorDebug = EditorGUILayout.Toggle("Debugging mode", _debug);
        if (injectorDebug != _debug)
        {
            _debug = injectorDebug;
            EditorPrefs.SetBool("AVDU Debug", injectorDebug);
        }
        
        

        EditorGUIUtility.labelWidth = lWidth;
        
        // var originalColr = GUI.backgroundColor;
        // GUI.backgroundColor = Color.magenta;
        // if (GUILayout.Button("DONT CLICK ME aaaaaaaaaa"))
        // {
        //     Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAA");
        // }
        // GUI.backgroundColor = originalColr;

        // if (EditorGUI.EndChangeCheck())
        // {
        //     
        // }
        //GUILayout.
    }
}
