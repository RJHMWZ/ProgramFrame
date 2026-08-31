using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源信息基类。
/// 用于让字典保存不同类型的 ResInfo2<T>。
/// </summary>
public abstract class ResInfoBase2
{
}

/// <summary>
/// 保存资源、回调和协程等信息。
/// </summary>
public class ResInfo2<T> : ResInfoBase2
{
    // 加载完成后的资源
    public T asset;

    // 等待资源加载完成的回调
    public UnityAction<T> callBack;

    // 当前执行的异步加载协程
    public Coroutine coroutine;

    // 是否需要在加载完成后卸载
    public bool isDel;
}

/// <summary>
/// Resources 资源加载管理器。
/// </summary>
public class ResMgr3 : Singleton2<ResMgr3>
{
    // 保存正在加载和已经加载完成的资源
    private readonly Dictionary<string, ResInfoBase2> resDic = new();

    private ResMgr3()
    {
    }

    /// <summary>
    /// 同步加载资源。
    /// </summary>
    public T Load<T>(string path)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        // 字典中没有记录：同步加载并缓存
        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase2 baseInfo))
        {
            T resource = Resources.Load<T>(path);

            ResInfo2<T> newInfo = new ResInfo2<T>();
            newInfo.asset = resource;

            resDic.Add(resName, newInfo);
            return resource;
        }

        ResInfo2<T> info = baseInfo as ResInfo2<T>;

        if (info == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return null;
        }

        // 字典中存在记录，但异步加载尚未完成
        if (info.asset == null)
        {
            if (info.coroutine != null)
            {
                MonoMgr.Instance.StopCoroutine(info.coroutine);
            }

            // 改为同步加载
            T resource = Resources.Load<T>(path);
            info.asset = resource;

            // 通知之前等待异步结果的调用者
            info.callBack?.Invoke(resource);

            info.callBack = null;
            info.coroutine = null;

            return resource;
        }

        // 已经加载完成，直接返回缓存
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
            out ResInfoBase2 baseInfo))
        {
            ResInfo2<T> info = new ResInfo2<T>();

            resDic.Add(resName, info);
            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync<T>(path)
            );

            return;
        }

        ResInfo2<T> ResInfo2 = baseInfo as ResInfo2<T>;

        if (ResInfo2 == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return;
        }

        if (ResInfo2.asset == null)
        {
            ResInfo2.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(ResInfo2.asset);
        }
    }

    /// <summary>
    /// 真正执行泛型异步加载的协程。
    /// </summary>
    private IEnumerator ReallyLoadAsync<T>(string path)
        where T : UnityEngine.Object
    {
        ResourceRequest request =Resources.LoadAsync<T>(path);
        yield return request;
        string resName = path + "_" + typeof(T).Name;
        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase2 baseInfo))
        {
            yield break;
        }

        ResInfo2<T> ResInfo2 = baseInfo as ResInfo2<T>;

        if (ResInfo2 == null)
        {
            yield break;
        }

        ResInfo2.asset = request.asset as T;

        // 加载期间收到了卸载请求
        if (ResInfo2.isDel)
        {
            UnloadAsset<T>(path);
            yield break;
        }

        ResInfo2.callBack?.Invoke(ResInfo2.asset);

        ResInfo2.callBack = null;
        ResInfo2.coroutine = null;
    }

    /// <summary>
    /// Type 异步加载方式。
    /// 不建议与泛型方式混用。
    /// </summary>
    [Obsolete(
        "建议使用泛型加载方式；Type方式不能与泛型方式混合加载同类型、同名资源"
    )]
    public void LoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase2 baseInfo))
        {
            ResInfo2<UnityEngine.Object> info =
                new ResInfo2<UnityEngine.Object>();

            resDic.Add(resName, info);
            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync(path, type)
            );

            return;
        }

        ResInfo2<UnityEngine.Object> ResInfo2 =
            baseInfo as ResInfo2<UnityEngine.Object>;

        if (ResInfo2 == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return;
        }

        if (ResInfo2.asset == null)
        {
            ResInfo2.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(ResInfo2.asset);
        }
    }

    /// <summary>
    /// 真正执行 Type 异步加载的协程。
    /// </summary>
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
            out ResInfoBase2 baseInfo))
        {
            yield break;
        }

        ResInfo2<UnityEngine.Object> ResInfo2 =
            baseInfo as ResInfo2<UnityEngine.Object>;

        if (ResInfo2 == null)
        {
            yield break;
        }

        ResInfo2.asset = request.asset;

        if (ResInfo2.isDel)
        {
            UnloadAsset(path, type);
            yield break;
        }

        ResInfo2.callBack?.Invoke(ResInfo2.asset);

        ResInfo2.callBack = null;
        ResInfo2.coroutine = null;
    }

    /// <summary>
    /// 使用泛型卸载指定资源。
    /// </summary>
    public void UnloadAsset<T>(string path)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase2 baseInfo))
        {
            return;
        }

        ResInfo2<T> ResInfo2 = baseInfo as ResInfo2<T>;

        if (ResInfo2 == null)
            return;

        if (ResInfo2.asset != null)
        {
            resDic.Remove(resName);
            Resources.UnloadAsset(ResInfo2.asset);
        }
        else
        {
            // 资源还在加载，等待加载完成后再卸载
            ResInfo2.isDel = true;
        }
    }

    /// <summary>
    /// 使用 Type 卸载指定资源。
    /// </summary>
    public void UnloadAsset(string path, Type type)
    {
        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase2 baseInfo))
        {
            return;
        }

        ResInfo2<UnityEngine.Object> ResInfo2 =
            baseInfo as ResInfo2<UnityEngine.Object>;

        if (ResInfo2 == null)
            return;

        if (ResInfo2.asset != null)
        {
            resDic.Remove(resName);
            Resources.UnloadAsset(ResInfo2.asset);
        }
        else
        {
            ResInfo2.isDel = true;
        }
    }

    /// <summary>
    /// 异步卸载当前未使用的资源。
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
        AsyncOperation operation =
            Resources.UnloadUnusedAssets();

        yield return operation;

        callBack?.Invoke();
    }
}