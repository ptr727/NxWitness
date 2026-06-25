#!/bin/bash

# Rename the LinuxServer.io "abc" user and group to the mediaserver account name, and
# repoint the base init-adduser script (which maps PUID/PGID onto "abc") at the new name.
# LSIO does not support renaming "abc", so assert the expected init-adduser signature
# before and after patching and fail the build if it no longer matches, rather than
# silently regressing PUID/PGID handling.
# https://docs.linuxserver.io/misc/non-root/
# https://github.com/just-containers/s6-overlay#container-environment
# https://www.linuxserver.io/blog/how-is-container-formed
# https://github.com/linuxserver/docker-baseimage-ubuntu/blob/noble/root/etc/s6-overlay/s6-rc.d/init-adduser/run
# Arguments:
#   $1 - mediaserver account name (COMPANY_NAME)

set -euo pipefail

COMPANY_NAME="${1:?company name is required}"
ADDUSER_RUN="/etc/s6-overlay/s6-rc.d/init-adduser/run"

# Verify the LSIO base still matches the expected signature before patching
getent passwd abc >/dev/null || { echo "ERROR: expected LSIO user abc not found" >&2; exit 1; }
getent group abc >/dev/null || { echo "ERROR: expected LSIO group abc not found" >&2; exit 1; }
test -f "${ADDUSER_RUN}" || { echo "ERROR: ${ADDUSER_RUN} not found" >&2; exit 1; }
grep -q 'groupmod -o -g "${PGID}" abc' "${ADDUSER_RUN}" || { echo "ERROR: init-adduser groupmod signature changed" >&2; exit 1; }
grep -q 'usermod -o -u "${PUID}" abc' "${ADDUSER_RUN}" || { echo "ERROR: init-adduser usermod signature changed" >&2; exit 1; }

# Rename abc to the mediaserver account and repoint init-adduser at the new name
usermod -l "${COMPANY_NAME}" abc
groupmod -n "${COMPANY_NAME}" abc
sed -i "s/abc/${COMPANY_NAME}/g" "${ADDUSER_RUN}"

# Verify the rename took effect and no stray abc reference remains
getent passwd "${COMPANY_NAME}" >/dev/null || { echo "ERROR: rename to ${COMPANY_NAME} user failed" >&2; exit 1; }
getent group "${COMPANY_NAME}" >/dev/null || { echo "ERROR: rename to ${COMPANY_NAME} group failed" >&2; exit 1; }
if getent passwd abc >/dev/null; then echo "ERROR: abc user still present after rename" >&2; exit 1; fi
grep -q "usermod -o -u \"\${PUID}\" ${COMPANY_NAME}" "${ADDUSER_RUN}" || { echo "ERROR: init-adduser not repointed to ${COMPANY_NAME}" >&2; exit 1; }
if grep -qw abc "${ADDUSER_RUN}"; then echo "ERROR: stray abc token remains in init-adduser" >&2; exit 1; fi
