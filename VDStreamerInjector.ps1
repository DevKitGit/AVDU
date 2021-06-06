(Get-Process | Where-Object {$_.Id -eq 4656}).WaitForExit();
&"C:\Program Files\Virtual Desktop Streamer\VirtualDesktop.Streamer.exe" "C:\Program Files\Unity\Hub\Editor\2020.3.11f1\Editor\Unity.exe" -injectedTrue -useHub -hubIPC -cloudEnvironment production -projectPath C:\Medialogy\Unity\VRTest;
