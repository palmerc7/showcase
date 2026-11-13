"""
BotInbox.ai POC – Local MCP Auth Server
- Exposes MCP tools (check_auth, list_users)
- Also exposes simple REST endpoints so the demo web page can call it
- Uses an in-memory mock database (accept / reject)
"""

from typing import Dict, Any, Optional
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
#from mcp.server.fastmcp import FastMCP
from mcp.server.mcpserver import MCPServer
import uvicorn

# ------------------------------------------------------------------
# Mock "database"
# ------------------------------------------------------------------
MOCK_USERS: Dict[str, Dict[str, Any]] = {
    "test": {
        "user_id": "test",
        "display_name": "Test User",
        "passkey_registered": True,
        "allowed": True,
    },
    "luigi": {
        "user_id": "luigi",
        "display_name": "Luigi Cousin",
        "passkey_registered": True,
        "allowed": True,
    },
    "mario": {
        "user_id": "mario",
        "display_name": "Mario Main",
        "passkey_registered": False,
        "allowed": False,
    },
}

# ------------------------------------------------------------------
# MCP Server
# ------------------------------------------------------------------
mcp = MCPServer(
    name="BotInbox Auth MCP",
    instructions="Local authentication MCP server for BotInbox.ai POC. Provides check_auth and list_users tools."
)

@mcp.tool()
def check_auth(user_id: str, credential_proof: str = "passkey-ok") -> dict:
    """
    Mock authorization check.
    Returns accept or reject based on the in-memory store.
    In a real system this would verify a WebAuthn assertion.
    """
    user = MOCK_USERS.get(user_id.lower())
    if not user:
        return {
            "status": "reject",
            "reason": "user_not_found",
            "user_id": user_id,
        }

    if not user.get("allowed", False):
        return {
            "status": "reject",
            "reason": "user_not_allowed",
            "user_id": user_id,
        }

    if not user.get("passkey_registered", False):
        return {
            "status": "reject",
            "reason": "no_passkey_registered",
            "user_id": user_id,
        }

    # Very simple mock of credential verification
    if credential_proof not in ("passkey-ok", "valid", "success"):
        return {
            "status": "reject",
            "reason": "invalid_credential",
            "user_id": user_id,
        }

    return {
        "status": "accept",
        "user_id": user["user_id"],
        "display_name": user["display_name"],
        "message": "Authorization successful (mock)",
    }


@mcp.tool()
def list_users() -> dict:
    """List all mock users and their auth status (for demo / debugging)."""
    return {
        "users": [
            {
                "user_id": u["user_id"],
                "display_name": u["display_name"],
                "passkey_registered": u["passkey_registered"],
                "allowed": u["allowed"],
            }
            for u in MOCK_USERS.values()
        ]
    }


# ------------------------------------------------------------------
# FastAPI app (REST + MCP HTTP transport)
# ------------------------------------------------------------------
app = FastAPI(title="BotInbox Auth MCP + REST")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Mount the MCP server under /mcp (streamable HTTP)
app.mount("/mcp", mcp.streamable_http_app())


class AuthRequest(BaseModel):
    user_id: str = Field(..., example="demo")
    credential_proof: str = Field("passkey-ok", example="passkey-ok")


@app.get("/")
def root():
    return {
        "service": "BotInbox.ai Auth MCP POC",
        "mcp_endpoint": "/mcp",
        "rest_check": "POST /auth/check",
        "rest_users": "GET /auth/users",
    }


@app.post("/auth/check")
def rest_check_auth(body: AuthRequest):
    """REST wrapper around the MCP check_auth tool (for the web demo)."""
    result = check_auth(body.user_id, body.credential_proof)
    if result["status"] == "reject":
        raise HTTPException(status_code=401, detail=result)
    return result


@app.get("/auth/users")
def rest_list_users():
    return list_users()


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
