﻿using System;
using Il2CppCoreNet.StateSync.Routers;
using Il2CppCoreNet.StateSync.Syncs;
using UnityEngine;
using UnityEngine.Networking;

namespace CementGB.Mod.Modules.NetBeard;

public enum NetworkerType
{
    Rigidbody,
    Transform
}

[MelonLoader.RegisterTypeInIl2Cpp]
public class EzSyncer : MonoBehaviour
{
    public NetworkerType networkType;
    public GameObject? routerObject;
    public bool spawnedIn = false;

    const string b = "0123456789abcdef";
    public static NetworkHash128 NewHash()
    {
        string x = "";
        for (int i = 0; i < 32; ++i)
            x += b[UnityEngine.Random.RandomRangeInt(0, 15)];
        return NetworkHash128.Parse(x);
    }

    public void Awake()
    {
        if (routerObject == null) routerObject = gameObject;
        BaseSyncRouter? router = null;
        BaseSync? baseSync = null;
        switch (networkType)
        {
            case NetworkerType.Rigidbody:
                router = routerObject.AddComponent<RigidbodySyncRouter>();
                baseSync = gameObject.AddComponent<RigidbodySync>();
                break;

            case NetworkerType.Transform:
                router = routerObject.AddComponent<TransformSyncRouter>();
                baseSync = gameObject.AddComponent<TransformSync>();
                break;
        }

        if (baseSync == null) throw new NullReferenceException("BaseSync is unexpectedly null!");
        if (router == null) throw new NullReferenceException("BaseSyncRouter is unexpectedly null!");

        var identity = gameObject.AddComponent<NetworkIdentity>();
        baseSync.Lazy = true;
        router.Lazy = true;

        identity.m_AssetId = NewHash();
        NetworkScene.RegisterPrefab(gameObject);

        if (NetworkClient.active)
        {
            gameObject.SetActive(false);
            return;
        }

        if (NetworkServer.active)
            NetworkServer.Spawn(Instantiate(gameObject));

        Destroy(this);
    }
}
