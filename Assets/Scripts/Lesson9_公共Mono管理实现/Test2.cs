using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test2:Singleton<Test2>
{
    private bool isRunning;
    private Coroutine routine;
    public Test2()
    {
        
    }

    public void StartTest2Fun()
    {
        if (isRunning)
            return;
        isRunning = true;
        MonoMgr.Instance.AddUpdateListener(UpdateDebug);
        routine = MonoMgr.Instance.StartCoroutineListener(IEnumeratorTest());
    }

    public void StopTest2Fun()
    {
        isRunning=false;
        MonoMgr.Instance.RemoveUpdateListener(UpdateDebug);
        if(routine!=null)
        {
            MonoMgr.Instance.StopCoroutineListener(routine);
            routine=null;
        }
        
    }
    
    public void UpdateDebug()
    {
        Debug.Log("需要执行Update函数");
    }

    public IEnumerator IEnumeratorTest()
    {
        yield return new WaitForSeconds(3f);
        Debug.Log("协程执行完毕");
        routine = null;
    }
}
