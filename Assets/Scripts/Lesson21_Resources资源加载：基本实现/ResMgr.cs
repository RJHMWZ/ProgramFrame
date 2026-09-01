using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ResMgr : Singleton2<ResMgr>
{
    private ResMgr()
    {
        
    }

    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T">需要加载的资源类型</typeparam>
    /// <param name="path">资源相对于 `Resources` 文件夹的路径</param>
    /// <returns></returns>
    public T Load<T>(string path)where T:UnityEngine.Object
    {
        return Resources.Load<T>(path) ;
    }

    /// <summary>
    /// 使用泛型异步加载资源
    /// </summary>
    /// <typeparam name="T">需要加载的资源类型</typeparam>
    /// <param name="path">资源相对于 `Resources` 文件夹的路径</param>
    /// <param name="callBack">加载资源完之后执行的（带参数）委托</param>
    public void LoadAsync<T>(string path,UnityAction<T> callBack) where T:UnityEngine.Object
    {
        MonoMgr.Instance.StartCoroutine(ReallyLoadAsync(path,callBack));
    }

    /// <summary>
    /// 真正执行泛型异步加载的协程
    /// </summary>
    /// <typeparam name="T">需要加载的资源类型</typeparam>
    /// <param name="path">资源相对于 `Resources` 文件夹的路径</param>
    /// <param name="callBack">加载资源完之后执行的（带参数）委托</param>

    public IEnumerator ReallyLoadAsync<T>(string path,UnityAction<T> callBack)where T : UnityEngine.Object
    {
        ResourceRequest resourceRequest= Resources.LoadAsync<T>(path);
        yield return resourceRequest;
        callBack?.Invoke(resourceRequest.asset as T);
    }

    /// <summary>
    /// 根据 Type 异步加载资源。
    /// </summary>
    /// <param name="path">资源相对于 `Resources` 文件夹的路径</param>
    /// <param name="type">要加载的资源类型</param>
    /// <param name="callBack">加载资源完之后执行的（带参数）委托</param>
    public void LoadAsync(string path,Type type,UnityAction<UnityEngine.Object> callBack)
    {
        MonoMgr.Instance.StartCoroutine(ReallyLoadAsync(path,type,callBack));
    }

    /// <summary>
    /// 真正执行 Type 异步加载的协程。
    /// </summary>
    /// <param name="path">资源相对于 `Resources` 文件夹的路径</param>
    /// <param name="type">要加载的资源类型</param>
    /// <param name="callBack">加载资源完之后执行的（带参数）委托</param>
    /// <returns></returns>
    public IEnumerator ReallyLoadAsync(string path,Type type,UnityAction<UnityEngine.Object> callBack)
    {
        ResourceRequest resourceRequest= Resources.LoadAsync(path,type);
        yield return resourceRequest;
        callBack?.Invoke(resourceRequest.asset);
    }

    /// <summary>
    /// 卸载指定的单个资源。
    /// 不能用于销毁场景中的 GameObject 实例。
    /// </summary>
    public void UnloadAsset(UnityEngine.Object assetToUnload)
    {
        if (assetToUnload == null)
            return;
        Resources.UnloadAsset(assetToUnload);
    }
    
    /// <summary>
    /// 异步卸载当前未被使用的资源，并在卸载完成后执行回调
    /// </summary>
    /// <param name="callBack"></param>
    public void UnloadUnusedAssets(UnityAction callBack = null)
    {
        MonoMgr.Instance.StartCoroutine(ReallyUnloadUnusedAssets(callBack));
    }

    /// <summary>
    /// 真正执行异步卸载当前未被使用的资源的协程
    /// </summary>
    /// <param name="callBack">资源卸载完成后执行的回调函数</param>
    /// <returns>用于协程执行的迭代器</returns>
    private IEnumerator ReallyUnloadUnusedAssets(UnityAction callBack)
    {
        AsyncOperation operation =Resources.UnloadUnusedAssets();
        yield return operation;
        callBack?.Invoke();
    }
}
