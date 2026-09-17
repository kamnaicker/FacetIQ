using FacetIQ.Domain.Abstractions.Repositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace FacetIQ.Data.Identity
{
    public sealed class IdentityUserDirectory : IUserDirectory
    {
        private readonly UserManager<AppUser> _users;

        public IdentityUserDirectory(UserManager<AppUser> users)
        {
            _users = users;
        }

        // UserManager lookups take no cancellation token.
        public async Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var user = await _users.FindByEmailAsync(email);

            // An unconfirmed address may belong to someone else.
            if (user is null || !user.EmailConfirmed)
            {
                return null;
            }

            return user.Id;
        }

        public async Task<string?> FindEmailAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await _users.FindByIdAsync(userId);

            return user?.Email;
        }
    }
}
