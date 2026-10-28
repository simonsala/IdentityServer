import * as oidc from "openid-client"
import {
  createCookieSessionStorage,
  createMemorySessionStorage,
  data,
  redirect,
} from "react-router"
import { identityServerUrl } from "@/lib/identity"
import { createNewSession } from "@/services/sessionService"

const appUrl = process.env.APP_URL
const clientId = process.env.OIDC_CLIENT_ID
const clientSecret = process.env.OIDC_CLIENT_SECRET
const sessionSecret: string = process.env.SESSION_COOKIE_SECRET ?? ""

const redirectUri = new URL("/auth/callback", appUrl).href
const scope = "openid profile roles ecommerceAPI offline_access"

type LoginTransaction = {
  state: string
  codeVerifier: string
  returnTo: string
}

type User = {
  name?: string
  givenName?: string
}

// Short-lived signed cookie that carries state + PKCE verifier from /auth/login to /auth/callback.
const transactionStorage = createCookieSessionStorage<{ tx: LoginTransaction }>({
  cookie: {
    name: "__spa_oidc",
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/auth",
    maxAge: 60 * 10,
    secrets: [sessionSecret],
  },
})

// Tokens stay on the server; the browser only gets an opaque session id cookie.
const sessionStorage = createCookieSessionStorage<{ id: number }>({
  cookie: {
    name: "__spa_session",
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    maxAge: 60 * 60 * 8,
    secrets: [sessionSecret],
  },
})

let oidcConfig: Promise<oidc.Configuration> | undefined

// Discovery is cached; a failed attempt is not, so the next request retries.
function getOidcConfig() {
  oidcConfig ??= oidc
    .discovery(new URL(identityServerUrl), clientId ?? "", clientSecret)
    .catch((error: unknown) => {
      oidcConfig = undefined
      throw error
    })
  return oidcConfig
}

export async function beginLogin(request: Request) {
  const config = await getOidcConfig()
  const state = oidc.randomState()
  const codeVerifier = oidc.randomPKCECodeVerifier()
  const codeChallenge = await oidc.calculatePKCECodeChallenge(codeVerifier)

  const authorizeUrl = oidc.buildAuthorizationUrl(config, {
    redirect_uri: redirectUri,
    scope,
    state,
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
  })

  const tx = await transactionStorage.getSession()
  tx.set("tx", {
    state,
    codeVerifier,
    returnTo: localPathOrRoot(new URL(request.url).searchParams.get("returnTo")),
  })

  return redirect(authorizeUrl.href, {
    headers: { "Set-Cookie": await transactionStorage.commitSession(tx) },
  })
}

export async function completeLogin(request: Request) {
  const tx = await transactionStorage.getSession(request.headers.get("Cookie"))
  const transaction = tx.get("tx")
  const clearTransaction = await transactionStorage.destroySession(tx)

  if (!transaction) {
    return data(
      { error: "The sign-in attempt expired or was started in another browser. Please try again." },
      { status: 400, headers: { "Set-Cookie": clearTransaction } }
    )
  }

  try {
    const config = await getOidcConfig()

    // Rebuild the callback URL from the registered redirect_uri so it matches what was sent to /authorize.
    const callbackUrl = new URL(redirectUri)
    callbackUrl.search = new URL(request.url).search

    // Validates iss + state, then redeems the code at the token endpoint with the PKCE verifier and client secret.
    const tokens = await oidc.authorizationCodeGrant(config, callbackUrl, {
      pkceCodeVerifier: transaction.codeVerifier,
      expectedState: transaction.state,
    })

    const idTokenClaims = tokens.claims()
    if (!tokens.id_token || !idTokenClaims) {
      throw new Error("IdentityServer did not return an id_token.")
    }

    // Profile and role claims are not in the id_token by default; read them from userinfo.
    const userInfo = await oidc.fetchUserInfo(config, tokens.access_token, idTokenClaims.sub)

    var sessionId = await createNewSession({
      user: {
        name: userInfo.name,
        givenName: userInfo.given_name,
      },
      tokens: {
        idToken: tokens.id_token,
        accessToken: tokens.access_token,
        refreshToken: tokens.refresh_token,
        expiresAt: Date.now() + (tokens.expiresIn() ?? 0) * 1000,
      }
    })

    if (!sessionId) {
      throw new Error("Failed to create session.")
    }

    var session = await sessionStorage.getSession()
    session.set("id", sessionId)

    const headers = new Headers()
    headers.append("Set-Cookie", clearTransaction)
    headers.append("Set-Cookie", await sessionStorage.commitSession(session))
    return redirect(transaction.returnTo, { headers })
  } catch (error) {
    console.error("OIDC callback failed", error)

    const message =
      error instanceof oidc.AuthorizationResponseError
        ? (error.error_description ?? error.error)
        : "Sign-in could not be completed. Please try again."

    return data({ error: message }, { status: 400, headers: { "Set-Cookie": clearTransaction } })
  }
}

export async function getUser(request: Request) {
  const session = await sessionStorage.getSession(request.headers.get("Cookie"))
  
  const sessionId = session.get("id")
  if (!sessionId) {
    return null
  }
  return await getSession(sessionId)
}

// Only allow local paths so /auth/login?returnTo= cannot be used as an open redirect.
function localPathOrRoot(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//") && !value.startsWith("/\\") ? value : "/"
}