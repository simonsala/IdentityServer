// IdentityServer origin, used by the browser (account API) and by the SPA server (OIDC protocol).
export const identityServerUrl: string =
  import.meta.env.VITE_IDENTITY_SERVER_URL ?? "https://localhost:5001"
