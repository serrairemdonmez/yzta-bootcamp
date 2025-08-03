using UnityEngine;

public class Bilboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    private void Start() {
        // Ana kamerayý bul. Daha performanslý bir çözüm için GameManager'da referans tutulabilir.
        if (Camera.main != null) {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate() {
        // Eðer kamera bulunmuþsa, bu objenin rotasyonunu kameranýn rotasyonuyla ayný yap.
        if (mainCameraTransform != null) {
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}
