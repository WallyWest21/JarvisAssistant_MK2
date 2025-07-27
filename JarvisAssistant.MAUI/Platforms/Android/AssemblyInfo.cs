using Android;
using Android.App;

// Assembly-level permissions for Android
[assembly: UsesPermission(Manifest.Permission.Internet)]
[assembly: UsesPermission(Manifest.Permission.AccessNetworkState)]
[assembly: UsesPermission(Manifest.Permission.RecordAudio)]

// Assembly-level features for Android
[assembly: UsesFeature("android.hardware.microphone", Required = false)]