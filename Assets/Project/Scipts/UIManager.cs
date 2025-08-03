using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;

    private void Start() {
        hostBtn.onClick.AddListener(OnHostButtonClicked);
        clientBtn.onClick.AddListener(OnClientButtonClicked);
    }

    private void OnDestroy() {
        hostBtn.onClick.RemoveListener(OnHostButtonClicked);
        clientBtn.onClick.RemoveListener(OnClientButtonClicked);
    }

    private void OnHostButtonClicked() {
        NetworkManager.Singleton.StartHost();
        panel.SetActive(false); // Host butonuna týklandýðýnda paneli gizle
    }

    private void OnClientButtonClicked() {
        NetworkManager.Singleton.StartClient();
        panel.SetActive(false); // Client butonuna týklandýðýnda paneli gizle
    }

}