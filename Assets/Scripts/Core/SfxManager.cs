using UnityEngine;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Button SFX")]
    [SerializeField] AudioClip buttonClickClip;   
    [Range(0f, 1f)] [SerializeField] float buttonClickVolume = 0.7f;
    [SerializeField] float buttonClickPitchMin = 0.95f;
    [SerializeField] float buttonClickPitchMax = 1.05f;

    [Header("Block SFX")]
    [SerializeField] AudioClip blockPickupClip;   
    [SerializeField] AudioClip blockReleaseClip; 

    [Header("Screw SFX")]
    [SerializeField] AudioClip screwUnlockClip;   

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] float blockPickupVolume  = 0.55f;
    [Range(0f, 1f)] [SerializeField] float blockReleaseVolume = 0.50f;
    [Range(0f, 1f)] [SerializeField] float screwUnlockVolume  = 0.85f;

    [Header("Pitch Randomization")]
    [SerializeField] float pickupPitchMin  = 0.95f;
    [SerializeField] float pickupPitchMax  = 1.10f;
    [SerializeField] float releasePitchMin = 0.92f;
    [SerializeField] float releasePitchMax = 1.08f;
    [SerializeField] float unlockPitchMin  = 0.90f;
    [SerializeField] float unlockPitchMax  = 1.10f;

    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop        = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayButtonClick()
    {
        PlayOneShot(buttonClickClip, buttonClickVolume,
                    Random.Range(buttonClickPitchMin, buttonClickPitchMax));
    }

    public void PlayBlockPickup()
    {
        PlayOneShot(blockPickupClip, blockPickupVolume,
                    Random.Range(pickupPitchMin, pickupPitchMax));
    }

    public void PlayBlockRelease()
    {
        PlayOneShot(blockReleaseClip, blockReleaseVolume,
                    Random.Range(releasePitchMin, releasePitchMax));
    }

    public void PlayScrewUnlock()
    {
        PlayOneShot(screwUnlockClip, screwUnlockVolume,
                    Random.Range(unlockPitchMin, unlockPitchMax));
    }

    void PlayOneShot(AudioClip clip, float volume, float pitch = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }
}
