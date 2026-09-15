using UnityEngine;

namespace NOSDA
{
    // Lives on the persistent NOSDA_Worker GameObject (Plugin.OnSceneLoaded). Resets DeathCounter
    // on every mission start, so a pilot's death count doesn't carry over from a previous mission —
    // leaving to the main menu, restarting the same mission, and starting a different one all show
    // up the same way here: MissionManager.IsRunning going false then true again.
    internal class MissionLifecycle : MonoBehaviour
    {
        private bool _missionWasRunning;

        private void Update()
        {
            bool missionRunning = MissionManager.IsRunning;
            if (missionRunning && !_missionWasRunning) DeathCounter.Reset();
            _missionWasRunning = missionRunning;
        }
    }
}
