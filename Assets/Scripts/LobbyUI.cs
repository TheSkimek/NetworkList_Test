using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] Button createGameButton;
    [SerializeField] Button joinGameButton;

    private void Awake()
    {
        createGameButton.onClick.AddListener(() =>
        {
            GameManager.Instance.StartHost("Host");
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        });

        joinGameButton.onClick.AddListener(() =>
        {
            GameManager.Instance.StartClient();
        });
    }


}
