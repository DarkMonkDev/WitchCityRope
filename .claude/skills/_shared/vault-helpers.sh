#!/bin/bash
# Shared Vault Helper Functions for WitchCityRope Skills
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Usage: source this file from skill execute.sh scripts
#   source "$(dirname "$0")/../_shared/vault-helpers.sh"
#
# Functions:
#   vault_init          - Set up vault, validate token
#   vault_pull_env      - Pull all secrets to .env file
#   vault_get_field     - Get single field value
#   vault_try_init      - Non-fatal init (for optional vault usage)

VAULT_BIN="$HOME/bin/vault"
VAULT_ADDR_DEFAULT="https://vault.monksafterdark.com"

# Initialize vault - exits on failure
# Sets VAULT_ADDR, validates CLI and token
vault_init() {
    export VAULT_ADDR="${VAULT_ADDR:-$VAULT_ADDR_DEFAULT}"

    if [ ! -x "$VAULT_BIN" ]; then
        echo "   ❌ FAIL: Vault CLI not found at $VAULT_BIN"
        echo ""
        echo "   Install: Download from https://releases.hashicorp.com/vault/"
        echo "   Place at: $VAULT_BIN"
        exit 1
    fi

    if ! $VAULT_BIN token lookup > /dev/null 2>&1; then
        echo "   ❌ FAIL: Not logged into Vault"
        echo ""
        echo "   Run: $VAULT_BIN login"
        echo "   Vault: $VAULT_ADDR"
        exit 1
    fi

    echo "   ✅ Vault authenticated ($VAULT_ADDR)"
}

# Non-fatal vault init - returns 0 on success, 1 on failure
# Use for scripts where vault is optional
vault_try_init() {
    export VAULT_ADDR="${VAULT_ADDR:-$VAULT_ADDR_DEFAULT}"

    if [ ! -x "$VAULT_BIN" ]; then
        echo "   ⚠️  Vault CLI not found - continuing without vault"
        return 1
    fi

    if ! $VAULT_BIN token lookup > /dev/null 2>&1; then
        echo "   ⚠️  Not logged into Vault - continuing without vault"
        return 1
    fi

    echo "   ✅ Vault authenticated ($VAULT_ADDR)"
    return 0
}

# Pull all secrets from a vault path to a .env file
# Usage: vault_pull_env "secret/projects/witchcityrope/staging" "/tmp/env-file"
vault_pull_env() {
    local secret_path="$1"
    local output_file="$2"

    if [ -z "$secret_path" ] || [ -z "$output_file" ]; then
        echo "   ❌ Usage: vault_pull_env <secret_path> <output_file>"
        return 1
    fi

    $VAULT_BIN kv get -format=json "$secret_path" 2>/dev/null | \
        jq -r '.data.data | to_entries | .[] | "\(.key)=\(.value)"' > "$output_file"

    if [ ! -s "$output_file" ]; then
        echo "   ❌ FAIL: No secrets found at $secret_path"
        rm -f "$output_file"
        return 1
    fi

    local count
    count=$(wc -l < "$output_file" | tr -d ' ')
    echo "   ✅ Pulled $count secrets from $secret_path"
    return 0
}

# Get a single field value from vault
# Usage: value=$(vault_get_field "secret/projects/witchcityrope/staging" "DB_CONNECTION_STRING")
vault_get_field() {
    local secret_path="$1"
    local field_name="$2"

    if [ -z "$secret_path" ] || [ -z "$field_name" ]; then
        echo "   ❌ Usage: vault_get_field <secret_path> <field_name>" >&2
        return 1
    fi

    local value
    value=$($VAULT_BIN kv get -field="$field_name" "$secret_path" 2>/dev/null)

    if [ -z "$value" ]; then
        echo "   ❌ FAIL: Field '$field_name' not found at $secret_path" >&2
        return 1
    fi

    echo "$value"
    return 0
}
