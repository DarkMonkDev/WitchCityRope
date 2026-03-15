<!-- Last Updated: 2026-03-15 -->
<!-- Version: 1.2 -->
<!-- Owner: Infrastructure -->
<!-- Status: Active -->

# DNS Cutover Plan: witchcityrope.com Migration from Wix to DigitalOcean

## Overview

This plan covered switching `witchcityrope.com` DNS from Wix to the existing DigitalOcean production server. The migration is COMPLETE as of 2026-03-15. The production application is live at `witchcityrope.com` and remains accessible via the backdoor domain `prod.notfai.com`.

## Current Infrastructure

| Component | Detail |
|-----------|--------|
| Production Server | DigitalOcean Droplet, IP `104.131.165.14` |
| Current Domain | `witchcityrope.com` (live) + `prod.notfai.com` (backdoor) |
| DNS Registrar | GoDaddy (nameservers restored to GoDaddy, DNS configured) |
| SSL | Let's Encrypt / Certbot, auto-renewal via cron (twice daily) |
| Reverse Proxy | Nginx on host (not containerized), SSL termination at Nginx |
| Nginx Config | `/etc/nginx/sites-available/witchcityrope-production` |
| SSH Access | User `witchcity`, key `~/.ssh/id_ed25519_witchcityrope` |
| CORS | Already includes `https://witchcityrope.com` and `https://www.witchcityrope.com` in `docker-compose.production.yml` |
| JWT Config | Issuer/Audience already set to `https://witchcityrope.com` |
| Containers | Running and healthy |

---

## Phase 1: Pre-Cutover Preparation -- COMPLETE

Complete these steps BEFORE changing any DNS records.

### 1.1 Switch Nameservers Back to GoDaddy

The domain currently has **nameservers delegated to Wix**, meaning Wix controls all DNS records. Before we can create A records pointing to DigitalOcean, we need to reclaim DNS control at GoDaddy.

1. Log into GoDaddy -> Domain Settings -> `witchcityrope.com` -> **Nameservers**
2. Current nameservers will show Wix nameservers (e.g., `ns1.wixdns.net`, `ns2.wixdns.net`)
3. Change to **GoDaddy's default nameservers** (select "Use GoDaddy nameservers" or equivalent option)
4. Save the change

**Important**: Once nameservers switch back to GoDaddy, the Wix site will stop resolving at `witchcityrope.com`. There will be a **brief downtime window** between when the nameserver change propagates and when we set up the new A records. Nameserver changes can take **up to 24-48 hours** to fully propagate (though often much faster, sometimes within 1-2 hours).

**Mitigation**: After switching nameservers, immediately proceed to step 1.2 to set up the A records at GoDaddy so they're ready as soon as GoDaddy's nameservers become authoritative.

### 1.2 Set Up A Records and Lower TTL at GoDaddy

Once nameservers are back on GoDaddy, go to DNS Management and:

1. Create A records pointing to the DigitalOcean server (see Phase 3 for exact records)
2. Set TTL on all records to **300 seconds** (5 minutes) -- this ensures fast rollback if needed
3. Wait for nameserver propagation to complete before proceeding to Phase 2

Verify nameservers have propagated:

```bash
dig NS witchcityrope.com +short
# Should show GoDaddy nameservers (e.g., ns*.domaincontrol.com), NOT Wix
```

### 1.3 Verify Production Health

```bash
# SSH into production server
ssh -i ~/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14

# Check containers are running
docker ps

# Health check the API
curl -s http://localhost:5655/health

# Verify the site loads externally
curl -sI https://prod.notfai.com | head -20
```

### 1.4 Verify Production Configuration

```bash
# On the DO server, check environment variables are set
docker exec witchcityrope-api-1 env | grep -E "(SENDGRID|AUTHORIZE_NET|JWT)" | head -5

# Verify CORS includes witchcityrope.com
grep -i "witchcityrope.com" /home/witchcity/docker-compose.production.yml

# Verify JWT issuer/audience
grep -i "jwt" /home/witchcity/docker-compose.production.yml
```

### 1.5 Test Payment Processing

Verify Authorize.net is in live mode (not sandbox). Process a small test transaction if possible, or verify the configuration references production credentials.

### 1.6 Test Email Sending

