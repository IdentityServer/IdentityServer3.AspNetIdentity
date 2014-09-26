/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Owin;
using SelfHost.Config;
using Thinktecture.IdentityManager;
using Thinktecture.IdentityServer.Core.Configuration;

namespace SelfHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/admin", adminApp =>
            {
                var factory = new Thinktecture.IdentityManager.Host.AspNetIdentityIdentityManagerFactory("AspId");
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
                Factory = Factory.Configure("AspId"),
                CorsPolicy = CorsPolicy.AllowAll
            };

            app.UseIdentityServer(options);
        }
    }
}