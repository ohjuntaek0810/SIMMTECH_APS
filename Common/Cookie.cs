using Google.Protobuf.WellKnownTypes;
using HS.Core;
using System.Net;
using System.Runtime.CompilerServices;

namespace HS.Web.Common
{
    public class User : Core.Cookie
    {
        public User()
        {
            Option.Timeout = 36;
            Option.Interval = CookieInerval.Hour;
            Option.IsCrypt = true;
            //Option.Domain = Variable.Domain;
            this.Init("User");
        }

        public string USER_ID
        {
            get
            {
                return this.Get("USER_ID");
            }
            set
            {
                this.Set("USER_ID", value);
            }
        }

        public string CLIENT
        {
            get
            {
                return this.Get("CLIENT");
            }
            set
            {
                this.Set("CLIENT", value);
            }
        }
        public string USER_NM
        {
            get
            {
                return this.Get("USER_NM");
            }
            set
            {
                this.Set("USER_NM", value);
            }
        }

    }  
}
