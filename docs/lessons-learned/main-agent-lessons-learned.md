# Main Agent Lessons Learned

## DigitalOcean Server Access

**CRITICAL**: When accessing the staging/production DigitalOcean server for administrative tasks:

### ✅ CORRECT Method - SSH as root
```bash
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope root@104.131.165.14
```

**Why**: Root user has full permissions, no sudo password required.

**Use for**:
- Nginx configuration changes (`/etc/nginx/`)
- SSL certificate management (certbot)
- System package installation (apt)
- Service management (systemctl)

### ❌ WRONG Method - SSH as witchcity with sudo
```bash
# DON'T DO THIS - requires password we don't have
ssh -i /home/chad/.ssh/id_ed25519_witchcityrope witchcity@104.131.165.14
sudo nginx -t  # FAILS - requires password
```

**Why it fails**: The `witchcity` user doesn't have passwordless sudo configured.

**Only use witchcity user for**:
- Application deployment (docker-compose in `/opt/witchcityrope/staging/`)
- Checking application logs
- Non-privileged operations

### Available Tools

**doctl (DigitalOcean CLI)**:
```bash
doctl compute droplet list  # List droplets
doctl compute ssh <droplet-id>  # SSH to droplet
```

**Authentication**: Already configured in `/home/chad/.config/doctl/config.yaml`

---

## Pattern: Administrative Tasks on Staging/Production

When you need to modify system configuration (nginx, SSL, etc.):

1. **SSH as root**: `ssh -i /home/chad/.ssh/id_ed25519_witchcityrope root@104.131.165.14`
2. **Make changes**: Full root access, no sudo needed
3. **Test**: `nginx -t`, `systemctl status`, etc.
4. **Apply**: `systemctl reload nginx`, etc.

When you need to deploy application changes:

1. **Use staging-deploy skill**: Builds and pushes images
2. **SSH as witchcity**: Application-level operations only
3. **docker-compose commands**: Restart containers, check logs

---

**Last Updated**: 2025-11-22
**Applies To**: Staging and production servers at 104.131.165.14
