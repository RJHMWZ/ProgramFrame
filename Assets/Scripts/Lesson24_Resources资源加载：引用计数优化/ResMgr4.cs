using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源信息基类。
/// </summary>
public abstract class ResInfoBase3
{
    // 当前资源的使用者数量
    public int refCount;
}

/// <summary>
/// 保存资源及其加载信息。
/// </summary>
public class ResInfo3<T> : ResInfoBase3
{
    public T asset;
    public UnityAction<T> callBack;
    public Coroutine coroutine;

    // 引用计数为零时是否立即卸载
    public bool isDel;

    public void AddRefCount()
    {
        refCount++;
    }

    public void SubRefCount()
    {
        if (refCount <= 0)
        {
            Debug.LogError(
                "引用计数不能小于0，请检查加载和卸载是否配对"
            );
            return;
        }

        refCount--;
    }
}

/// <summary>
/// Resources 资源加载管理器。
/// </summary>
public class ResMgr4 : Singleton2<ResMgr4>
{
    private readonly Dictionary<string, ResInfoBase3> resDic = new();

    private ResMgr4()
    {
    }

    /// <summary>
    /// 同步加载资源。
    /// </summary>
    public T Load<T>(string path)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            T resource = Resources.Load<T>(path);

            ResInfo3<T> newInfo = new ResInfo3<T>();
            newInfo.asset = resource;
            newInfo.AddRefCount();

