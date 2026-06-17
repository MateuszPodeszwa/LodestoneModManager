// Patreon OAuth + membership lookup. We use the "identity" endpoint with the
// user's memberships so we can confirm they're a patron and read their tier/pledge.
// Docs: https://docs.patreon.com/#oauth  /  https://docs.patreon.com/#get-api-oauth2-v2-identity

const AUTHORIZE = 'https://www.patreon.com/oauth2/authorize'
const TOKEN = 'https://www.patreon.com/api/oauth2/token'
const IDENTITY = 'https://www.patreon.com/api/oauth2/v2/identity'

export interface PatreonConfig {
  clientId: string
  clientSecret: string
  redirectUri: string
  campaignId: string
  allowFormer: boolean
  betaThresholdCents: number
  // The campaign owner isn't a member of their own campaign, so they'd never resolve as a patron.
  // These allowlists (Patreon user ids / emails) grant supporter + beta to the owner and any teammates.
  ownerIds: string[]
  ownerEmails: string[]
}

function csv(value: unknown): string[] {
  return String(value ?? '')
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean)
}

export function patreonConfig(): PatreonConfig {
  const c = useRuntimeConfig()
  return {
    clientId: c.patreonClientId,
    clientSecret: c.patreonClientSecret,
    redirectUri: c.patreonRedirectUri,
    campaignId: c.patreonCampaignId,
    allowFormer: String(c.patreonAllowFormer) === 'true',
    betaThresholdCents: Number(c.betaThresholdCents) || 700,
    ownerIds: csv(c.patreonOwnerIds),
    ownerEmails: csv(c.patreonOwnerEmails).map((e) => e.toLowerCase()),
  }
}

export function isPatreonConfigured(): boolean {
  const c = patreonConfig()
  return !!(c.clientId && c.clientSecret && c.redirectUri)
}

export function buildAuthorizeUrl(state: string): string {
  const c = patreonConfig()
  const params = new URLSearchParams({
    response_type: 'code',
    client_id: c.clientId,
    redirect_uri: c.redirectUri,
    scope: 'identity identity[email] identity.memberships',
    state,
  })
  return `${AUTHORIZE}?${params.toString()}`
}

export async function exchangeCode(code: string): Promise<{ access_token: string; refresh_token: string }> {
  const c = patreonConfig()
  return await $fetch(TOKEN, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      code,
      grant_type: 'authorization_code',
      client_id: c.clientId,
      client_secret: c.clientSecret,
      redirect_uri: c.redirectUri,
    }).toString(),
  })
}

export interface PatreonEligibility {
  patreonUserId: string
  fullName: string | null
  email: string | null
  imageUrl: string | null
  patronStatus: string | null
  tierTitle: string | null
  currentlyEntitledCents: number
  lifetimeSupportCents: number
  isPatron: boolean
  betaAccess: boolean
}

export async function fetchEligibility(accessToken: string): Promise<PatreonEligibility> {
  const c = patreonConfig()
  const url =
    `${IDENTITY}?include=memberships,memberships.currently_entitled_tiers,memberships.campaign` +
    `&fields%5Buser%5D=full_name,email,image_url` +
    `&fields%5Bmember%5D=patron_status,currently_entitled_amount_cents,lifetime_support_cents,last_charge_status` +
    `&fields%5Btier%5D=title`

  const res = await $fetch<any>(url, { headers: { Authorization: `Bearer ${accessToken}` } })

  const user = res.data
  const included: any[] = res.included ?? []
  const tiersById = new Map<string, string>()
  for (const inc of included) {
    if (inc.type === 'tier') tiersById.set(inc.id, inc.attributes?.title ?? '')
  }

  // Candidate memberships (optionally restricted to our campaign).
  let members = included.filter((i) => i.type === 'member')
  if (c.campaignId) {
    members = members.filter((m) => m.relationships?.campaign?.data?.id === c.campaignId)
  }

  // Prefer an active patron with the highest current pledge.
  const rank = (s?: string) => (s === 'active_patron' ? 2 : s === 'declined_patron' ? 1 : 0)
  members.sort((a, b) => {
    const r = rank(b.attributes?.patron_status) - rank(a.attributes?.patron_status)
    if (r !== 0) return r
    return (b.attributes?.currently_entitled_amount_cents ?? 0) - (a.attributes?.currently_entitled_amount_cents ?? 0)
  })
  const best = members[0]

  const patronStatus: string | null = best?.attributes?.patron_status ?? null
  const currentlyEntitledCents: number = best?.attributes?.currently_entitled_amount_cents ?? 0
  const lifetimeSupportCents: number = best?.attributes?.lifetime_support_cents ?? 0

  let tierTitle: string | null = null
  const entitledTierId = best?.relationships?.currently_entitled_tiers?.data?.[0]?.id
  if (entitledTierId) tierTitle = tiersById.get(entitledTierId) ?? null

  const email: string | null = user.attributes?.email ?? null
  // The owner can't be a patron of their own campaign — grant supporter + beta via the allowlist.
  const isOwner = c.ownerIds.includes(String(user.id)) || (!!email && c.ownerEmails.includes(email.toLowerCase()))

  const isActive = patronStatus === 'active_patron'
  const isFormer = patronStatus === 'declined_patron' || patronStatus === 'former_patron'
  const isPatron = isOwner || (!!best && (isActive || (c.allowFormer && isFormer)))
  const betaAccess = isOwner || (isActive && currentlyEntitledCents >= c.betaThresholdCents)

  return {
    patreonUserId: user.id,
    fullName: user.attributes?.full_name ?? null,
    email,
    imageUrl: user.attributes?.image_url ?? null,
    patronStatus: patronStatus ?? (isOwner ? 'active_patron' : null),
    tierTitle: tierTitle ?? (isOwner ? 'Owner' : null),
    currentlyEntitledCents,
    lifetimeSupportCents,
    isPatron,
    betaAccess,
  }
}
