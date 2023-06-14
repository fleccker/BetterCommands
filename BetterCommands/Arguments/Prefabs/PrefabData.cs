using Mirror;

using System.Linq;

using UnityEngine;

namespace BetterCommands.Arguments.Prefabs
{
    public struct PrefabData
    {
        public int Index { get; }
        public string Name { get; }

        public GameObject Prefab { get; }

        public PrefabData(int index)
        {
            Index = index;
            Prefab = NetworkClient.prefabs.ElementAt(index).Value;
            Name = Prefab.name;
        }
    }
}