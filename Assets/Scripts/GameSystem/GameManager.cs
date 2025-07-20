using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystem;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // Singleton
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (!_instance)
                _instance = FindAnyObjectByType<GameManager>();
            return _instance;
        }
    }

    // Managers
    private static readonly Dictionary<Type, BaseManager> _managers = new Dictionary<Type, BaseManager>();

    // public static SettingManager Setting => GetManager<SettingManager>();

    public PlayerInput playerInput { get; private set; }

    // Get Manager
    public static T GetManager<T>() where T : BaseManager
    {
        // return manager by Type
        if (_managers.TryGetValue(typeof(T), out var manager))
        {
            return manager as T;
        }

        CustomLog.LogError($"Manager of type {typeof(T)} not found!");
        return null;
    }

    public static void SetPlayerInput(bool OnOff)
    {
        if (OnOff)
        {
            Instance.playerInput.actions.Enable();
        }
        else
        {
            Instance.playerInput.actions.Disable();
        }
    }

    // Set Manager in GameManager
    public static void RegisterManager(BaseManager manager)
    {
        var type = manager.GetType();
        if (!_managers.TryAdd(type, manager))
        {
            Debug.LogError("WARNING! FIND TWO MANAGER : " + manager.name);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            _instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        playerInput = GetComponent<PlayerInput>();
        Resources.UnloadUnusedAssets();
    }

    void OnDestroy()
    {
        playerInput?.actions.Disable();

        _instance = null;
        _managers.Clear();
    }
}

