using Microsoft.AspNet.Identity.EntityFramework;
using Thinktecture.IdentityManager.Core;

namespace Thinktecture.IdentityManager.Host
{
    public class AspNetIdentityIdentityManagerFactory
    {
        static AspNetIdentityIdentityManagerFactory()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<IdentityDbContext>());
            
            //System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<SelfHost.CustomDbContext>());
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
            mgr.PasswordValidator = new Microsoft.AspNet.Identity.PasswordValidator
            {
                RequiredLength = 3
            };
            return new Thinktecture.IdentityManager.AspNetIdentity.IdentityManagerService<IdentityUser, string>(mgr, db);

            //var db = new SelfHost.CustomDbContext("CustomAspId");
            //var store = new SelfHost.CustomUserStore(db);
            //var mgr = new SelfHost.CustomUserManager(store);
            //return new Thinktecture.IdentityManager.AspNetIdentity.IdentityManagerService<SelfHost.CustomUser, int>(mgr, db);
        }
    }
}