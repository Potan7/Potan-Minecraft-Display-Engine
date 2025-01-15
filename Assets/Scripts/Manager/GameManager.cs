using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : RootManager
{
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

    private static Dictionary<Type, RootManager> managers = new Dictionary<Type, RootManager>();

    public static T GetManager<T>() where T : RootManager
    {
        if (managers.TryGetValue(typeof(T), out var manager))
        {
            return manager as T;
        }

        Debug.LogError($"Manager of type {typeof(T)} not found!");
        return null;
    }

    public void RegisterManager(RootManager manager)
    {
        var type = manager.GetType();
        if (!managers.ContainsKey(type))
        {
            managers[type] = manager;
        }
        else
        {
            Debug.LogWarning($"Manager of type {type} is already registered.");
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

