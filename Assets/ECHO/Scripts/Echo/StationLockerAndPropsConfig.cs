using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StationLockerAndProps", menuName = "ECHO/Station Locker And Props")]
public sealed class StationLockerAndPropsConfig : ScriptableObject
{
    [Serializable]
    public struct LockerPlacement
    {
        public Vector3 position;
        public float yaw;
    }

    [Serializable]
    public struct PropPlacement
    {
        public int prefabIndex;
        public Vector3 position;
        public float yaw;
        public Vector3 scale;
    }

    [SerializeField] private GameObject lockerPrefab;
    [SerializeField] private GameObject[] propPrefabs = new GameObject[0];
    [SerializeField] private LockerPlacement[] lockers = new LockerPlacement[0];
    [SerializeField] private PropPlacement[] props = new PropPlacement[0];

    public GameObject LockerPrefab => lockerPrefab;
    public GameObject[] PropPrefabs => propPrefabs;
    public LockerPlacement[] Lockers => lockers;
    public PropPlacement[] Props => props;
}