Verify SendGrid is configured with production API key (not sandbox mode). Trigger a test email (e.g., password reset for a test account) and confirm delivery.

---

## Phase 2: SSL Certificate for witchcityrope.com -- COMPLETE

### Option A: DNS-01 Challenge (Preferred -- Get Cert BEFORE DNS Cutover)

This approach obtains the certificate before DNS points to the server. It requires adding a TXT record at GoDaddy.

```bash
# On the DO server
sudo certbot certonly --manual --preferred-challenges dns \
  -d witchcityrope.com -d www.witchcityrope.com
```

Certbot will prompt you to create a TXT record at `_acme-challenge.witchcityrope.com` in GoDaddy DNS. Add the record, wait for propagation (~2-5 minutes), then confirm in certbot.

Verify the TXT record propagated before confirming:

```bash
# From any machine
dig TXT _acme-challenge.witchcityrope.com +short
```

### Option B: HTTP-01 Challenge (Simpler, but Requires DNS First)

If DNS is already pointing to the server, this is simpler:

```bash
# On the DO server (after DNS cutover)
sudo certbot certonly --nginx \
  -d witchcityrope.com -d www.witchcityrope.com
```

### Update Nginx Config

After obtaining the certificate, update the Nginx server block:

```bash
sudo nano /etc/nginx/sites-available/witchcityrope-production
```

Update the `server_name` and SSL cert paths:

```nginx
server {
    listen 443 ssl;
    server_name witchcityrope.com www.witchcityrope.com;

    ssl_certificate /etc/letsencrypt/live/witchcityrope.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/witchcityrope.com/privkey.pem;

    # ... rest of existing config ...
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name witchcityrope.com www.witchcityrope.com;
    return 301 https://witchcityrope.com$request_uri;
}

# Redirect www to non-www (optional, pick one canonical domain)
server {
    listen 443 ssl;
    server_name www.witchcityrope.com;

    ssl_certificate /etc/letsencrypt/live/witchcityrope.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/witchcityrope.com/privkey.pem;

    return 301 https://witchcityrope.com$request_uri;
}
```

Test and reload:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

**Note**: Keep the existing `prod.notfai.com` server block active as a fallback during transition.

---

## Phase 3: DNS Cutover at GoDaddy -- COMPLETE

### 3.1 Confirm DNS Records

By this point, the A records should already be set from Phase 1.2. Verify they're correct in GoDaddy DNS management:

| Record Type | Host | Value | TTL |
|------------|------|-------|-----|
| A | `@` | `104.131.165.14` | 300 |
| A | `www` | `104.131.165.14` | 300 |

**Note**: The app uses `/api` path proxying through Nginx, not a separate `api.` subdomain. No additional subdomain records are needed.

### 3.2 Clean Up Stale Records

Remove any leftover A, AAAA, or CNAME records for `@` and `www` that may have been auto-created by GoDaddy or carried over. Ensure only the new A records remain.

### 3.3 Verify DNS Propagation

```bash
# Check A record resolution
dig A witchcityrope.com +short
# Expected: 104.131.165.14

dig A www.witchcityrope.com +short
# Expected: 104.131.165.14

# Alternative check
nslookup witchcityrope.com 8.8.8.8
```

With TTL at 300 seconds, propagation should complete within 5-10 minutes for most resolvers.

---

## Phase 4: Post-Cutover Verification -- COMPLETE

Run through each check after DNS has propagated.

### 4.1 Site Loads

```bash
curl -sI https://witchcityrope.com | head -20
# Expect: HTTP/2 200, valid headers

curl -sI https://www.witchcityrope.com | head -5
# Expect: 301 redirect to https://witchcityrope.com (if www redirect configured)
```

Open `https://witchcityrope.com` in a browser and verify the page renders correctly.

### 4.2 SSL Certificate Valid

```bash
echo | openssl s_client -servername witchcityrope.com -connect witchcityrope.com:443 2>/dev/null | openssl x509 -noout -dates -subject
# Verify: subject includes witchcityrope.com, dates are current
```

Also check in the browser: click the padlock icon and verify the certificate is issued by Let's Encrypt for `witchcityrope.com`.

### 4.3 Login / Auth Flow

1. Navigate to `https://witchcityrope.com`
2. Log in with a test account (e.g., `admin@witchcityrope.com` / `Test123!`)
3. Verify login succeeds and session persists across page navigation
4. Verify logout works

