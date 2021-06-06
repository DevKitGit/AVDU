(Get-Process | Where-Object {$_.Id -eq 5064}).WaitForExit();
&'C:\Program Files\Virtual Desktop Streamer\VirtualDesktop.Streamer.exe' 'C:\Program Files\Unity\Hub\Editor\2020.3.11f1\Editor\Unity.exe' -projectpath 'C:\Medialogy\Unity\VRTest -useHub -hubIPC -cloudEnvironment production -injectedTrue'
