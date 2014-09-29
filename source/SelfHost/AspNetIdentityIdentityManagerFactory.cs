﻿using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Thinktecture.IdentityManager;

namespace SelfHost
{
    public class AspNetIdentityIdentityManagerFactory
    {
        static AspNetIdentityIdentityManagerFactory()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<IdentityDbContext>());

            //System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<CustomDbContext>());
        }

        string _connString;
        public AspNetIdentityIdentityManagerFactory(string connString)
        {
            _connString = connString;
        }
        
        public IIdentityManagerService Create()
        {
            var db = new IdentityDbContext<IdentityUser>(_connString);
            var userstore = new UserStore<IdentityUser>(db);
            var usermgr = new Microsoft.AspNet.Identity.UserManager<IdentityUser>(userstore);
            usermgr.PasswordValidator = new Microsoft.AspNet.Identity.PasswordValidator
            {
                RequiredLength = 3
            };
            var rolestore = new RoleStore<IdentityRole>(db);
            var rolemgr = new Microsoft.AspNet.Identity.RoleManager<IdentityRole>(rolestore);

            var svc = new Thinktecture.IdentityManager.AspNetIdentity.AspNetIdentityManagerService<IdentityUser, string, IdentityRole, string>(usermgr, rolemgr);

            var dispose = new DisposableIdentityManagerService(svc, db);
            return dispose;

            //var db = new CustomDbContext(_connString);
            //var userstore = new CustomUserStore(db);
            //var usermgr = new CustomUserManager(userstore);
            //var rolestore = new CustomRoleStore(db);
            //var rolemgr = new CustomRoleManager(rolestore);

            //var svc = new Thinktecture.IdentityManager.AspNetIdentity.AspNetIdentityManagerService<CustomUser, int, CustomRole, int>(usermgr, rolemgr);
            //var dispose = new DisposableIdentityManagerService(svc, db);
            //return dispose;
        }
    }
}