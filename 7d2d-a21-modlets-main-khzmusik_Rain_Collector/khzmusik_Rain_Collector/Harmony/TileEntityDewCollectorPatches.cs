using HarmonyLib;
using System.Reflection;

namespace RainCollector.Harmony
{
    /// <summary>
    /// Harmony patches for <see cref="TileEntityDewCollector"/>.
    /// </summary>
    public class TileEntityDewCollectorPatches
    {
        /// <summary>
        /// Holds information about the weather.
        /// </summary>
        public class WeatherInfo
        {
            /// <summary>
            /// The ID of the biome in which this weather occurs.
            /// </summary>
            public byte BiomeId { get; set; }

            /// <summary>
            /// True if these values represent incremental averages, false if they represent the
            /// current weather values.
            /// </summary>
            public bool IsAverage { get; set; }

            /// <summary>
            /// Fog density, 0..1.
            /// </summary>
            public float FogDensity { get; set; }

            /// <summary>
            /// Rainfall, 0..1.
            /// </summary>
            public float Rainfall { get; set; }

            /// <summary>
            /// Temperature.
            /// </summary>
            public float Temperature { get; set; }

            /// <summary>
            /// Number of <em>in-game</em> seconds since the last time the weather was checked.
            /// This is always current, it is never an incremental average.
            /// </summary>
            public float DeltaTime { get; set; }

            /// <summary>
            /// The average delta time.
            /// </summary>
            public float AvgDeltaTime { get; set; }

            /// <summary>
            /// The world time where this weather info was last updated.
            /// </summary>
            public ulong Updated { get; set; }

            /// <summary>
            /// Total time, in <em>real time</em> seconds, this weather was active.
            /// For current weather, this is <see cref="DeltaTime"/> converted to live seconds.
            /// </summary>
            public float TotalTime { get; set; }

            /// <summary>
            /// Total time, in <em>real time</em> seconds, this weather was above the minimum
            /// temperature needed for the dew collector to be active.
            /// </summary>
            public float AboveTemperatureTime { get; set; }

            /// <summary>
            /// The ratio of time spent above the minimum conversion temperature.
            /// </summary>
            public float AboveTemperatureRatio => AboveTemperatureTime / TotalTime;

            /// <summary>
            /// Whether this object has been initialized with weather information.
            /// Only objects representing incremental averages need initialization.
            /// </summary>
            public bool Initialized => !IsAverage ||
                !(FogDensity == default && Rainfall == default && Temperature == default);

