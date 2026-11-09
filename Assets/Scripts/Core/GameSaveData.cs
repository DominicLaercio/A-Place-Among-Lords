using System;
using System.Collections.Generic;

namespace APlaceAmongLords.Core
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int saveVersion = 1;
        public int currentRound;
        public bool isComplete;
        public bool attributesAllocated;
        public int diplomacy;
        public int warfare;
        public int management;
        public int combat;
        public int influence;
        public int treasury;
        public int supplies;
        public int reputation;
        public int troops;
        public List<int> relationshipValues = new List<int>();
        public List<EventProgress> eventProgress = new List<EventProgress>();
        public List<bool> conversationResponses = new List<bool>();
        public List<DialogueReplyRecord> dialogueReplies = new List<DialogueReplyRecord>();
    }

    [Serializable]
    public sealed class DialogueReplyRecord
    {
        public string dialogueId;
        public string replyText;
    }
}
