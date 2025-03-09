using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글톤 Static 패턴
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<GameManager>();
            return instance;
        }
    }

    // 모든 매니저가 저장되는 딕셔너리
    private Dictionary<Type, BaseManager> managers = new Dictionary<Type, BaseManager>();

    public SettingManager Setting => GetManager<SettingManager>();

    // 매니저를 가져오는 함수
    public static T GetManager<T>() where T : BaseManager
    {
        // 딕셔너리에서 해당 타입의 매니저를 찾아 반환
        if (instance.managers.TryGetValue(typeof(T), out var manager))
        {
            if (manager is T)
                return manager as T;
        }

        CustomLog.LogError($"Manager of type {typeof(T)} not found!");
        return null;
    }

    // 매니저를 등록하는 함수
    public void RegisterManager(BaseManager manager)
    {
        var type = manager.GetType();
        if (!managers.ContainsKey(type))
        {
            managers[type] = manager;
        }
        else
        {
            Debug.LogError("WARNING! FIND TWO MANAGER : " + manager.name);
        }
    }

    void Awake()
    {
        // 자기 자신을 즉시 등록 (검색 없이)
        if (Instance == null)
        {
            instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
}

