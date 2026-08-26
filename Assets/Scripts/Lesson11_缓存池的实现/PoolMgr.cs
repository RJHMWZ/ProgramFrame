using System.Collections;
using System.Collections.Generic;
using UnityEditor.iOS;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PoolMgr : Singleton2<PoolMgr>
{
    private Dictionary<string,Stack<GameObject>> poolDic=new Dictionary<string, Stack<GameObject>>();
    private PoolMgr()
    {
        
    }
    
    /// <summary>
    /// 从缓存池中取物体
    /// </summary>
    /// <param name="name">物体相对路径。比如：Prefabs\Cube</param>
    /// <returns></returns>
    public GameObject GetObj(string name)
    {
        GameObject obj;
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            obj=poolDic[name].Pop();
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.Instantiate(Resources.Load<GameObject>(name));
            obj.name=name;
        }
        return obj;
    } 

    /// <summary>
    /// 向缓存池中保存物体
    /// </summary>
    /// <param name="Obj"></param>
    public void saveObj(GameObject Obj)
    {
        Obj.SetActive(false);
        if (!poolDic.ContainsKey(Obj.name))
        {
            poolDic.Add(Obj.name,new Stack<GameObject>());
        }
        poolDic[Obj.name].Push(Obj);
    }
}
