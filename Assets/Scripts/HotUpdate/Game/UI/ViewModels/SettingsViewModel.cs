
    /// <summary>设置 ViewModel</summary>
    public class SettingsViewModel : ObservableObject
    {
        public BindableProperty<bool> SoundEnabled = new BindableProperty<bool>(true);
        public BindableProperty<bool> VibrationEnabled = new BindableProperty<bool>(true);

        public SettingsViewModel()
        {
            // 从 PlayerPrefs 加载设置
            SoundEnabled.Value = UnityEngine.PlayerPrefs.GetInt("setting_sound", 1) == 1;
            VibrationEnabled.Value = UnityEngine.PlayerPrefs.GetInt("setting_vibration", 1) == 1;

            SoundEnabled.OnChanged += v =>
                UnityEngine.PlayerPrefs.SetInt("setting_sound", v ? 1 : 0);
            VibrationEnabled.OnChanged += v =>
                UnityEngine.PlayerPrefs.SetInt("setting_vibration", v ? 1 : 0);
        }

        public void ResetAllProgress()
        {
            SaveManager.ClearAll();
        }

        public override void Dispose()
        {
            SoundEnabled.Clear();
            VibrationEnabled.Clear();
            base.Dispose();
        }
    }
