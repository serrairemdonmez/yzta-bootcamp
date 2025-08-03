using UnityEngine;

public class Bilboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    private void Start() {
        // Ana kameray� bul. Daha performansl� bir ��z�m i�in GameManager'da referans tutulabilir.
        if (Camera.main != null) {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate() {
        // E�er kamera bulunmu�sa, bu objenin rotasyonunu kameran�n rotasyonuyla ayn� yap.
        if (mainCameraTransform != null) {
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}
