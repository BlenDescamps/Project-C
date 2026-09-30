using System;

namespace KJD.DreamVision.Data
{
    [Serializable]
    public struct SnapshotMetadata
    {
        public string NodeGUID;         // Unique ID for this specific snapshot
        public string ParentGUID;       // The ID of the branch this spawned from (empty if root)
        public string BranchName;       // e.g., "Grappling Hook Prototype"
        public long TimestampTicks;     // Chronological sorting data (math is faster than strings)
        public string[] Tags;           // e.g., ["FUN", "FLOATY"]
        public string UserNotes;        // Your design thoughts at the time
        public string SceneFilePath;    // Where the actual backup .unity file is hidden
    }
}