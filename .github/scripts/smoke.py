"""Temporary CI environment; never reads the local .env or prints credentials."""
import http.client
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
COMPOSE = ["docker", "compose", "--env-file", os.devnull,
           "-f", "compose.ci.yml", "-p", "finanzauto-ci"]


def compose(*args, capture=False):
    return subprocess.run(COMPOSE + list(args), cwd=ROOT, check=True,
                          text=True, stdout=subprocess.PIPE if capture else None)


def request(base, path, expected, payload=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(base + path, data=data, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=5) as response:
            status, body = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, body = error.code, error.read()
    if status != expected:
        raise RuntimeError(f"{path}: expected HTTP {expected}, received {status}")
    return json.loads(body) if body and expected == 200 else None


def main():
    for name in ("CI_POSTGRES_PASSWORD", "CI_JWT_SIGNING_KEY",
                 "CI_BOOTSTRAP_ADMIN_PASSWORD", "CI_BOOTSTRAP_ADMIN_EMAIL"):
        if not os.environ.get(name, "").strip():
            raise RuntimeError(f"Missing configuration: {name}")
    if len(os.environ["CI_JWT_SIGNING_KEY"].encode()) < 32:
        raise RuntimeError("CI_JWT_SIGNING_KEY requires at least 32 bytes")
    if not 12 <= len(os.environ["CI_BOOTSTRAP_ADMIN_PASSWORD"]) <= 128:
        raise RuntimeError("CI_BOOTSTRAP_ADMIN_PASSWORD requires 12-128 characters")
    # Quote the password as a connection-string value, including embedded quotes.
    password = os.environ["CI_POSTGRES_PASSWORD"].replace('"', '""')
    os.environ["CI_DATABASE_CONNECTION"] = (
        'Host=db;Port=5432;Database=finanzauto_ci;Username=finanzauto_ci;'
        f'Password="{password}"')
    try:
        compose("up", "--build", "--detach", "--wait", "--wait-timeout", "120")
        address = compose("port", "api", "8080", capture=True).stdout.strip()
        base = "http://" + address
        deadline = time.monotonic() + 120
        while True:
            try:
                health = request(base, "/health", 200)
                if health.get("status") != "Healthy":
                    raise RuntimeError("Unexpected health response")
                break
            except (urllib.error.URLError, TimeoutError, ConnectionError,
                    http.client.HTTPException, RuntimeError):
                if time.monotonic() >= deadline:
                    raise RuntimeError("API did not become healthy within 120 seconds") from None
                time.sleep(2)
        print("PASS: health and database connectivity", flush=True)
        request(base, "/Profile", 401)
        print("PASS: profile rejects anonymous requests", flush=True)
        login = request(base, "/Login", 200, {
            "email": os.environ["CI_BOOTSTRAP_ADMIN_EMAIL"],
            "password": os.environ["CI_BOOTSTRAP_ADMIN_PASSWORD"]})
        token = login.get("accessToken")
        if not isinstance(token, str) or not token:
            raise RuntimeError("Login did not return an access token")
        profile = request(base, "/Profile", 200, token=token)
        if (profile.get("email", "").casefold()
                != os.environ["CI_BOOTSTRAP_ADMIN_EMAIL"].strip().casefold()
                or profile.get("roleName") != "Admin"):
            raise RuntimeError("Profile does not match the bootstrap administrator")
        print("PASS: login and authenticated administrator profile", flush=True)
    except Exception:
        compose("ps", "--all")
        raise
    finally:
        compose("down", "--volumes", "--remove-orphans")


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        # Do not print HTTP bodies, tokens, container logs or environment values.
        print(f"Smoke check failed: {error}", file=sys.stderr)
        sys.exit(1)
