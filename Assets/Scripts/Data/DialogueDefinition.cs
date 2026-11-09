using System;
using System.Collections.Generic;
using UnityEngine;

namespace APlaceAmongLords.Data
{
    /// <summary>Editable dialogue content. Create instances through the Assets/Create menu.</summary>
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "A Place Among Lords/Dialogue Definition")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        public string dialogueId;
        [Min(1)] public int roundNumber = 1;
        public string conversationKey;
        public string speakerName;
        [TextArea(3, 8)] public string openingText;
        public RelationshipTarget relationshipTarget;
        public List<DialogueResponse> responses = new List<DialogueResponse>();
    }

    [Serializable]
    public sealed class DialogueResponse
    {
        [TextArea(2, 4)] public string playerResponse;
        [TextArea(2, 5)] public string characterReply;
        [Range(-1, 1)] public int relationshipChange;
    }
}