            resDic.Add(resName, newInfo);
            return resource;
        }

        ResInfo3<T> info = baseInfo as ResInfo3<T>;

        if (info == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return null;
        }

        info.AddRefCount();

        // 同一资源正在异步加载，改为同步加载
        if (info.asset == null)
        {
            if (info.coroutine != null)
            {
                MonoMgr.Instance.StopCoroutine(info.coroutine);
            }

            T resource = Resources.Load<T>(path);
            info.asset = resource;

            info.callBack?.Invoke(resource);

            info.callBack = null;
            info.coroutine = null;

            return resource;
        }

        return info.asset;
    }

    /// <summary>
    /// 泛型异步加载资源。
    /// </summary>
    public void LoadAsync<T>(
        string path,
        UnityAction<T> callBack)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            ResInfo3<T> info = new ResInfo3<T>();
            info.AddRefCount();

            resDic.Add(resName, info);
            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync<T>(path)
            );

            return;
        }

        ResInfo3<T> resInfo = baseInfo as ResInfo3<T>;

        if (resInfo == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return;
        }

        resInfo.AddRefCount();

        if (resInfo.asset == null)
        {
            resInfo.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(resInfo.asset);
        }
    }

    private IEnumerator ReallyLoadAsync<T>(string path)
        where T : UnityEngine.Object
    {
        ResourceRequest request =
            Resources.LoadAsync<T>(path);

        yield return request;

        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            yield break;
        }

        ResInfo3<T> resInfo = baseInfo as ResInfo3<T>;

        if (resInfo == null)
            yield break;

        resInfo.asset = request.asset as T;

        if (resInfo.refCount == 0)
        {
            // 管理器内部检查，不重复减少计数
            UnloadAsset<T>(
                path,
                resInfo.isDel,
                null,
                false
            );

            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
        else
        {
            resInfo.callBack?.Invoke(resInfo.asset);
            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
    }

    /// <summary>
    /// Type 异步加载方式。
    /// </summary>
    [Obsolete(
        "建议使用泛型加载；Type方式不能与泛型方式混合加载同类型、同名资源"
    )]
    public void LoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            ResInfo3<UnityEngine.Object> info =
                new ResInfo3<UnityEngine.Object>();

            info.AddRefCount();
            resDic.Add(resName, info);
            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync(path, type)
            );

            return;
        }

        ResInfo3<UnityEngine.Object> resInfo =
            baseInfo as ResInfo3<UnityEngine.Object>;

        if (resInfo == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return;
        }

        resInfo.AddRefCount();

        if (resInfo.asset == null)
        {
            resInfo.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(resInfo.asset);
        }
    }

    private IEnumerator ReallyLoadAsync(
        string path,
        Type type)
    {
        ResourceRequest request =
            Resources.LoadAsync(path, type);

        yield return request;

        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            yield break;
        }

        ResInfo3<UnityEngine.Object> resInfo =
            baseInfo as ResInfo3<UnityEngine.Object>;

        if (resInfo == null)
            yield break;

        resInfo.asset = request.asset;

        if (resInfo.refCount == 0)
        {
            UnloadAsset(
                path,
                type,
                resInfo.isDel,
                null,
                false
            );

            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
        else
        {
            resInfo.callBack?.Invoke(resInfo.asset);
            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
    }

    /// <summary>
    /// 泛型资源卸载。
    /// </summary>
    public void UnloadAsset<T>(
        string path,
        bool isDel = false,
        UnityAction<T> callBack = null,
        bool isSub = true)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            return;
        }

        ResInfo3<T> resInfo = baseInfo as ResInfo3<T>;

        if (resInfo == null)
            return;

        if (isSub)
        {
            resInfo.SubRefCount();
        }

        // 只要有一次要求立即删除，就保留删除意图
        resInfo.isDel = resInfo.isDel || isDel;

        if (resInfo.asset != null &&
            resInfo.refCount == 0 &&
            resInfo.isDel)
        {
            resDic.Remove(resName);
            Resources.UnloadAsset(resInfo.asset);
        }
        else if (resInfo.asset == null && callBack != null)
        {
            // 调用者不再等待异步结果
            resInfo.callBack -= callBack;
        }
    }

    /// <summary>
    /// Type 资源卸载。
    /// </summary>
    public void UnloadAsset(
        string path,
        Type type,
        bool isDel = false,
        UnityAction<UnityEngine.Object> callBack = null,
        bool isSub = true)
    {
        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            return;
        }

        ResInfo3<UnityEngine.Object> resInfo =
            baseInfo as ResInfo3<UnityEngine.Object>;

        if (resInfo == null)
            return;

        if (isSub)
        {
            resInfo.SubRefCount();
        }

        resInfo.isDel = resInfo.isDel || isDel;

        if (resInfo.asset != null &&
            resInfo.refCount == 0 &&
            resInfo.isDel)
        {
            resDic.Remove(resName);
            Resources.UnloadAsset(resInfo.asset);
        }
        else if (resInfo.asset == null && callBack != null)
        {
            resInfo.callBack -= callBack;
        }
    }

    /// <summary>
    /// 卸载当前未使用的资源。
    /// </summary>
    public void UnloadUnusedAssets(
        UnityAction callBack = null)
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyUnloadUnusedAssets(callBack)
        );
    }

    private IEnumerator ReallyUnloadUnusedAssets(
        UnityAction callBack)
    {
        List<string> removeList = new List<string>();

        foreach (string key in resDic.Keys)
        {
            if (resDic[key].refCount == 0)
            {
                removeList.Add(key);
            }
        }

        foreach (string key in removeList)
        {
            resDic.Remove(key);
        }

        AsyncOperation operation =
            Resources.UnloadUnusedAssets();

        yield return operation;

        callBack?.Invoke();
    }

    /// <summary>
    /// 获取指定资源的引用计数。
    /// </summary>
    public int GetRefCount<T>(string path)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (resDic.TryGetValue(
            resName,
            out ResInfoBase3 baseInfo))
        {
            return baseInfo.refCount;
        }

        return 0;
    }

    /// <summary>
    /// 清空资源字典并卸载未使用资源。
    /// </summary>
    public void ClearDic(UnityAction callBack = null)
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyClearDic(callBack)
        );
    }

    private IEnumerator ReallyClearDic(
        UnityAction callBack)
    {
        resDic.Clear();

        AsyncOperation operation =
            Resources.UnloadUnusedAssets();

        yield return operation;

        callBack?.Invoke();
    }
}