### 4.4 Event Browsing

1. Navigate to the events page
2. Verify events load from the API
3. Click into an event detail page and verify it renders

### 4.5 Payment Flow

If safe to do so, attempt a small test transaction to verify Authorize.net integration works under the production domain. Otherwise, verify the payment form loads and Accept.js initializes without console errors.

### 4.6 Email Delivery

Trigger a password reset or other transactional email. Verify it arrives and that links in the email point to `https://witchcityrope.com` (not `prod.notfai.com`).

### 4.7 CORS / API Calls

Open browser dev tools (Network tab) and verify API calls from the frontend do not show CORS errors. Check that requests to `/api/` endpoints return successful responses.

---

## Phase 5: Rollback Plan

If something goes wrong after cutover:

1. **Switch nameservers back to Wix at GoDaddy**: Go to Domain Settings -> Nameservers -> change back to Wix nameservers (e.g., `ns1.wixdns.net`, `ns2.wixdns.net`)
2. **Propagation time**: Nameserver changes can take **up to 24-48 hours** to fully propagate (unlike A record changes which are faster). This is a slower rollback than a simple A record change.
3. **Keep Wix active**: Do NOT cancel or modify the Wix site until go-live is confirmed stable
4. **Revert Nginx if needed**: The `prod.notfai.com` server block remains active throughout

**Save Wix nameservers BEFORE cutover** so you have them for rollback:

```bash
dig NS witchcityrope.com +short
# Record the Wix nameservers before making any changes
```

**Note**: Because nameserver rollback is slow (24-48 hours), take extra care during the verification phase (Phase 4) before declaring go-live successful.

---

## Phase 6: Post-Go-Live Cleanup

Complete these after confirming stable operation (recommended: wait at least 1 week).

### 6.1 Raise DNS TTL

In GoDaddy, increase TTL back to **3600** (1 hour) or higher for both A records.

### 6.2 Cancel Wix Subscription

Only after confirming everything works reliably for a week or more. Download any content or assets from Wix before canceling.

### 6.3 Clean Up prod.notfai.com References

Optionally remove or redirect `prod.notfai.com`:
- Update internal documentation referencing `prod.notfai.com`
- Consider adding a redirect from `prod.notfai.com` to `witchcityrope.com` in Nginx
- Or keep it as a secondary access point for admin/debugging purposes

### 6.4 Update Application Configuration

Review and update any references to `prod.notfai.com` in:
- Email templates (SendGrid dynamic templates)
- Webhook URLs (PayPal, Authorize.net callback URLs)
- Any hardcoded URLs in application configuration

### 6.5 Verify Certbot Auto-Renewal

Ensure certbot auto-renewal covers the new `witchcityrope.com` certificate:

```bash
sudo certbot renew --dry-run
```

Confirm the cron job or systemd timer is active:

```bash
systemctl list-timers | grep certbot
# or
crontab -l | grep certbot
```

---

## Checklist Summary

### Before Cutover
- [x] Wix nameservers recorded for rollback
- [x] Nameservers switched back to GoDaddy
- [x] A records created pointing to `104.131.165.14`
- [x] TTL set to 300 on all records
- [x] Nameserver propagation verified (`dig NS` shows GoDaddy)
- [x] Production app verified healthy at prod.notfai.com
- [x] Environment variables and secrets verified
- [x] Payment processing tested
- [x] Email delivery tested

### During Cutover
- [x] SSL certificate obtained for witchcityrope.com
- [x] Nginx config updated with new cert paths and server_name
- [x] Nginx tested and reloaded
- [x] DNS A records confirmed correct at GoDaddy
- [x] Stale DNS records removed

### After Cutover
- [x] https://witchcityrope.com loads correctly
- [x] SSL certificate valid in browser
- [x] Login/auth flow works
- [x] Events page loads data
- [x] Payment form initializes
- [x] Emails deliver with correct links
- [x] No CORS errors in browser console

### Post-Stabilization (1 week+)
- [ ] TTL raised back to 3600+
- [ ] Wix subscription canceled
- [ ] prod.notfai.com references updated
- [ ] Webhook URLs updated to witchcityrope.com
- [ ] Certbot auto-renewal verified for new domain
