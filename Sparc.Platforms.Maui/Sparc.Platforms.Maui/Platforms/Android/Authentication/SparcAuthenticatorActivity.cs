using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui
{
    public class SparcAuthenticatorActivity : Microsoft.Maui.Essentials.WebAuthenticatorCallbackActivity
    {
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// start the intermediate activity again with flags to close the custom tabs
			var intent = new Intent(this, typeof(SparcAuthenticatorIntermediateActivity));
			intent.SetData(Intent.Data);
			intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
			StartActivity(intent);

			// finish this activity
			Finish();
		}
	}
}
