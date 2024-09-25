﻿using HarmonyLib;
using Il2CppFemur;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using CementGB.Mod.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.AI;
using MelonLoader;

namespace CementGB.Mod.Modules.AlligatorNavigator;

[RegisterTypeInIl2Cpp]
public class AlNavigator : MonoBehaviour
{
    private static bool _navMeshInScene;
    private static List<Actor> _actorsToHandle = new();

    private static AlNavPoint[] _navPoints = new AlNavPoint[0];

    private void Awake()
    {
        SceneManager.add_sceneLoaded((UnityAction<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        _actorsToHandle.Clear();
        _navMeshInScene = FindObjectOfType<NavMeshData>() != null;
        UpdateNavPoints();
    }

    public static void UpdateNavPoints()
    {
        _navPoints = FindObjectsOfType<AlNavPoint>();
    }

    private void Update()
    {
        HandleNavigation();
    }

    private static void HandleNavigation()
    {
        if (_navPoints.Length == 0) return;

        foreach (Actor actor in _actorsToHandle)
        {
            actor.targetingHandeler.TargetOverride = _navPoints[0].transform;
        }
    }

    [HarmonyPatch(typeof(BodyHandeler_HumanoidMediumEctomorphv2), nameof(BodyHandeler_HumanoidMediumEctomorphv2.SetupTransforms))]
    private static class BodyHandelerSetupTransformsPatch
    {
        private static void Postfix(BodyHandeler_HumanoidMediumEctomorphv2 __instance)
        {
            LoggingUtilities.VerboseLog("[ALLIGATOR NAVIGATOR] patch called.");
            if (true) //(!_navMeshInScene)
            {
                LoggingUtilities.VerboseLog("[ALLIGATOR NAVIGATOR] no nav mesh available, switching agent over.");
                _actorsToHandle.Add(__instance.actor);
                __instance.agent.enabled = false;
            }
            
        }
    }
}

[RegisterTypeInIl2Cpp]
public class AlNavPoint : MonoBehaviour
{

}

public class AlNavAgent : NavMeshAgent
{

}