            /// <summary>
            /// Deconstruct into fog density, rainfall, temperature, and delta time.
            /// </summary>
            /// <param name="fogDensity"></param>
            /// <param name="rainfall"></param>
            /// <param name="temperature"></param>
            /// <param name="deltaTime"></param>
            public void Deconstruct(
                out float fogDensity,
                out float rainfall,
                out float temperature,
                out float deltaTime)
            {
                fogDensity = FogDensity;
                rainfall = Rainfall;
                temperature = Temperature;
                deltaTime = DeltaTime;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $@"(Biome {BiomeId
                    }) Rainfall = {Rainfall
                    } / Fog Density = {FogDensity
                    } / Temperature = {Temperature}{
                    (IsAverage ? " (inc avg) " : string.Empty)}";
            }
        }

        /// <summary>
        /// Patches <see cref="TileEntityDewCollector.HandleUpdate(World)"/> to handle the
        /// additional features of this mod.
        /// </summary>
        [HarmonyPatch(typeof(TileEntityDewCollector), nameof(TileEntityDewCollector.HandleUpdate))]
        public class HandleUpdate
        {
            /// <summary>
            /// Fog Convert Multiplier property name.
            /// </summary>
            public const string PropFogConvertMultiplier = "FogConvertMultiplier";

            /// <summary>
            /// Minimum Convert Temperature property name.
            /// </summary>
            public const string PropMinConvertTemperature = "MinConvertTemperature";

            /// <summary>
            /// Rain Convert Multiplier property name.
            /// </summary>
            public const string PropRainConvertMultiplier = "RainConvertMultiplier";

            /// <summary>
            /// Container size property name.
            /// </summary>
            public const string PropContainerSize = "ContainerSize";

            private static bool initialized = false;
            private static bool enabled = false;

            private static float fogConvertMultiplier = 0;
            private static float minConvertTemperature = float.MinValue;
            private static float rainConvertMultiplier = 0;
            private static Vector2i containerSize = Vector2i.zero;

            // This is a reference to the protected TileEntity.setModified method -
            // needed if we add additional fill time and/or create water
            private static MethodInfo setModified = null;

            // Incremental averages of water info values, indexed by biome ID
            private static WeatherInfo[] biomeWeatherInfo = null;

            /// <summary>
            /// Harmony prefix for <see cref="TileEntityDewCollector.HandleUpdate(World)"/>
            /// that saves the last world time as state, and bypasses the method if the temperature
            /// check fails.
            /// </summary>
            /// <param name="world"></param>
            /// <param name="__instance"></param>
            /// <param name="___lastWorldTime"></param>
            /// <param name="__state"></param>
            /// <returns></returns>
            public static bool Prefix(
                World world,
                TileEntityDewCollector __instance,
                ulong ___lastWorldTime,
                out WeatherInfo __state)
            {
                __state = null;

                if (!initialized)
                {
                    Initialize(__instance.blockValue.Block);
                }

                // If the XML sets the container size, then do it even if it's otherwise disabled
                if (containerSize != Vector2i.zero && containerSize != __instance.GetContainerSize())
                {
                    SetContainerSize(__instance);
                }

                if (!enabled)
                {
                    return true;
                }

                float deltaTime = (___lastWorldTime != 0UL && ___lastWorldTime < world.worldTime)
                    ? GameUtils.WorldTimeToTotalSeconds(world.worldTime - ___lastWorldTime)
                    : 0f;

                if (deltaTime <= 0f)
                {
                    return true;
                }

                __state = CalculateWeatherInfo(__instance, deltaTime);

                // Run the original method only if it's above the minimum conversion temperature.
                return __state.Temperature >= minConvertTemperature;
            }

            /// <summary>
            /// Harmony postfix for <see cref="TileEntityDewCollector.HandleUpdate(World)"/>
            /// that handles adding additional fill time due to fog density and rainfall.
            /// </summary>
            /// <param name="world"></param>
            /// <param name="__instance"></param>
            /// <param name="__state"></param>
            /// <param name="___lastWorldTime"></param>
            public static void Postfix(
                World world,
                TileEntityDewCollector __instance,
                WeatherInfo __state,
                ref ulong ___lastWorldTime)
            {
                if (!enabled)
                {
                    return;
                }

                if (__state == null)
                {
                    return;
                }

                if (__instance.IsBlocked)
                {
                    return;
                }

                if (__instance.blockValue.Block.IsUnderwater(
                    GameManager.Instance.World,
                    __instance.ToWorldPos(),
                    __instance.blockValue))
                {
                    return;
                }

                // Update the world times now, in case the original method was skipped.
                ___lastWorldTime = __instance.worldTimeTouched = world.worldTime;

                var (fogDensity, rainfall, temperature, deltaTime) = __state;

                if (fogDensity <= 0 && rainfall <= 0)
                {
                    return;
                }

                var additionalTime = deltaTime * fogDensity * fogConvertMultiplier;
                additionalTime += deltaTime * rainfall * rainConvertMultiplier;
                
                if (temperature < minConvertTemperature)
                {
                    // If this is the current weather, don't add anything.
                    if (!__state.IsAverage)
                    {
                        RainCollector.DebugLog($@"({__instance.localChunkPos
                            }) Temperature is {temperature
                            }, minimum converstion temperature is {minConvertTemperature}");
                        
                        return;
                    }

                    // Approximate how much would have been added, taking temperature into account.
                    // Since we skipped the original, add the vanilla amount, then multiply by the
                    // relative amount of time spent above the minimum conversion temperature.
                    additionalTime += deltaTime;
                    additionalTime *= __state.AboveTemperatureRatio;
                }

                RainCollector.DebugLog($@"({__instance.localChunkPos}) Adding {additionalTime
                    } to {(temperature < minConvertTemperature ? 0 : deltaTime)}. {__state}");

                var currentIndex = __instance.CurrentIndex;
                if (currentIndex == -1)
                {
                    // Add any extra time to the leftover time, it will be handled next update
                    __instance.leftoverTime += additionalTime;
                    return;
                }

                // Copied nearly verbatim from the vanilla code.
                __instance.fillValues[currentIndex] += additionalTime;
                if (__instance.fillValues[currentIndex] >= __instance.CurrentConvertTime)
                {
                    __instance.leftoverTime = __instance.fillValues[currentIndex] - __instance.CurrentConvertTime;
                    __instance.items[currentIndex] = new ItemStack(new ItemValue(__instance.ConvertToItem.Id, false), 1);
                    __instance.fillValues[currentIndex] = -1f;
                    __instance.CurrentConvertTime = -1f;
                    __instance.CurrentIndex = -1;
                }

                // This sends another message over the wire but I don't think it can be avoided.
                setModified?.Invoke(__instance, null);
            }

            /// <summary>
            /// Initializes the mod using the block properties, if it hasn't already been initialized,
            /// and returns true if the mod is enabled.
            /// </summary>
            /// <param name="block"></param>
            /// <returns></returns>
            public static bool Initialize(Block block)
            {
                if (initialized)
                {
                    return enabled;
                }

                // In multiplayer games, this should only be done on the server, otherwise extra
                // fill time will be added for each player
                if (!(SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer
                    || GameManager.IsDedicatedServer))
                {
                    RainCollector.DebugLog("Calculations must be done on the server in MP games");
                    initialized = true;
                    return enabled = false;
                }

                block.Properties.ParseFloat(PropFogConvertMultiplier, ref fogConvertMultiplier);
                block.Properties.ParseFloat(PropMinConvertTemperature, ref minConvertTemperature);
                block.Properties.ParseFloat(PropRainConvertMultiplier, ref rainConvertMultiplier);

                if (block.Properties.Values.TryGetString(PropContainerSize, out var size))
                {
                    containerSize = StringParsers.ParseVector2i(size);
                }

                if (fogConvertMultiplier == 0 &&
                    rainConvertMultiplier == 0 &&
                    minConvertTemperature == float.MinValue)
                {
                    initialized = true;
                    return enabled = false;
                }

                setModified = typeof(TileEntity).GetMethod(
                        "setModified",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                // Initialize the array of average water info by biome
                var maxBiomeId = GetMaxBiomeId();
                biomeWeatherInfo = new WeatherInfo[maxBiomeId + 1];
                for (var i = 0; i <= maxBiomeId; i++)
                {
                    biomeWeatherInfo[i] = new WeatherInfo
                    {
                        BiomeId = (byte)i,
                        IsAverage = true
                    };
                }

                // We want to print this message even if we're not debugging
                Log.Out($@"RainCollector initialized: {
                    PropFogConvertMultiplier}={fogConvertMultiplier} / {
                    PropRainConvertMultiplier}={rainConvertMultiplier} / {
                    PropMinConvertTemperature}={minConvertTemperature}");

                initialized = true;
                return enabled = true;
            }

            private static float CalculateLiveSeconds(float deltaTime)
            {
                var incPerSec = GameStats.GetInt(EnumGameStats.TimeOfDayIncPerSec);

                // As determined through testing, for some reason this can have different values
                var divisor = (incPerSec > 13 ? 7.02f : 7.2f) / 2.0f;

                var seconds = deltaTime / (incPerSec * divisor);

                return seconds;
            }

            private static WeatherInfo CalculateWeatherInfo(TileEntity tileEntity, float deltaTime)
            {
                var biomeId = GetBiomeId(tileEntity);

                var avgWeather = biomeWeatherInfo[biomeId];

                var msgPrefix = $"({tileEntity.localChunkPos}) Using biome average weather";

                // If the dew collector isn't in the current biome, use the incremental average.
                // BUT, the current biome is always the forest biome while in the grace period.
                if (!WeatherManager.inWeatherGracePeriod && !IsCurrentWeatherBiome(biomeId))
                {
                    RainCollector.DebugLog($@"{msgPrefix}: {biomeId} is not the current biome ID");
                    return avgWeather;
                }

                var liveSeconds = CalculateLiveSeconds(deltaTime);

                var currentWeather = GetCurrentWeatherInfo(biomeId, deltaTime, liveSeconds);

                UpdateAverageWeather(avgWeather, currentWeather);

                // Dew collector updates happen roughly every two real-time seconds.
                // If it's been more than three seconds since the last check, assume we're
                // returning to the chunk, and use the incremental average.
                if (liveSeconds > 3)
                {
                    RainCollector.DebugLog($@"{msgPrefix}: live seconds = {liveSeconds}");
                    return avgWeather;
                }

                return currentWeather;
            }

            private static byte GetBiomeId(TileEntity tileEntity)
            {
                var worldPosition = tileEntity.ToWorldPos();

                return tileEntity.GetChunk().GetBiomeId(
                    World.toBlockXZ(worldPosition.x),
                    World.toBlockXZ(worldPosition.z));
            }

            private static byte GetCurrentWeatherBiomeId()
            {
                // In the unlikely event the weather isn't initialized, use the pine forest ID.
                return WeatherManager.currentWeather?.biomeDefinition?.m_Id ?? 3;
            }

            private static WeatherInfo GetCurrentWeatherInfo(byte tileEntityBiomeId, float deltaTime, float liveSeconds)
            {
                return new WeatherInfo
                {
                    BiomeId = WeatherManager.inWeatherGracePeriod
                        ? tileEntityBiomeId
                        : GetCurrentWeatherBiomeId(),
                    FogDensity = SkyManager.GetFogDensity(),
                    Rainfall = WeatherManager.Instance.GetCurrentRainfallValue(),
                    Temperature = WeatherManager.inWeatherGracePeriod
                        ? GetGracePeriodTemperature(tileEntityBiomeId)
                        : WeatherManager.Instance.GetCurrentTemperatureValue(),
                    DeltaTime = deltaTime,
                    Updated = GameManager.Instance.World.worldTime,
                    TotalTime = liveSeconds
                };
            }

            // Adopted from WeatherManager.CurrentWeatherFromNearBiomesFrameUpdate
            private static float GetGracePeriodTemperature(byte biomeId)
            {
                // snow
                if (biomeId == 1)
                {
                    return 45f;
                }
                // pine_forest and whatever would be in ID 2 if it existed
                if (biomeId - 2 <= 1)
                {
                    return 60f;
                }
                // Everything else: desert, burnt_forest, wasteland, etc.
                return 70f;
            }

            private static byte GetMaxBiomeId()
            {
                byte maxId = byte.MinValue;

                var count = WeatherManager.Instance.biomeWeather.Count;

                for (var i = 0; i < count; i++)
                {
                    var id = WeatherManager.Instance.biomeWeather[i].biomeDefinition.m_Id;
                    if (id > maxId)
                    {
                        maxId = id;
                    }
                }

                return maxId;
            }

            private static bool IsCurrentWeatherBiome(byte biomeId)
            {
                return GetCurrentWeatherBiomeId() == biomeId;
            }

            private static void SetContainerSize(TileEntityDewCollector __instance)
            {
                // The SetContainerSize method doesn't change the fill values array
                // (this is probably a TFP bug)
                __instance.fillValues = new float[containerSize.x * containerSize.y];
                __instance.SetContainerSize(containerSize, true);

                RainCollector.DebugLog($"Container size on {__instance.EntityId} set to {containerSize}");
            }

            private static void UpdateAverageWeather(WeatherInfo average, WeatherInfo current)
            {
                var msgPrefix = $"Average weather";

                // Throttle updating to 1.5 real-time seconds.
                if (average.Initialized &&
                    1.5 > CalculateLiveSeconds(
                        GameUtils.WorldTimeToTotalSeconds(current.Updated - average.Updated)))
                {
                    return;
                }

                var isWarmEnough = current.Temperature > minConvertTemperature;

                average.DeltaTime = current.DeltaTime;
                average.Updated = current.Updated;
                average.TotalTime += current.TotalTime;
                average.AboveTemperatureTime += isWarmEnough ? current.TotalTime : 0;

                if (!average.Initialized)
                {
                    // Substitute the delta time for the two-period averge delta time (see below)
                    average.AvgDeltaTime = (2 * current.DeltaTime) / current.TotalTime;

                    // Copy the current weather for the other initial values
                    average.Temperature = current.Temperature;
                    average.FogDensity = current.FogDensity;
                    average.Rainfall = current.Rainfall;

                    RainCollector.DebugLog($"{msgPrefix} initialized: {average}");

                    return;
                }

                // This is the "two-period" average delta time, used in the formulas below.
                // Dividing by the total time (real world time) works out because it's ~2 seconds
                // between updates, and anything longer than that should make up for the longer
                // current delta time if this is from returning to a chunk.
                var avgDeltaTime = (average.AvgDeltaTime + current.DeltaTime) / current.TotalTime;
                
                average.AvgDeltaTime = avgDeltaTime;

                average.Temperature += (current.Temperature - average.Temperature) / avgDeltaTime;

                // Fog density and rainfall should only be updated if it's currently above the
                // minimum temperature required for the dew collector to convert.
                if (isWarmEnough)
                {
                    average.FogDensity += (current.FogDensity - average.FogDensity) / avgDeltaTime;
                    average.Rainfall += (current.Rainfall - average.Rainfall) / avgDeltaTime;
                }

                RainCollector.DebugLog($"{msgPrefix} updated: {average}");
            }
        }
    }
}
