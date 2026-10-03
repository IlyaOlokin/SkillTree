using System;
using System.Collections.Generic;

namespace Tutorials
{
    [Serializable]
    public sealed class TutorialProgress
    {
        public bool skipAll;
        public List<string> completedIds = new List<string>();
        // Ordered and persisted, including the currently displayed (unacknowledged) tutorial.
        public List<string> pendingIds = new List<string>();
    }
}
