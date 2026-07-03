/// <summary>
/// Interface standar untuk semua objek yang bisa di-interaksi oleh Player.
/// Pasang interface ini ke script apapun yang ingin bisa di-interaksi.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Teks yang muncul di tombol Interact (contoh: "Ambil", "Bicara", "Buka")
    /// </summary>
    string InteractLabel { get; }

    /// <summary>
    /// Dipanggil saat Player menekan tombol Interact
    /// </summary>
    void OnInteract();
}
