using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace ArObjectDetector
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Zkontrolovat oprávnění při startu
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Android.Manifest.Permission.Camera }, 100);
            }
        }
    }
}
