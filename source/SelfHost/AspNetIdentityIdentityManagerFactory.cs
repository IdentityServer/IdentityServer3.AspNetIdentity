using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Thinktecture.IdentityManager.Core;

namespace Thinktecture.IdentityManager.Host
{
    public class AspNetIdentityIdentityManagerFactory
    {
        static AspNetIdentityIdentityManagerFactory()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<IdentityDbContext>());
            
            //System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<CustomDbContext>());
        }

        string connString;
        public AspNetIdentityIdentityManagerFactory(string connString)
        {
            this.connString = connString;
        }
        
        public IIdentityManagerService Create()
        {
            var db = new IdentityDbContext<IdentityUser>(this.connString);
            var store = new UserStore<IdentityUser>(db);
            var mgr = new Microsoft.AspNet.Identity.UserManager<IdentityUser>(store);
            return new Thinktecture.IdentityManager.AspNetIdentity.IdentityManagerService<IdentityUser, string>(mgr, db);

            //var db = new CustomDbContext("CustomAspId");
            //var store = new CustomUserStore(db);
            //var mgr = new CustomUserManager(store);
            //return new Thinktecture.IdentityManager.AspNetIdentity.UserManager<CustomUser, int, CustomUserLogin, CustomUserRole, CustomUserClaim>(mgr, db);
        }
    }
}