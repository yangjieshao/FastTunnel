using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FastTunnel.Api.Helper
{
    /// <summary>
    /// </summary>
    public class AllowAnonymousAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        // 摘要: A context for
        // Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents.OnTokenValidated.
        public class AllowAnonymousContext : ResultContext<AuthenticationSchemeOptions>
        {
            // 摘要: Initializes a new instance of
            // Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext.
            public AllowAnonymousContext(HttpContext context, AuthenticationScheme scheme, AuthenticationSchemeOptions options)
                : base(context, scheme, options) { }
        }

        /// <summary>
        /// </summary>
        public AllowAnonymousAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        /// <summary>
        /// </summary>
        /// <returns> </returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return await Task.Run(() =>
            {
                var identity = new ClaimsIdentity([], Scheme.Name);
                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
            });
        }
    }
}
