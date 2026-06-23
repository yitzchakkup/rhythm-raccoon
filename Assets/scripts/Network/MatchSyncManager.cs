using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MatchSyncManager : MonoBehaviourPunCallbacks
{
    [Header("Components")]
    public WordGenerator wordGenerator;

    private bool matchStarted = false;
    private double exactStartTime = 0;
    
    private const string START_TIME_KEY = "MatchStartTime";
    private const string LOADED_KEY = "SceneLoaded"; // New key to track loading

    private void Start()
    {
        // 1. Tell the network: "My scene has finished loading and I am ready!"
        Hashtable props = new Hashtable { { LOADED_KEY, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        // 2. The Master Client listens for everyone's loading status
        if (!PhotonNetwork.IsMasterClient || matchStarted || exactStartTime != 0) return;

        if (changedProps.ContainsKey(LOADED_KEY))
        {
            CheckIfAllPlayersLoaded();
        }
    }

    private void CheckIfAllPlayersLoaded()
    {
        // Ensure every single player in the room has the "SceneLoaded" property set to true
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue(LOADED_KEY, out object loadedState) || !(bool)loadedState)
            {
                return; // Someone is still loading. Stop and wait for them!
            }
        }

        // 3. Everyone is fully loaded! Set the synced clock exactly 0.5 seconds into the future.
        double futureTime = PhotonNetwork.Time + 0.5f;
        Hashtable roomProps = new Hashtable { { START_TIME_KEY, futureTime } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // 4. Both computers receive the exact start time from the Master Client
        if (propertiesThatChanged.ContainsKey(START_TIME_KEY))
        {
            exactStartTime = (double)propertiesThatChanged[START_TIME_KEY];
        }
    }

    private void Update()
    {
        // Wait until we have a start time, and stop checking once the match starts
        if (matchStarted || exactStartTime == 0) return;

        double timeLeft = exactStartTime - PhotonNetwork.Time;

        if (timeLeft <= 0)
        {
            // 5. BOOM! Start the game on the exact same frame for both players.
            matchStarted = true;
            
            if (wordGenerator != null) 
            {
                wordGenerator.StartGameLoop();
            }
        }
    }
}