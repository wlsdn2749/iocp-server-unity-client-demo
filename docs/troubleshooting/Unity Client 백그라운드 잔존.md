---
tags: [troubleshooting]
date: 2025-07-04
---
# Unity Client 백그라운드 잔존

## **1. 환경**
- 커밋 :  
- 버전 :  v0.0.6
- 유니티 버전 : 6000.0.31f1

## **2. 증상**
빌드된 유니티 프로그램을 종료해도 백그라운드에 프로세스가 잔존해있음.

## **3. 원인**
유니티 버전 문제 였음

당시 Unity  6000.0.31f1을 사용하고 있었고, 그 버전에서 발생하는 문제였음

- 발생 이유 요약 : 버전 문제

## **4. 해결**

- 6000.0.37f1에서 이 문제는 해결되었으므로 , 업그레이드 하는 김에 6000.1로 버전 업그레이드를 해서 해결함.
- 혹은 다음과 같은 코드를 넣으면 해결 할 수 있다.

```csharp
void OnApplicationQuit()  
{  
	if (!Application.isEditor)  
	{  
		System.Diagnostics.Process.GetCurrentProcess().Kill();  
	}  
}
```

빌드시 프로세스를 죽이는 코드인데, 이 코드를 사용해도되고
혹은 IL2CPP Build의 경우
```cpp
using System;
using System.Runtime.InteropServices;

using UnityEngine;


public static class AppTerminator
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int TerminateProcess(IntPtr hProcess, uint exitCode);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeOnLoad()
    {
        Application.quitting += Application_Quitting;
    }

    private static void Application_Quitting()
    {
        IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().Handle;
        TerminateProcess(handle, 0);
    }
#endif
}
```

이 코드를 사용해 된다.

출처: https://discussions.unity.com/t/bug-unity-6-build-game-process-continues-running-in-background-after-closing-window/1573387/17