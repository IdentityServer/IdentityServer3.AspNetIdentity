﻿using Owin;
using SelfHost.Config;
using Thinktecture.IdentityManager;
using Thinktecture.IdentityServer.Core.Configuration;

namespace SelfHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var connString = "AspId";
            //var connString = "CustomAspId";

            app.Map("/admin", adminApp =>
            {
                var factory = new AspNetIdentityIdentityManagerFactory(connString);
                adminApp.UseIdentityManager(new IdentityManagerConfiguration()
                {
                    IdentityManagerFactory = factory.Create
                });
            });

            var options = new IdentityServerOptions
            {
                IssuerUri = "https://idsrv3.com",
                SiteName = "Thinktecture IdentityServer v3 - UserService-AspNetIdentity",
                PublicHostName = "http://localhost:3333",

                SigningCertificate = Certificate.Get(),
                Factory = Factory.Configure(connString),
                CorsPolicy = CorsPolicy.AllowAll
            };

            app.UseIdentityServer(options);
        }
    }
}