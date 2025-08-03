using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerCameraSetup : NetworkBehaviour
{
    private CinemachineCamera cinemachineCam;

    public override void OnNetworkSpawn() {
        // Bu kod, bu objenin ağ üzerinde oluşturulduğu anda çalışır.
        cinemachineCam = GetComponent<CinemachineCamera>();

        if (IsOwner) {
            // Eğer bu obje bana aitse (benim kontrol ettiğim oyuncuysa),
            // sanal kamerayı aktif et ve ona yüksek öncelik ver.
            // Yüksek öncelik, Cinemachine Brain'in bu kamerayı seçmesini garantiler.
            cinemachineCam.Priority = 100;
        }
        else {
            // Eğer bu obje bana ait değilse (başka bir oyuncunun karakteriyse),
            // onun sanal kamerasını benim oyunumda devre dışı bırak.
            cinemachineCam.enabled = false;
        }
    }
}