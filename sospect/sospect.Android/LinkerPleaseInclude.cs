using System;
using Android.Gms.Auth.Api.SignIn;
using Firebase.Messaging;

namespace sospect.Droid
{
    public class LinkerPleaseInclude
    {
        public void Include(Firebase.Messaging.FirebaseMessaging fm)
        {
            fm.AutoInitEnabled = fm.AutoInitEnabled;
        }

        public void Include(GoogleSignInAccount account)
        {
            var dummy = account.DisplayName;
        }

        public void Include(GoogleSignInOptions options)
        {
            var dummy = options.Account;
        }
    }
}

