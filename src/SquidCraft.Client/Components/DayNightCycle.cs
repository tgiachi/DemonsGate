// using Microsoft.Xna.Framework;

// namespace SquidCraft.Client.Components
// {
//     public class DayNightCycle
//     {
//         private float _timeOfDay = 0.25f; // Start at morning (6 AM)
//         private const float DayLength = 24000f; // 24 minutes real time = 1 day

//         public float TimeOfDay => _timeOfDay;

//         public void Update(GameTime gameTime)
//         {
//             // Normal speed: 1 real second = 1000 game ticks = ~24 minutes per day
//             _timeOfDay += (float)gameTime.ElapsedGameTime.TotalSeconds * 1000f / DayLength;
//             if (_timeOfDay >= 1.0f) _timeOfDay -= 1.0f;
//         }

//         public Color GetSunColor()
//         {
//             // Time of day: 0.0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset, 1.0 = midnight

//             if (_timeOfDay < 0.2f || _timeOfDay > 0.8f)
//             {
//                 // Night: very blue and dark
//                 return new Color(0.0f, 0.0f, 0.5f);
//             }
//             else if (_timeOfDay >= 0.2f && _timeOfDay <= 0.3f)
//             {
//                 // Sunrise: bright orange-red
//                 var t = (_timeOfDay - 0.2f) / 0.1f;
//                 return Color.Lerp(new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f), t);
//             }
//             else if (_timeOfDay >= 0.7f && _timeOfDay <= 0.8f)
//             {
//                 // Sunset: bright orange-red
//                 var t = (_timeOfDay - 0.7f) / 0.1f;
//                 return Color.Lerp(new Color(1.0f, 0.5f, 0.0f), new Color(1.0f, 0.0f, 0.0f), t);
//             }
//             else
//             {
//                 // Day: bright yellow-white
//                 return new Color(1.0f, 1.0f, 0.0f);
//             }
//         }

//         public float GetSunIntensity()
//         {
//             // Sun intensity varies with time of day
//             if (_timeOfDay < 0.2f || _timeOfDay > 0.8f)
//             {
//                 // Night: very low intensity
//                 return 0.1f;
//             }
//             else if (_timeOfDay >= 0.2f && _timeOfDay <= 0.3f)
//             {
//                 // Sunrise: increasing intensity
//                 var t = (_timeOfDay - 0.2f) / 0.1f;
//                 return MathHelper.Lerp(0.1f, 1.0f, t);
//             }
//             else if (_timeOfDay >= 0.7f && _timeOfDay <= 0.8f)
//             {
//                 // Sunset: decreasing intensity
//                 var t = (_timeOfDay - 0.7f) / 0.1f;
//                 return MathHelper.Lerp(1.0f, 0.1f, t);
//             }
//             else
//             {
//                 // Day: full intensity
//                 return 1.0f;
//             }
//         }

//         public Vector3 GetSunDirection()
//         {
//             // Sun moves from east to west
//             var sunAngle = _timeOfDay * MathHelper.TwoPi - MathHelper.PiOver2; // Start at sunrise
//             var sunX = MathF.Cos(sunAngle);
//             var sunY = MathF.Sin(sunAngle);
//             var sunZ = 0f; // Sun moves north-south, but we'll keep it simple

//             return new Vector3(sunX, sunY, sunZ);
//         }
//     }
// }