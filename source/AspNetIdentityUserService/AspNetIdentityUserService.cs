﻿/*
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

using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services;
using Thinktecture.IdentityServer.Core.Extensions;
using Thinktecture.IdentityModel;
using Thinktecture.IdentityServer.Core;

namespace Thinktecture.IdentityServer.AspNetIdentity
{
    public class AspNetIdentityUserService<TUser, TKey> : IUserService, IDisposable
        where TUser : class, IUser<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        protected readonly UserManager<TUser, TKey> userManager;
        IDisposable cleanup;

        protected readonly Func<string, TKey> ConvertSubjectToKey;
        
        public AspNetIdentityUserService(UserManager<TUser, TKey> userManager, IDisposable cleanup = null, Func<string, TKey> parseSubject = null)
        {
            if (userManager == null) throw new ArgumentNullException("userManager");
            
            this.userManager = userManager;
            this.cleanup = cleanup;

            if (parseSubject != null)
            {
                ConvertSubjectToKey = parseSubject;
            }
            else
            {
                var keyType = typeof (TKey);
                if (keyType == typeof (string)) ConvertSubjectToKey = subject => (TKey) ParseString(subject);
                else if (keyType == typeof (int)) ConvertSubjectToKey = subject => (TKey) ParseInt(subject);
                else if (keyType == typeof (uint)) ConvertSubjectToKey = subject => (TKey) ParseUInt32(subject);
                else if (keyType == typeof (long)) ConvertSubjectToKey = subject => (TKey) ParseLong(subject);
                else if (keyType == typeof (Guid)) ConvertSubjectToKey = subject => (TKey) ParseGuid(subject);
                else
                {
                    throw new InvalidOperationException("Key type not supported");
                }
            }
        }

        object ParseString(string sub)
        {
            return sub;
        }
        object ParseInt(string sub)
        {
            int key;
            if (!Int32.TryParse(sub, out key)) return 0;
            return key;
        }
        object ParseUInt32(string sub)
        {
            uint key;
            if (!UInt32.TryParse(sub, out key)) return 0;
            return key;
        }
        object ParseLong(string sub)
        {
            long key;
            if (!Int64.TryParse(sub, out key)) return 0;
            return key;
        }
        object ParseGuid(string sub)
        {
            Guid key;
            if (!Guid.TryParse(sub, out key)) return Guid.Empty;
            return key;
        }
        
        public virtual void Dispose()
        {
            if (this.cleanup != null)
            {
                this.cleanup.Dispose();
                this.cleanup = null;
            }
        }

        public virtual async Task<IEnumerable<Claim>> GetProfileDataAsync(
            ClaimsPrincipal subject,
            IEnumerable<string> requestedClaimTypes = null)
        {
            if (subject == null) throw new ArgumentNullException("subject");

            TKey key = ConvertSubjectToKey(subject.GetSubjectId());
            var acct = await userManager.FindByIdAsync(key);
            if (acct == null)
            {
                throw new ArgumentException("Invalid subject identifier");
            }

            var claims = await GetClaimsFromAccount(acct);
            if (requestedClaimTypes != null)
            {
                claims = claims.Where(x => requestedClaimTypes.Contains(x.Type));
            }
            return claims;
        }

        protected virtual async Task<IEnumerable<Claim>> GetClaimsFromAccount(TUser user)
        {
            var claims = new List<Claim>{
                new Claim(Thinktecture.IdentityServer.Core.Constants.ClaimTypes.Subject, user.Id.ToString()),
            };

            if (userManager.SupportsUserEmail)
            {
                var email = await userManager.GetEmailAsync(user.Id);
                if (!String.IsNullOrWhiteSpace(email))
                {
                    claims.Add(new Claim(Thinktecture.IdentityServer.Core.Constants.ClaimTypes.Email, email));
                    var verified = await userManager.IsEmailConfirmedAsync(user.Id);
                    claims.Add(new Claim(Thinktecture.IdentityServer.Core.Constants.ClaimTypes.EmailVerified, verified ? "true" : "false"));
                }
            }

            if (userManager.SupportsUserPhoneNumber)
            {
                var phone = await userManager.GetPhoneNumberAsync(user.Id);
                if (!String.IsNullOrWhiteSpace(phone))
                {
                    claims.Add(new Claim(Thinktecture.IdentityServer.Core.Constants.ClaimTypes.PhoneNumber, phone));
                    var verified = await userManager.IsPhoneNumberConfirmedAsync(user.Id);
                    claims.Add(new Claim(Thinktecture.IdentityServer.Core.Constants.ClaimTypes.PhoneNumberVerified, verified ? "true" : "false"));
                }
            }

            if (userManager.SupportsUserClaim)
            {
                claims.AddRange(await userManager.GetClaimsAsync(user.Id));
            }

            if (userManager.SupportsUserRole)
            {
                var roleClaims =
                    from role in await userManager.GetRolesAsync(user.Id)
                    select new Claim(Thinktecture.IdentityServer.Core.Constants.ClaimTypes.Role, role);
                claims.AddRange(roleClaims);
            }

            return claims;
        }

        protected virtual async Task<string> GetDisplayNameForAccountAsync(TKey userID)
        {
            if (userManager.SupportsUserClaim)
            {            
                var claims = await userManager.GetClaimsAsync(userID);
                var nameClaim = claims.FirstOrDefault(x => x.Type == Thinktecture.IdentityServer.Core.Constants.ClaimTypes.Name);
                if (nameClaim == null) nameClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
                if (nameClaim != null) return nameClaim.Value;
            }
            
            var user = await userManager.FindByIdAsync(userID);
            return user.UserName;
        }

        public virtual Task<AuthenticateResult> PreAuthenticateAsync(SignInMessage message)
        {
            return Task.FromResult<AuthenticateResult>(null);
        }

        protected async virtual Task<TUser> FindUserAsync(string username)
        {
            return await userManager.FindByNameAsync(username);
        }

        protected virtual Task<AuthenticateResult> PostAuthenticateLocalAsync(TUser account, SignInMessage message)
        {
            //if (await userManager.GetTwoFactorEnabledAsync(userId) &&
            //    !await AuthenticationManager.TwoFactorBrowserRememberedAsync(user.Id))
            //{
            //    var identity = new ClaimsIdentity(DefaultAuthenticationTypes.TwoFactorCookie);
            //    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            //    AuthenticationManager.SignIn(identity);
            //    return SignInStatus.RequiresTwoFactorAuthentication;
            //}

            return Task.FromResult<AuthenticateResult>(null);
        }
        
        public virtual async Task<AuthenticateResult> AuthenticateLocalAsync(string username, string password, SignInMessage message = null)
        {
            if (!userManager.SupportsUserPassword)
            {
                return null;
            }

            var user = await FindUserAsync(username);
            if (user == null)
            {
                return null;
            }

            if (userManager.SupportsUserLockout &&
                await userManager.IsLockedOutAsync(user.Id))
            {
                return null;
            }

            if (await userManager.CheckPasswordAsync(user, password))
            {
                if (userManager.SupportsUserLockout)
                {
                    userManager.ResetAccessFailedCount(user.Id);
                }

                var result = await PostAuthenticateLocalAsync(user, message);
                if (result != null) return result;

                return new AuthenticateResult(user.Id.ToString(), await GetDisplayNameForAccountAsync(user.Id));
            }
            else if (userManager.SupportsUserLockout)
            {
                await userManager.AccessFailedAsync(user.Id);
            }

            return null;
        }

        public virtual async Task<AuthenticateResult> AuthenticateExternalAsync(ExternalIdentity externalUser, SignInMessage message)
        {
            if (externalUser == null)
            {
                throw new ArgumentNullException("externalUser");
            }

            var user = await userManager.FindAsync(new UserLoginInfo(externalUser.Provider, externalUser.ProviderId));
            if (user == null)
            {
                return await ProcessNewExternalAccountAsync(externalUser.Provider, externalUser.ProviderId, externalUser.Claims);
            }
            else
            {
                return await ProcessExistingExternalAccountAsync(user.Id, externalUser.Provider, externalUser.ProviderId, externalUser.Claims);
            }
        }

        protected virtual async Task<AuthenticateResult> ProcessNewExternalAccountAsync(string provider, string providerId, IEnumerable<Claim> claims)
        {
            var user = await CreateNewAccountFromExternalProviderAsync(provider, providerId, claims);
            if (user == null) throw new InvalidOperationException("CreateNewAccountFromExternalProvider returned null");

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return new AuthenticateResult(createResult.Errors.First());
            }

            var externalLogin = new UserLoginInfo(provider, providerId);
            var addExternalResult = await userManager.AddLoginAsync(user.Id, externalLogin);
            if (!addExternalResult.Succeeded)
            {
                return new AuthenticateResult(addExternalResult.Errors.First());
            }

            var result = await AccountCreatedFromExternalProviderAsync(user.Id, provider, providerId, claims);
            if (result != null) return result;

            return await SignInFromExternalProviderAsync(user.Id, provider);
        }

        protected virtual Task<TUser> CreateNewAccountFromExternalProviderAsync(string provider, string providerId, IEnumerable<Claim> claims)
        {
            var user = new TUser() { UserName = Guid.NewGuid().ToString("N") };
            return Task.FromResult(user);
        }

        protected virtual async Task<AuthenticateResult> AccountCreatedFromExternalProviderAsync(TKey userID, string provider, string providerId, IEnumerable<Claim> claims)
        {
            claims = await SetAccountEmailAsync(userID, claims);
            claims = await SetAccountPhoneAsync(userID, claims);

            return await UpdateAccountFromExternalClaimsAsync(userID, provider, providerId, claims);
        }

        protected virtual async Task<AuthenticateResult> SignInFromExternalProviderAsync(TKey userID, string provider)
        {
            
            return new AuthenticateResult(
                userID.ToString(), 
                await GetDisplayNameForAccountAsync(userID), 
                authenticationMethod: Thinktecture.IdentityServer.Core.Constants.AuthenticationMethods.External, 
                identityProvider: provider);
        }

        protected virtual async Task<AuthenticateResult> UpdateAccountFromExternalClaimsAsync(TKey userID, string provider, string providerId, IEnumerable<Claim> claims)
        {
            var existingClaims = await userManager.GetClaimsAsync(userID);
            var intersection = existingClaims.Intersect(claims, new ClaimComparer());
            var newClaims = claims.Except(intersection, new ClaimComparer());

            foreach (var claim in newClaims)
            {
                var result = await userManager.AddClaimAsync(userID, claim);
                if (!result.Succeeded)
                {
                    return new AuthenticateResult(result.Errors.First());
                }
            }

            return null;
        }

        protected virtual async Task<AuthenticateResult> ProcessExistingExternalAccountAsync(TKey userID, string provider, string providerId, IEnumerable<Claim> claims)
        {
            return await SignInFromExternalProviderAsync(userID, provider);
        }

        protected virtual async Task<IEnumerable<Claim>> SetAccountEmailAsync(TKey userID, IEnumerable<Claim> claims)
        {
            var email = claims.FirstOrDefault(x => x.Type == Thinktecture.IdentityServer.Core.Constants.ClaimTypes.Email);
            if (email != null)
            {
                var userEmail = await userManager.GetEmailAsync(userID);
                if (userEmail == null)
                {
                    // if this fails, then presumably the email is already associated with another account
                    // so ignore the error and let the claim pass thru
                    var result = await userManager.SetEmailAsync(userID, email.Value);
                    if (result.Succeeded)
                    {
                        var email_verified = claims.FirstOrDefault(x => x.Type == Thinktecture.IdentityServer.Core.Constants.ClaimTypes.EmailVerified);
                        if (email_verified != null && email_verified.Value == "true")
                        {
                            var token = await userManager.GenerateEmailConfirmationTokenAsync(userID);
                            await userManager.ConfirmEmailAsync(userID, token);
                        }

                        var emailClaims = new string[] { Thinktecture.IdentityServer.Core.Constants.ClaimTypes.Email, Thinktecture.IdentityServer.Core.Constants.ClaimTypes.EmailVerified };
                        return claims.Where(x => !emailClaims.Contains(x.Type));
                    }
                }
            }

            return claims;
        }

        protected virtual async Task<IEnumerable<Claim>> SetAccountPhoneAsync(TKey userID, IEnumerable<Claim> claims)
        {
            var phone = claims.FirstOrDefault(x=>x.Type==Thinktecture.IdentityServer.Core.Constants.ClaimTypes.PhoneNumber);
            if (phone != null)
            {
                var userPhone = await userManager.GetPhoneNumberAsync(userID);
                if (userPhone == null)
                {
                    // if this fails, then presumably the phone is already associated with another account
                    // so ignore the error and let the claim pass thru
                    var result = await userManager.SetPhoneNumberAsync(userID, phone.Value);
                    if (result.Succeeded)
                    {
                        var phone_verified = claims.FirstOrDefault(x => x.Type == Thinktecture.IdentityServer.Core.Constants.ClaimTypes.PhoneNumberVerified);
                        if (phone_verified != null && phone_verified.Value == "true")
                        {
                            var token = await userManager.GenerateChangePhoneNumberTokenAsync(userID, phone.Value);
                            await userManager.ChangePhoneNumberAsync(userID, phone.Value, token);
                        }

                        var phoneClaims = new string[] { Thinktecture.IdentityServer.Core.Constants.ClaimTypes.PhoneNumber, Thinktecture.IdentityServer.Core.Constants.ClaimTypes.PhoneNumberVerified };
                        return claims.Where(x => !phoneClaims.Contains(x.Type));
                    }
                }
            }
            
            return claims;
        }

        public virtual async Task<bool> IsActiveAsync(ClaimsPrincipal subject)
        {
            if (subject == null) throw new ArgumentNullException("subject");

            TKey key = ConvertSubjectToKey(subject.GetSubjectId());
            var acct = await userManager.FindByIdAsync(key);
            if (acct == null)
            {
                return false;
            }

            return true;
        }

        public virtual Task SignOutAsync(ClaimsPrincipal subject)
        {
            return Task.FromResult<object>(null);
        }
    }
}