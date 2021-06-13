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
        _reviveVD = EditorPrefs.GetBool("AVDU reviveVD", false);
        _debug = EditorPrefs.GetBool("AVDU Debug", false);
        return provider;
    }

    private static string SavedPath { get; set; } = string.Empty;
    private static bool _reviveVD = true;
    private static bool _debug = true;
    private static string Path { get; set; } = string.Empty;

    private static void GUIHandler(string obj)
    {
        // EditorGUI.BeginChangeCheck();
        var lWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 250;
        
        EditorGUILayout.BeginHorizontal();
        var tempPath = Path;
        if (String.IsNullOrEmpty(Path.Trim()))
        {
            Path = EditorPrefs.GetString("AVDU VDS path", "");
        }
        Path = EditorGUILayout.TextField("Virtual Desktop Streamer Path", Path);
        
        if (GUILayout.Button("find",GUILayout.Width(40)))
        {
            Path = EditorUtility.OpenFolderPanel("Find the Virtual Desktop streamer executable", Path,"");
        }
        EditorGUILayout.EndHorizontal();
        
        if (!new FileInfo($"{Path}/VirtualDesktop.Streamer.exe").Exists)
        {
            EditorGUILayout.HelpBox("Please supply the correct folder path containing the 'VirtualDesktop.Streamer.exe' file", MessageType.Error);
            return;
        }

        if (!Path.Equals(tempPath))
        {
            SavedPath = Path;
            EditorPrefs.SetString("AVDU VDS path",SavedPath);
        }
       

        var toggleReviveVD = EditorGUILayout.Toggle("Restart VD Streamer if not alive on play", _reviveVD);
        if (toggleReviveVD != _reviveVD)
        {
            _reviveVD = toggleReviveVD;
            EditorPrefs.SetBool("AVDU reviveVD", toggleReviveVD);
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
