"""
Simple MCP client for the BotInbox.ai Auth POC
Connects to the local MCP server and calls the auth tools.
"""

import asyncio
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client


MCP_URL = "http://localhost:8000/mcp"


async def main():
    print(f"Connecting to MCP server at {MCP_URL} ...\n")

    async with streamablehttp_client(MCP_URL) as (read, write, _):
        async with ClientSession(read, write) as session:
            # Initialize the session
            await session.initialize()

            # 1. List available tools
            tools = await session.list_tools()
            print("Available tools:")
            for tool in tools.tools:
                print(f"  • {tool.name}: {tool.description}")
            print()

            # 2. Call check_auth (should ACCEPT for "test")
            print("→ Calling check_auth(user_id='test')")
            result = await session.call_tool(
                "check_auth",
                arguments={
                    "user_id": "test",
                    "credential_proof": "passkey-ok"
                }
            )
            print("Result:", result.content[0].text if result.content else result)
            print()

            # 3. Call check_auth for a rejected user
            print("→ Calling check_auth(user_id='bob')")
            result = await session.call_tool(
                "check_auth",
                arguments={
                    "user_id": "bob",
                    "credential_proof": "passkey-ok"
                }
            )
            print("Result:", result.content[0].text if result.content else result)
            print()

            # 4. List all mock users
            print("→ Calling list_users()")
            result = await session.call_tool("list_users", arguments={})
            print("Result:", result.content[0].text if result.content else result)


if __name__ == "__main__":
    asyncio.run(main())