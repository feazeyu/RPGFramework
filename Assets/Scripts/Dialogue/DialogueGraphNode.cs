
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Dialogue
{
    [Serializable]
    public abstract class DialogueGraphNode
    {
        public string guid;
        public string name;
        public Vector2 position;
        [SerializeReference]
        public List<DialogueGraphNode> OutgoingConnections;
    }
}
