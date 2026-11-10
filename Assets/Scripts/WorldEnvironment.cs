using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class WorldEnvironment : MonoBehaviour
{
    public static WorldEnvironment Instance { get; private set; }

    [SerializeField] int daysPerSeason = 3;
    [SerializeField] float bestSeasonBonus = 1.5f;
    [SerializeField] Tilemap[] tintedTilemaps;
    [SerializeField] Image weatherOverlay;
    [SerializeField] RainEffect rain;
    [SerializeField] TMP_Text infoText;

    static readonly string[] SeasonNames = { "Xuân", "Hạ", "Thu", "Đông" };
    static readonly string[] WeatherNames = { "Nắng", "Nhiều mây", "Mưa" };

    static readonly Color[] SeasonTints =
    {
        new Color(1.00f, 1.00f, 1.00f), // Xuân
        new Color(1.00f, 1.00f, 0.88f), // Hạ
        new Color(1.00f, 0.84f, 0.68f), // Thu
        new Color(0.82f, 0.90f, 1.00f)  // Đông
    };

    static readonly float[] SeasonGrowth = { 0.2f, 0f, 0f, -0.3f };

    // Trọng số [Nắng, Nhiều mây, Mưa] theo từng mùa
    static readonly int[][] WeatherWeights =
    {
        new[] { 50, 20, 30 },
        new[] { 70, 20, 10 },
        new[] { 40, 30, 30 },
        new[] { 40, 40, 20 }
    };

    public Season CurrentSeason { get; private set; }
    public Weather CurrentWeather { get; private set; }
    public int WeatherIndex => (int)CurrentWeather;

    int lastDay = -1;

    float GrowthRate =>
        SeasonGrowth[(int)CurrentSeason] + (CurrentWeather == Weather.Rain ? 0.5f : 0f);

    void Awake() => Instance = this;

    Season SeasonOf(int day)
    {
        int index = Mathf.Max(0, day - 1) / Mathf.Max(1, daysPerSeason);
        return (Season)(index % 4);
    }

    // SaveManager gọi sau khi đã nạp ngày
    public void Restore(int weatherIndex)
    {
        lastDay = DayCycle.Instance.Day;
        CurrentSeason = SeasonOf(lastDay);
        CurrentWeather = (Weather)Mathf.Clamp(weatherIndex, 0, 2);
        Apply();
    }

    void Update()
    {
        if (DayCycle.Instance == null) return;
        int day = DayCycle.Instance.Day;
        if (day != lastDay)
        {
            lastDay = day;
            CurrentSeason = SeasonOf(day);
            RollWeather();
            Apply();
        }

        float rate = GrowthRate;
        if (rate != 0f && Time.deltaTime > 0f)
            Growables.Shift(rate * Time.deltaTime);
    }

    void RollWeather()
    {
        var w = WeatherWeights[(int)CurrentSeason];
        int r = Random.Range(0, w[0] + w[1] + w[2]);
        CurrentWeather = r < w[0] ? Weather.Sunny
            : r < w[0] + w[1] ? Weather.Cloudy
            : Weather.Rain;
    }

    void Apply()
    {
        var tint = SeasonTints[(int)CurrentSeason];
        foreach (var tm in tintedTilemaps)
            if (tm != null) tm.color = tint;

        if (weatherOverlay != null)
        {
            weatherOverlay.color = CurrentWeather switch
            {
                Weather.Rain => new Color(0.30f, 0.36f, 0.48f, 0.30f),
                Weather.Cloudy => new Color(0.55f, 0.58f, 0.62f, 0.15f),
                _ => new Color(0f, 0f, 0f, 0f)
            };
        }

        if (rain != null) rain.SetRaining(CurrentWeather == Weather.Rain);
        RefreshInfo();
    }

    void RefreshInfo()
    {
        if (infoText == null) return;
        int pct = Mathf.RoundToInt(GrowthRate * 100f);
        string sign = pct > 0 ? "+" : "";
        infoText.text = $"{SeasonNames[(int)CurrentSeason]} - {WeatherNames[(int)CurrentWeather]}\nTốc độ lớn: {sign}{pct}%";
    }

    public float SellMultiplier(CropData crop) =>
        crop != null && crop.hasBestSeason && crop.bestSeason == CurrentSeason
            ? bestSeasonBonus : 1f;
}
