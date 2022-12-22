using Android.Content;
using Android.OS;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthenticatorActivity : WebAuthenticatorCallbackActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // start the intermediate activity again with flags to close the custom tabs
        var intent = new Intent(this, typeof(BlossomAuthenticatorIntermediateActivity));
        intent.SetData(Intent.Data);
        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        StartActivity(intent);

        // finish this activity
        Finish();
    }
}
