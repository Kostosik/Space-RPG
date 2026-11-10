
namespace GameProject.Settings;

public sealed class AudioSettings
{
    private float _masterVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _effectsVolume = 1.0f;

    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = ValidateVolume(
            value,
            nameof(MasterVolume));
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = ValidateVolume(
            value,
            nameof(MusicVolume));
    }

    public float EffectsVolume
    {
        get => _effectsVolume;
        set => _effectsVolume = ValidateVolume(
            value,
            nameof(EffectsVolume));
    }

    public AudioSettings CreateCopy()
    {
        return new AudioSettings
        {
            MasterVolume = MasterVolume,
            MusicVolume = MusicVolume,
            EffectsVolume = EffectsVolume
        };
    }

    private static float ValidateVolume(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0.0f || value > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                name,
                "Volume must be between 0 and 1.");
        }

        return value;
    }
}
