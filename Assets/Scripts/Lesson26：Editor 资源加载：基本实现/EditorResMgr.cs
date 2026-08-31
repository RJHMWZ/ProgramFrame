using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器资源管理器。
/// 只能在 Unity 编辑器中使用。
/// </summary>
public class EditorResMgr : Singleton2<EditorResMgr>
{
    private EditorResMgr(){}

    private const string RootPath="Assets/Editor/ArtRes/";

    /// <summary>
    /// 加载单个编辑器资源。
    /// 传入路径不需要包含扩展名，内部自行判断
    /// </summary>
    public T LoadEditorRes<T>(string path)where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("编辑器资源路径不能为空");
            return null;
        }

        string suffixName = GetSuffixName<T>();

        if (string.IsNullOrEmpty(suffixName))
        {
            Debug.LogError($"尚未配置资源类型 {typeof(T).Name} 的扩展名");
            return null;
        }

        string fullPath = RootPath + path + suffixName;

        T resource =AssetDatabase.LoadAssetAtPath<T>(fullPath);

        if (resource == null)
        {
            Debug.LogError($"没有找到编辑器资源：{fullPath}");
        }

        return resource;
    }

    /// <summary>
    /// 加载图集中的单个图片
    /// </summary>
    /// <param name="path">图集路径 </param>
    /// <param name="spriteName">图片名字</param>
    /// <returns></returns>
    public Sprite LoadSprite(string path,string spriteName)
    {
        if (string.IsNullOrEmpty(path) ||
            string.IsNullOrEmpty(spriteName))
        {
            Debug.LogError("图集路径或者 Sprite 名称不能为空");
            return null;
        }

        string fullPath = RootPath + path;

        UnityEngine.Object[] assets =
            AssetDatabase.LoadAllAssetRepresentationsAtPath(
                fullPath
            );

        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite &&
                sprite.name == spriteName)
            {
                return sprite;
            }
        }

        Debug.LogError(
            $"图集 {fullPath} 中没有找到 Sprite：{spriteName}"
        );

        return null;
    }

    /// <summary>
    /// 加载图集中的所有 Sprite。
    /// 传入路径需要包含扩展名。
    /// </summary>
    public Dictionary<string, Sprite> LoadSprites(
        string path)
    {
        Dictionary<string, Sprite> spriteDic = new();

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("图集路径不能为空");
            return spriteDic;
        }

        string fullPath = RootPath + path;

        UnityEngine.Object[] assets =
            AssetDatabase.LoadAllAssetRepresentationsAtPath(
                fullPath
            );

        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                spriteDic[sprite.name] = sprite;
            }
        }

        return spriteDic;
    }

    /// <summary>
    /// 根据资源类型获取文件扩展名。
    /// </summary>
    private string GetSuffixName<T>()where T : UnityEngine.Object
    {
        System.Type type = typeof(T);
        if (type == typeof(GameObject))
            return ".prefab";
        if (type == typeof(Material))
            return ".mat";
        if (type == typeof(Texture) ||
            type == typeof(Texture2D) ||
            type == typeof(Sprite))
        {
            return ".png";
        }
        if (type == typeof(AudioClip))
            return ".mp3";
        return string.Empty;
    }
}
