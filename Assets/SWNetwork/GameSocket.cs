using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SWNetwork;
using SWNetwork.Core;
using System;
using System.Text;
public class GameSocket : MonoBehaviour
{

    public enum LobbyState
    {
        Default,
        JoinedRoom,
    }

    [Serializable]
    public class PlayerData
    {
        public byte[] encodedName;

        public PlayerData(string name)
        {
            encodedName = Encoding.UTF8.GetBytes(name);
        }

        public string DecodeName()
        {
            return Encoding.UTF8.GetString(encodedName);
        }
    }

    public LobbyState State = LobbyState.Default;
    public bool Debugging = false;

    public GameObject PopoverBackground;
    public GameObject WaitForOpponentPopover;
    public GameObject Player1Portrait;
    public GameObject Player2Portrait;
    public InputField EnterNickName;

    public string myId;
    public InputField FiledAnswer;
    public List<string> words = new List<string>();
    public int iword, oword;
    public Text iWordShow, oWordShow;
    public Text iNameShow, oNameShow;
    public string oname;

    public GameObject menu, game;


    public void EnterAnswer()
    {
        for (int i = 0; i < words.Count; i++)
        {
            if (FiledAnswer.text == words[i])
            {
          //      AddWord(1);
                break;
            }
        }

        //Clear filedAnswer Text
        FiledAnswer.text = "";
    }
    //public void AddWord(int add)
    //{
    //    int word = syncPropertyAgentPlayer.GetPropertyWithName("AddWord").GetIntValue();
    //    word += add;
    //    syncPropertyAgentPlayer.Modify("AddWord", word);
    //}

    //public void OnWordChanged()
    //{
    //    // read hp value
    //    iword = syncPropertyAgentPlayer.GetPropertyWithName("AddWord").GetIntValue();
    //    UpdateHealthBar(iword);
    //}
    //public void OnWordReady()
    //{
    //    int remoteHP = syncPropertyAgentPlayer.GetPropertyWithName("AddWord").GetIntValue();
    //    if (networkIDPlayer.IsMine)
    //    {
    //        int version = syncPropertyAgentPlayer.GetPropertyWithName("AddWord").version;
    //        print("Verssion: " + version);
    //        if (version != 0)
    //        {
    //            iword = remoteHP;
    //        }
    //        else
    //        {
    //            syncPropertyAgentPlayer.Modify("AddWord", 0);
    //            iword = 0;
    //        }
    //    }
    //    else
    //    {
    //        iword = remoteHP;
    //    }
    //    UpdateHealthBar(iword);
    //    print("Verssion: " + remoteHP);
    //}

    void UpdateHealthBar(int currentWord)
    {
        iWordShow.text = currentWord.ToString();
    }


    private void Start()
    {
        // disable all online UI elements
        HideAllPopover();
        NetworkClient.Lobby.OnLobbyConnectedEvent += OnLobbyConnected;
        NetworkClient.Lobby.OnNewPlayerJoinRoomEvent += OnNewPlayerJoinRoomEvent;
        NetworkClient.Lobby.OnRoomReadyEvent += OnRoomReadyEvent;
        NetworkClient.Lobby.OnPlayerLeaveRoomEvent += CheckLeftRoom;
        NetworkClient.Lobby.OnRoomMessageEvent += OnGetMassageRoom;
    }

    private void OnDestroy()
    {
        if (NetworkClient.Lobby != null)
        {
            NetworkClient.Lobby.OnLobbyConnectedEvent -= OnLobbyConnected;
            NetworkClient.Lobby.OnNewPlayerJoinRoomEvent -= OnNewPlayerJoinRoomEvent;
            NetworkClient.Lobby.OnPlayerLeaveRoomEvent -= CheckLeftRoom;
            NetworkClient.Lobby.OnRoomMessageEvent -= OnGetMassageRoom;
        }
    }


    void OnGetMassageRoom(SWMessageRoomEventData eventData)
    {
        string data = eventData.GetMessageData<PlayerData>().DecodeName();
        print(data);

        if (data.EndsWith("*DataOppenent"))
        {
            string[] o_data = data.Split("*"[0]);
            oname = o_data[0];
            iNameShow.text = myId;
            oNameShow.text = oname;
        }
    }

    //Check Left Player
    void CheckLeftRoom(SWLeaveRoomEventData eventData)
    {
        if (NetworkClient.Lobby.IsOwner)
        {
            //check player left or oppenent
            if (eventData.leavePlayerIds[0] == myId)
            {
                print("You Losse , You Left The Room");
            }
            else
            {
                print("You Winner , Oppenent Left Room");
            }
        }
    }


    void ShowEnterNicknamePopover()
    {
        PopoverBackground.SetActive(true);
        EnterNickName.gameObject.SetActive(true);
    }

    void ShowJoinedRoomPopover()
    {
        PopoverBackground.SetActive(true);
        WaitForOpponentPopover.SetActive(true);
        EnterNickName.gameObject.SetActive(false);
        Player1Portrait.SetActive(false);
        Player2Portrait.SetActive(false);
    }

    void ShowReadyToStartUI()
    {
        Player1Portrait.SetActive(true);
        Player2Portrait.SetActive(true);
        Invoke("OnStartRoomClicked", 1f);
        //StartRoomGame.gameObject.SetActive(true);
    }

    void HideAllPopover()
    {
        PopoverBackground.SetActive(false);
        WaitForOpponentPopover.SetActive(false);
        Player1Portrait.SetActive(false);
        Player2Portrait.SetActive(false);
    }

