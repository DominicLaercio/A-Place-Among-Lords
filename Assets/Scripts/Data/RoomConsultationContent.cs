using System;
using System.Collections.Generic;
using UnityEngine;

namespace APlaceAmongLords.Data
{
    /// <summary>Editable text for consultation objects in the main hall.</summary>
    [CreateAssetMenu(fileName = "RoomConsultationContent", menuName = "A Place Among Lords/Room Consultation Content")]
    public sealed class RoomConsultationContent : ScriptableObject
    {
        public string noSelectionText;
        public string missingContentText;
        public List<RoundConsultationText> mapDescriptions = new List<RoundConsultationText>();
    }

    [Serializable]
    public sealed class RoundConsultationText
    {
        [Min(1)] public int roundNumber = 1;
        [TextArea(3, 6)] public string text;
    }
}
