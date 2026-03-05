---
name: maintenance-mode
description: Toggle maintenance mode for WitchCityRope staging or production. Shows a branded "site updating" page while containers restart or during planned downtime. Auto-refreshes every 30 seconds.
---

# Maintenance Mode Skill

**Purpose**: Enable/disable maintenance mode on staging or production via SSH to the droplet.

**When to Use**:
- During deployments that require container restarts (deploy skills call this automatically)
- For planned maintenance windows
- When you need users to see a friendly page instead of errors
- Manual toggle when working on production or staging

**When NOT to Use**:
- Testing locally (containers don't affect local dev)

## SINGLE SOURCE OF TRUTH

**This skill is the ONLY way to toggle maintenance mode.**

---

## How It Works

1. Connects to Vault to get SSH credentials
2. SSHs to the droplet
3. Creates/removes a flag file that nginx checks on every request
4. Reloads nginx to ensure the change takes effect immediately

**Flag File Locations**:
- Staging: `/opt/witchcityrope/staging/maintenance.flag`
- Production: `/opt/witchcityrope/production/maintenance.flag`

**When flag exists** -> Nginx returns 503 -> Shows maintenance.html (auto-refreshes every 30s)
**When flag removed** -> Nginx proxies to app normally
**When containers are down (502/504)** -> Shows maintenance page automatically (no flag needed)

**Backdoor URLs** (exempt from maintenance mode):
- Production: `https://notfai.com` / `https://www.notfai.com` (always proxies to real app)
- Staging: No backdoor (both domains show maintenance when enabled)

---

## Quick Reference

### Enable Maintenance Mode

```bash
# Staging (default)
bash .claude/skills/maintenance-mode/execute.sh --on

# Production
bash .claude/skills/maintenance-mode/execute.sh --on --env production
```

### Disable Maintenance Mode

```bash
# Staging (default)
bash .claude/skills/maintenance-mode/execute.sh --off

# Production
bash .claude/skills/maintenance-mode/execute.sh --off --env production
```

### Check Status

```bash
bash .claude/skills/maintenance-mode/execute.sh --status
```

---

## Prerequisites

### One-Time Setup

Before first use, run the setup script to deploy maintenance files and nginx configs:

```bash
bash .claude/skills/maintenance-mode/setup.sh
```

### Runtime Requirements

- Vault helpers at `.claude/skills/_shared/vault-helpers.sh`
- SSH credentials in Vault at `secret/shared/digitalocean`
- Network access to the droplet

---

## Deploy Skill Integration

The staging-deploy and production-deploy skills automatically:
1. Enable maintenance mode before container restart
2. Deploy containers
3. Wait for health checks to pass
4. Disable maintenance mode

This means users see the maintenance page during the ~30-60 second window while containers restart, instead of seeing errors.

---

## Troubleshooting

### Maintenance page not showing

**Cause**: Setup not run, or nginx config not updated

**Solution**: Run the setup script:
```bash
bash .claude/skills/maintenance-mode/setup.sh
```

### Can't disable maintenance mode

**Cause**: Flag file permissions issue

**Solution**: SSH and manually remove:
```bash
ssh -i ~/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 'rm -f /opt/witchcityrope/staging/maintenance.flag && sudo systemctl reload nginx'
```

### Maintenance page shows but looks broken (no CSS)

**Cause**: `/maintenance-assets/` route not configured or files missing

**Solution**: Check files exist on server:
```bash
ssh -i ~/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14 'ls -la /opt/witchcityrope/maintenance/'
```

---

## Technical Details

### Why HTTP 503?

- **SEO-safe**: Search engines don't index 503 pages
- **Cache-safe**: Browsers don't cache 503 responses
- **Semantic**: Means "temporarily unavailable" (correct for maintenance)

### Automatic Fallback

Nginx also catches container failures (502/504) and shows the maintenance page. No manual toggle needed for unexpected container crashes.

### Auto-Refresh

The maintenance page includes `<meta http-equiv="refresh" content="30">` which reloads the page every 30 seconds. When maintenance mode is disabled and the app is back up, the next refresh lands on the real site automatically.

---

## Files

| File | Purpose |
|------|---------|
| `execute.sh` | Main toggle script (on/off/status) |
| `setup.sh` | One-time droplet setup |
| `SKILL.md` | This documentation |
| `files/maintenance.html` | Maintenance page HTML |
| `files/styles.css` | Maintenance page styles (WCR branded) |
| `files/429.html` | Rate limiting error page |
| `files/nginx-notfai-staging.conf` | Updated staging nginx config |
| `files/nginx-witchcityrope-production.conf` | Updated production nginx config |
| `files/nginx-notfai-production.conf` | Updated backdoor nginx config |

---

**Remember**: This skill is executable automation. Run it, don't copy it.
