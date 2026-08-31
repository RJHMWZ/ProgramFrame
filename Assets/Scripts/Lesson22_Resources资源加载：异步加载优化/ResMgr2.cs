using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源信息基类。
/// 用于让字典保存不同类型的 ResInfo<T>。
/// </summary>
public abstract class ResInfoBase
{
}

/// <summary>
/// 保存资源及其异步加载信息。
/// </summary>
public class ResInfo<T> : ResInfoBase
{
    // 加载完成的资源
    public T asset;

    // 等待该资源的所有回调
    public UnityAction<T> callBack;

    // 当前执行的加载协程
    public Coroutine coroutine;
}

/// <summary>
/// Resources 资源加载管理器。
/// </summary>
public class ResMgr2 : Singleton2<ResMgr2>
{
    // 保存正在加载和已经加载完成的资源
    private readonly Dictionary<string, ResInfoBase> resDic = new();

    private ResMgr2()
    {
    }

    /// <summary>
    /// 同步加载 Resources 资源。
    /// 当前同步加载尚未接入自建缓存字典。
    /// </summary>
    public T Load<T>(string path)
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// 泛型异步加载资源。
    /// </summary>
    public void LoadAsync<T>(
        string path,
        UnityAction<T> callBack)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("资源路径不能为空");
            return;
        }

        // 路径和类型共同组成资源的唯一标识
        string resName = path + "_" + typeof(T).Name;

        // 字典中不存在，说明这是第一次请求该资源
        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            ResInfo<T> info = new ResInfo<T>();

            // 必须先加入字典，避免后续请求重复开启协程
            resDic.Add(resName, info);

            // 记录等待资源的回调
            info.callBack += callBack;

            // 开启并记录加载协程
            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync<T>(path)
            );

            return;
        }

        // 取出该资源对应的信息
        ResInfo<T> resInfo = baseInfo as ResInfo<T>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            return;
        }

        // asset 为空，说明资源还在加载
        if (resInfo.asset == null)
        {
            // 只追加回调，不开启新协程
            resInfo.callBack += callBack;
        }
        else
        {
            // 资源已经加载完成，直接返回缓存
            callBack?.Invoke(resInfo.asset);
        }
    }

    /// <summary>
    /// 真正执行泛型异步加载的协程。
    /// </summary>
    private IEnumerator ReallyLoadAsync<T>(string path)
        where T : UnityEngine.Object
    {
        ResourceRequest request =
            Resources.LoadAsync<T>(path);

        yield return request;

        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            yield break;
        }

        ResInfo<T> resInfo = baseInfo as ResInfo<T>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            yield break;
        }

        // 保存加载结果
        resInfo.asset = request.asset as T;

        if (resInfo.asset == null)
        {
            Debug.LogError(
                $"Resources 中没有找到资源：{path}"
            );
        }

        // 通知所有等待该资源的调用者
        resInfo.callBack?.Invoke(resInfo.asset);

        // 加载完成后清空不再需要的引用
        resInfo.callBack = null;
        resInfo.coroutine = null;
    }

    /// <summary>
    /// 使用 Type 异步加载资源。
    /// 不建议与泛型方式混合加载同路径、同类型资源。
    /// </summary>
    [Obsolete(
        "建议使用泛型加载方式；Type方式不能与泛型方式混合加载同类型、同名资源"
    )]
    public void LoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        if (string.IsNullOrEmpty(path) || type == null)
        {
            Debug.LogError("资源路径或者资源类型不合法");
            return;
        }

        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            ResInfo<UnityEngine.Object> info =
                new ResInfo<UnityEngine.Object>();

            resDic.Add(resName, info);

            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync(path, type)
            );

            return;
        }

        ResInfo<UnityEngine.Object> resInfo =
            baseInfo as ResInfo<UnityEngine.Object>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            return;
        }

        if (resInfo.asset == null)
        {
            resInfo.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(resInfo.asset);
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
            out ResInfoBase baseInfo))
        {
            yield break;
        }

        ResInfo<UnityEngine.Object> resInfo =
            baseInfo as ResInfo<UnityEngine.Object>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            yield break;
        }

        resInfo.asset = request.asset;

        if (resInfo.asset == null)
        {
            Debug.LogError(
                $"Resources 中没有找到资源：{path}"
            );
        }

        resInfo.callBack?.Invoke(resInfo.asset);

        resInfo.callBack = null;
        resInfo.coroutine = null;
    }

    /// <summary>
    /// 卸载指定资源。
    /// 当前卸载方法尚未删除 resDic 中的缓存记录。
    /// </summary>
    public void UnloadAsset(
        UnityEngine.Object assetToUnload)
    {
        if (assetToUnload == null)
            return;

        Resources.UnloadAsset(assetToUnload);
    }

    /// <summary>
    /// 异步卸载没有使用的 Resources 资源。
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