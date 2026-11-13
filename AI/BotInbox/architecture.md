# BotInbox.ai — Architecture Components

High-level visionary architecture focused on innovation, global scale, and deep agent integration.

## 1. Client Layer
- Web application
- Mobile apps (iOS / Android)
- Desktop clients and browser extensions

Clients provide the primary user interface and capture local signals (camera for vision, platform biometrics).

## 2. Deep Auth & Identity
- Modern OAuth / OIDC flows
- Passkeys and WebAuthn
- Biometric authentication (thumbprint, Face ID, platform authenticators)
- Vision-enhanced identity checks (liveness detection, document verification)
- Strong session and device management

Identity is treated as a first-class concern so agents and services can operate with clear, revocable permissions.

## 3. Edge / API Gateway
- Global anycast or multi-region API gateway
- Rate limiting, WAF, DDoS protection
- Request routing and authentication enforcement

## 4. MCP Orchestration Layer (Core Innovation)
Central hub that exposes standardized Model Context Protocol servers. This is the primary integration surface for intelligence.

### MCP Servers
- **SI Search MCP Server** — Cross-account semantic + hybrid search with super-intelligent ranking and context awareness
- **Grok MCP Server** — Reasoning, conversation, tool use and real-time knowledge
- **Muse MCP Server** — Creative capabilities and extended toolset
- **Openclaw Bot MCP Server** — Autonomous agent workflows and bot orchestration

Any MCP-compatible client or agent can discover and call these tools securely. New agents or models can be added by implementing additional MCP servers without changing the core platform.

## 5. BotInbox Core Services
- **Unified Inbox Engine** — Aggregation, normalization and presentation of multiple email accounts
- **Agent Runtime & Orchestrator** — Lifecycle management, permissioning and coordination of agents
- **SI Triage & Reasoning** — Intelligent prioritization, summarization and action suggestion
- **Vision Pipeline** — Processing of attachments, screenshots, OCR, image understanding and multimodal context

## 6. Globally Scalable Data Layer
- **Primary Database**: CockroachDB or Google Spanner / AlloyDB (distributed SQL with strong multi-region consistency and native vector support)
- **Vector Index** — Embedded semantic search for SI capabilities
- **Object Storage** — Global, durable storage for attachments and media
- **Edge Cache** — Low-latency caching layer

The data layer is designed for agent-scale workloads: high concurrency, global distribution, and the ability to serve both transactional inbox operations and large-scale semantic retrieval.

## 7. External Systems
- Email providers (Gmail, Outlook / Microsoft 365, IMAP, custom domains) via OAuth and standard protocols
- Additional MCP-compatible tools and third-party services

## Design Principles
- **Agent-native** — MCP is the primary extension point
- **User-controlled** — Clear permissions, auditability and revocation for every agent action
- **Globally consistent** — Strong consistency where it matters, low latency via multi-region deployment
- **Multimodal** — Vision is a first-class input, not an afterthought
- **Secure by default** — Deep biometric and vision-assisted identity, least-privilege agent access

---

This document describes a high-level visionary architecture as of November 2026. It is intended for reference and exploration.
