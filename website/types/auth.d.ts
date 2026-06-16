// Shape of the signed session cookie (nuxt-auth-utils). `user` is readable on the
// client via useUserSession(); secrets would go in SecureSessionData (we keep none).
declare module '#auth-utils' {
  interface User {
    patreonUserId: string
    name: string | null
    email: string | null
    imageUrl: string | null
    isPatron: boolean
    betaAccess: boolean
    tierTitle: string | null
    patronStatus: string | null
    currentlyEntitledCents: number
    supporterId: string | null
  }

  interface UserSession {
    loggedInAt: number
    // Fallback regen-lock timestamp when no database is configured.
    lastKeyGenAt?: number
  }

  interface SecureSessionData {
    _placeholder?: never
  }
}

export {}
