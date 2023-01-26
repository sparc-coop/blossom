using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Sparc.Blossom.Authentication;

[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
class BlossomAuthenticatorIntermediateActivity : Activity
{
	const string launchedExtra = "launched";
	const string actualIntentExtra = "actual_intent";

	bool launched;
	Intent? actualIntent;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		var extras = savedInstanceState ?? Intent?.Extras;

		// read the values
		launched = extras?.GetBoolean(launchedExtra, false) ?? false;
		actualIntent = extras?.GetParcelable(actualIntentExtra) as Intent;
	}

	protected override void OnResume()
	{
		base.OnResume();

		if (actualIntent != null && !launched)
		{
			// if this is the first time, start the authentication flow
			StartActivity(actualIntent);

			launched = true;
		}
		else
		{
			// otherwise, resume the auth flow and finish this activity
			BlossomAuthenticator.OnResume(Intent);

			Finish();
		}
	}

	protected override void OnNewIntent(Intent intent)
	{
		base.OnNewIntent(intent);

		Intent = intent;
	}

	protected override void OnSaveInstanceState(Bundle outState)
	{
		// save the values
		outState.PutBoolean(launchedExtra, launched);
		outState.PutParcelable(actualIntentExtra, actualIntent);

		base.OnSaveInstanceState(outState);
	}

	public static void StartActivity(Activity activity, Intent intent)
	{
		var intermediateIntent = new Intent(activity, typeof(BlossomAuthenticatorIntermediateActivity));
		intermediateIntent.PutExtra(actualIntentExtra, intent);

		activity.StartActivity(intermediateIntent);
	}
}
