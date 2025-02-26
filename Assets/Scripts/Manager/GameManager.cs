using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : BaseManager
{
    // 싱글톤 Static 패턴
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
            }
            return instance;
        }
    }

    // 모든 매니저가 저장되는 딕셔너리
    private static Dictionary<Type, BaseManager> managers = new Dictionary<Type, BaseManager>();

    // 매니저를 가져오는 함수
    public static T GetManager<T>() where T : BaseManager
    {
        // 딕셔너리에서 해당 타입의 매니저를 찾아 반환
        if (managers.TryGetValue(typeof(T), out var manager))
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
            //CustomLog.LogError($"Manager of type {type} is already registered.");
            Destroy(manager.gameObject);
        }
    }

    protected override void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

