using System;
using System.Collections.Generic;
using System.Text;

namespace FacetIQ.Domain.Abstractions.Repositories
{
    public interface IUserDirectory
    {
        /// <summary>Null when no account uses the email.</summary>
        Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken);

        /// <summary>Null when the account no longer exists.</summary>
        Task<string?> FindEmailAsync(string userId, CancellationToken cancellationToken);
    }
}