    //****************** Matchmaking *********************//
    void Checkin()
    {
        NetworkClient.Instance.CheckIn(myId, (bool successful, string error) =>
        {
            if (!successful)
            {
                Debug.LogError(error);
            }
        });
    }

    void RegisterToTheLobbyServer()
    {
        NetworkClient.Lobby.Register(myId, (successful, reply, error) => {
            if (successful)
            {
                Debug.Log("Lobby registered " + reply);
                if (string.IsNullOrEmpty(reply.roomId))
                {
                    JoinOrCreateRoom();
                }
                else if (reply.started)
                {
                    State = LobbyState.JoinedRoom;
                    ConnectToRoom();
                }
                else
                {
                    State = LobbyState.JoinedRoom;
                    ShowJoinedRoomPopover();
                    GetPlayersInTheRoom();
                }
            }
            else
            {
                Debug.Log("Lobby failed to register " + reply);
            }
        });
    }

    void JoinOrCreateRoom()
    {
        ResetGame();
        NetworkClient.Lobby.JoinOrCreateRoom(false, 2, 60, (successful, reply, error) => {
            if (successful)
            {
                Debug.Log("Joined or created room " + reply);
                State = LobbyState.JoinedRoom;
                ShowJoinedRoomPopover();
                GetPlayersInTheRoom();

            }
            else
            {
                Debug.Log("Failed to join or create room " + error);
            }
        });
    }

    private void ResetGame()
    {
        State = LobbyState.Default;
    }


    void GetPlayersInTheRoom()
    {
        NetworkClient.Lobby.GetPlayersInRoom((successful, reply, error) => {
            if (successful)
            {
                Debug.Log("Got players " + reply);
                if (reply.players.Count == 1)
                {
                    Player1Portrait.SetActive(true);
                    Player2Portrait.SetActive(false);
                }
                else
                {
                    Player1Portrait.SetActive(true);
                    Player2Portrait.SetActive(true);

                    if (NetworkClient.Lobby.IsOwner)
                    {
                        ShowReadyToStartUI();
                    }
                }
            }
            else
            {
                Debug.Log("Failed to get players " + error);
            }
        });
    }

    public void LeaveRoom()
    {
        NetworkClient.Lobby.LeaveRoom((successful, error) => {
            if (successful)
            {
                Debug.Log("Left room");
                State = LobbyState.Default;

                menu.SetActive(true);
                game.SetActive(false);

            }
            else
            {
                Debug.Log("Failed to leave room " + error);
            }
        });
    }

    void StartRoom()
    {
        NetworkClient.Lobby.StartRoom((successful, error) => {
            if (successful)
            {
                Debug.Log("Started room.");
            }
            else
            {
                Debug.Log("Failed to start room " + error);
            }
        });
    }


    void ConnectToRoom()
    {
        // connect to the game server of the room.
        NetworkClient.Instance.ConnectToRoom((connected) =>
        {
            Debug.Log(connected.ToString());
            if (connected)
            {
                Debug.Log("Connect To Room");
                game.SetActive(true);
                menu.SetActive(false);
                PopoverBackground.SetActive(false);
                WaitForOpponentPopover.SetActive(false);
                SendMessageGame(myId  + "*DataOppenent");
            }
            else
            {
                Debug.Log("Failed to connect to the game server.");
            }
        });
    }



    //****************** Lobby events *********************//
    void OnLobbyConnected()
    {
        RegisterToTheLobbyServer();
    }

    void OnNewPlayerJoinRoomEvent(SWJoinRoomEventData eventData)
    {
        if (NetworkClient.Lobby.IsOwner)
        {
            ShowReadyToStartUI();
        }
    }

    void OnRoomReadyEvent(SWRoomReadyEventData eventData)
    {
        ConnectToRoom();
    }

    /// <summary>
    /// Start button in the WaitForOpponentPopover was clicked.
    /// </summary>
    public void OnStartRoomClicked()
    {
        Debug.Log("OnStartRoomClicked");
        // Start room
        StartRoom();
    }


    //****************** UI event handlers *********************//
    /// <summary>
    /// Practice button was clicked.
    /// </summary>
    public void OnPracticeClicked()
    {
        Debug.Log("OnPracticeClicked");
    }

    /// <summary>
    /// Online button was clicked.
    /// </summary>
    public void OnOnlineClicked()
    {
        Debug.Log("OnOnlineClicked");
        ShowEnterNicknamePopover();
    }

    /// <summary>
    /// Cancel button in the popover was clicked.
    /// </summary>
    public void OnCancelClicked()
    {
        Debug.Log("OnCancelClicked");

        if (State == LobbyState.JoinedRoom)
        {
            // TODO: leave room.
            LeaveRoom();
        }


        HideAllPopover();

    }

    public void SendMessageGame(string msg)
    {
        object messageData = new PlayerData(msg);
        NetworkClient.Lobby.MessageRoom(messageData, (bool successful, SWLobbyError error) =>
        {
            if (successful)
            {
                Debug.Log("Sent room message");
            }
            else
            {
                Debug.Log("Failed to send room message " + error);
            }
        });
    }
    public void OnApplicationQuit()
    {
        OnCancelClicked();
    }

    /// <summary>
    /// Ok button in the EnterNicknamePopover was clicked.
    /// </summary>
    public void OnConfirmNicknameClicked()
    {
        myId = EnterNickName.text;
        Debug.Log($"OnConfirmNicknameClicked: {myId}");
        if (Debugging)
        {
            ShowJoinedRoomPopover();
            ShowReadyToStartUI();
        }
        else
        {
            //Use nickname as player custom id to check into SocketWeaver.
            Checkin();
        }
    }
}
